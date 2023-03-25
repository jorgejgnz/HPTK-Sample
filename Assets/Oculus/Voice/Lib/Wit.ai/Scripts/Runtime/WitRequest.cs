/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.WitAi
{
    /// <summary>
    /// Manages a single request lifecycle when sending/receiving data from Wit.ai.
    ///
    /// Note: This is not intended to be instantiated directly. Requests should be created with the
    /// WitRequestFactory
    /// </summary>
    public class WitRequest
    {
        /// <summary>
        /// Error code thrown when an exception is caught during processing or
        /// some other general error happens that is not an error from the server
        /// </summary>
        public const int ERROR_CODE_GENERAL = -1;

        /// <summary>
        /// Error code returned when no configuration is defined
        /// </summary>
        public const int ERROR_CODE_NO_CONFIGURATION = -2;

        /// <summary>
        /// Error code returned when the client token has not been set in the
        /// Wit configuration.
        /// </summary>
        public const int ERROR_CODE_NO_CLIENT_TOKEN = -3;

        /// <summary>
        /// No data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_NO_DATA_FROM_SERVER = -4;

        /// <summary>
        /// Invalid data was returned from the server.
        /// </summary>
        public const int ERROR_CODE_INVALID_DATA_FROM_SERVER = -5;

        /// <summary>
        /// Request was aborted
        /// </summary>
        public const int ERROR_CODE_ABORTED = -6;

        /// <summary>
        /// Request to the server timeed out
        /// </summary>
        public const int ERROR_CODE_TIMEOUT = -7;

        private IWitRequestConfiguration configuration;
        public bool shouldPost;

        private string command;
        private string path;

        public QueryParam[] queryParams;

        private HttpWebRequest _request;
        private Stream _writeStream;

        private WitResponseNode responseData;

        private bool isActive;
        private bool responseStarted;

        public byte[] postData;
        public string postContentType;
        public string requestIdOverride;
        public string forcedHttpMethodType = null;

        private object streamLock = new object();

        private int bytesWritten;
        private bool requestRequiresBody;

        /// <summary>
        /// Callback called when a response is received from the server off a partial transcription
        /// </summary>
        public event Action<WitRequest> onPartialResponse;

        /// <summary>
        /// Callback called when a response is received from the server
        /// </summary>
        public event Action<WitRequest> onResponse;

        /// <summary>
        /// Callback called when the server is ready to receive data from the WitRequest's input
        /// stream. See WitRequest.Write()
        /// </summary>
        public Action<WitRequest> onInputStreamReady;

        /// <summary>
        /// Returns the raw string response that was received before converting it to a JSON object.
        ///
        /// NOTE: This response comes back on a different thread. Do not attempt ot set UI control
        /// values or other interactions from this callback. This is intended to be used for demo
        /// and test UI, not for regular use.
        /// </summary>
        public Action<string> onRawResponse;

        /// <summary>
        /// Returns a partial utterance from an in process request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        public Action<string> onPartialTranscription;

        /// <summary>
        /// Returns a full utterance from a completed request
        ///
        /// NOTE: This response comes back on a different thread.
        /// </summary>
        public Action<string> onFullTranscription;

        public delegate void PreSendRequestDelegate(ref Uri src_uri, out Dictionary<string,string> headers);

        /// <summary>
        /// Allows customization of the request before it is sent out.
        ///
        /// Note: This is for devs who are routing requests to their servers
        /// before sending data to Wit.ai. This allows adding any additional
        /// headers, url modifications, or customization of the request.
        /// </summary>
        public static PreSendRequestDelegate onPreSendRequest;

        public delegate Uri OnCustomizeUriEvent(UriBuilder uriBuilder);
        /// <summary>
        /// Provides an opportunity to customize the url just before a request executed
        /// </summary>
        public OnCustomizeUriEvent onCustomizeUri;

        public delegate Dictionary<string, string> OnProvideCustomHeadersEvent();
        /// <summary>
        /// Provides an opportunity to provide custom headers for the request just before it is
        /// executed.
        /// </summary>
        public OnProvideCustomHeadersEvent onProvideCustomHeaders;

        /// <summary>
        /// Returns true if a request is pending. Will return false after data has been populated
        /// from the response.
        /// </summary>
        public bool IsActive => isActive;

        /// <summary>
        /// JSON data that was received as a response from the server after onResponse has been
        /// called
        /// </summary>
        public WitResponseNode ResponseData => responseData;

        /// <summary>
        /// Encoding settings for audio based requests
        /// </summary>
        public AudioEncoding audioEncoding = new AudioEncoding();

        private int statusCode;
        public int StatusCode => statusCode;

        private string statusDescription;
        private bool isRequestStreamActive;
        public bool IsRequestStreamActive => IsActive && isRequestStreamActive;

        public bool HasResponseStarted => responseStarted;

        private bool isServerAuthRequired;
        public string StatusDescription => statusDescription;

        private int _timeoutMs;
        public int Timeout => _timeoutMs;

        private bool configurationRequired;
        private string callingStackTrace;
        private DateTime requestStartTime;
        private ConcurrentQueue<byte[]> writeBuffer = new ConcurrentQueue<byte[]>();

        public override string ToString()
        {
            return path;
        }

        public WitRequest(WitConfiguration configuration, string path,
            params QueryParam[] queryParams)
        {
            if (!configuration) throw new ArgumentException("Configuration is not set.");
            configurationRequired = true;
            this.configuration = configuration;
            this._timeoutMs = configuration ? configuration.timeoutMS : 10000;
            this.path = path;
            this.queryParams = queryParams;
            this.command = path.Split('/').First();
            this.shouldPost = WitEndpointConfig.GetEndpointConfig(configuration).Speech == this.command ||
                              WitEndpointConfig.GetEndpointConfig(configuration).Dictation == this.command;
        }

        public WitRequest(WitConfiguration configuration, string path, bool isServerAuthRequired,
            params QueryParam[] queryParams)
        {
            if (!isServerAuthRequired && !configuration)
                throw new ArgumentException("Configuration is not set.");
            configurationRequired = true;
            this.configuration = configuration;
            this._timeoutMs = configuration ? configuration.timeoutMS : 10000;
            this.isServerAuthRequired = isServerAuthRequired;
            this.path = path;
            this.queryParams = queryParams;
            this.command = path.Split('/').First();
            this.shouldPost = WitEndpointConfig.GetEndpointConfig(configuration).Speech == this.command ||
                              WitEndpointConfig.GetEndpointConfig(configuration).Dictation == this.command;
        }

        public WitRequest(string serverToken, string path, params QueryParam[] queryParams)
        {
            configurationRequired = false;
            this.isServerAuthRequired = true;
            this.command = path.Split('/').First();
            this.path = path;
            this.queryParams = queryParams;
            this._timeoutMs = 10000;
            #if UNITY_EDITOR
            this.configuration = new WitServerRequestConfiguration(serverToken);
            #endif
        }

        /// <summary>
        /// Key value pair that is sent as a query param in the Wit.ai uri
        /// </summary>
        public class QueryParam
        {
            public string key;
            public string value;
        }

        /// <summary>
        /// Start the async request for data from the Wit.ai servers
        /// </summary>
        public void Request()
        {
            responseStarted = false;

            Dictionary<string, string> requestParams = new Dictionary<string, string>();
            foreach (var par in queryParams)
            {
                requestParams[par.key] = par.value;
            }
            Func<UriBuilder, Uri> provideUri = (uriBuilder) => onCustomizeUri == null ? uriBuilder.Uri : onCustomizeUri(uriBuilder);
            WitVRequest.OnProvideCustomUri += provideUri;
            var uri = WitVRequest.GetWitUri(configuration, path, requestParams);
            WitVRequest.OnProvideCustomUri -= provideUri;
            StartRequest(uri);
        }

        private void StartRequest(Uri uri)
        {
            if (configuration == null && configurationRequired)
            {
                statusDescription = "Configuration is not set. Cannot start request.";
                VLog.E(statusDescription);
                statusCode = ERROR_CODE_NO_CONFIGURATION;
                SafeInvoke(onResponse);
                return;
            }

            if (!isServerAuthRequired && string.IsNullOrEmpty(configuration.GetClientAccessToken()))
            {
                statusDescription = "Client access token is not defined. Cannot start request.";
                VLog.E(statusDescription);
                statusCode = ERROR_CODE_NO_CLIENT_TOKEN;
                SafeInvoke(onResponse);
                return;
            }

            // Get headers
            Dictionary<string, string> headers = WitVRequest.GetWitHeaders(configuration, isServerAuthRequired);
            if (!string.IsNullOrEmpty(requestIdOverride))
            {
                headers[WitConstants.HEADER_REQUEST_ID] = requestIdOverride;
            }
            // Append additional headers
            if (onProvideCustomHeaders != null)
            {
                Dictionary<string, string> customHeaders = onProvideCustomHeaders();
                if (customHeaders != null)
                {
                    foreach (var key in customHeaders.Keys)
                    {
                        headers[key] = customHeaders[key];
                    }
                }
            }
            // Allow overrides
            if (onPreSendRequest != null)
            {
                onPreSendRequest(ref uri, out headers);
            }

            #if UNITY_WEBGL
            StartUnityRequest(uri, headers);
            #else
            StartThreadedRequest(uri, headers);
            #endif
        }

        private void StartThreadedRequest(Uri uri, Dictionary<string, string> headers)
        {
            // Create http web request
            _request = WebRequest.Create(uri.AbsoluteUri) as HttpWebRequest;

            if (forcedHttpMethodType != null) {
                _request.Method = forcedHttpMethodType;
            }

            #if UNITY_EDITOR
            // Required for batch mode & to ensure connections close properly
            _request.KeepAlive = false;
            #endif

            if (null != postContentType)
            {
                if (forcedHttpMethodType == null) {
                    _request.Method = "POST";
                }
                _request.ContentType = postContentType;
                _request.ContentLength = postData.Length;
            }

            // Configure additional headers
            if (shouldPost)
            {
                _request.Method = forcedHttpMethodType == null ? "POST" : forcedHttpMethodType;
                _request.ContentType = audioEncoding.ToString();
                _request.SendChunked = true;
            }

            requestRequiresBody = RequestRequiresBody(command);

            // Set user agent
            if (headers.ContainsKey(WitConstants.HEADER_USERAGENT))
            {
                _request.UserAgent = headers[WitConstants.HEADER_USERAGENT];
                headers.Remove(WitConstants.HEADER_USERAGENT);
            }
            // Apply all wit headers
            if (headers != null)
            {
                foreach (var key in headers.Keys)
                {
                    _request.Headers[key] = headers[key];
                }
            }

            requestStartTime = DateTime.UtcNow;
            isActive = true;
            statusCode = 0;
            statusDescription = "Starting request";
            _request.Timeout = Timeout;
            WatchMainThreadCallbacks();

            if (_request.Method == "POST" || _request.Method == "PUT")
            {
                var getRequestTask = _request.BeginGetRequestStream(HandleRequestStream, _request);
                ThreadPool.RegisterWaitForSingleObject(getRequestTask.AsyncWaitHandle,
                    HandleTimeoutTimer, _request, Timeout, true);
            }
            else
            {
                StartResponse();
            }
        }

        private void StartUnityRequest(Uri uri, Dictionary<string, string> headers)
        {
            UnityWebRequest request = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbGET);

            if (forcedHttpMethodType != null) {
                request.method = forcedHttpMethodType;
            }

            if (null != postContentType)
            {
                if (forcedHttpMethodType == null)
                {
                    request.method = UnityWebRequest.kHttpVerbPOST;
                }

                request.uploadHandler = new UploadHandlerRaw(postData);
                request.uploadHandler.contentType = postContentType;
            }

            // Configure additional headers
            if (shouldPost)
            {
                request.method = string.IsNullOrEmpty(forcedHttpMethodType) ?
                    UnityWebRequest.kHttpVerbPOST : forcedHttpMethodType;
                request.SetRequestHeader("Content-Type", audioEncoding.ToString());
            }

            requestRequiresBody = RequestRequiresBody(command);

            // Apply all wit headers
            foreach (var header in headers)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            requestStartTime = DateTime.UtcNow;
            isActive = true;
            statusCode = 0;
            statusDescription = "Starting request";
            request.timeout = Timeout;
            request.downloadHandler = new DownloadHandlerBuffer();

            if (request.method == UnityWebRequest.kHttpVerbPOST || request.method == UnityWebRequest.kHttpVerbPUT)
            {
                throw new NotImplementedException("Not yet implemented.");
            }

            VRequest performer = new VRequest();
            performer.RequestText(request, OnUnityRequestComplete, OnUnityRequestProgress);
        }

        private void OnUnityRequestProgress(float progress)
        {
            VLog.D("Request Progress: " + progress);
        }

        private void OnUnityRequestComplete(string response, string error)
        {
            isActive = false;
            responseStarted = false;
            responseData = WitResponseNode.Parse(response);
            statusCode = string.IsNullOrEmpty(error) ? 200 : 500;
            statusDescription = error;
            var responseString = response;
            responseData = WitResponseNode.Parse(responseString);
            try
            {
                onRawResponse?.Invoke(responseString);
                onPartialResponse?.Invoke(this);
                if (!string.IsNullOrEmpty(responseData.GetTranscription()))
                {
                    onFullTranscription?.Invoke(responseData.GetTranscription());
                }
            }
            catch (Exception e)
            {
                VLog.E("Error parsing response: " + e + "\n" + responseString);
                statusCode = ERROR_CODE_INVALID_DATA_FROM_SERVER;
                statusDescription = "Error parsing response: " + e + "\n" + responseString;
            }

            onResponse?.Invoke(this);
        }

        private bool RequestRequiresBody(string command)
        {
            return shouldPost;
        }

        private void StartResponse()
        {
            var asyncResult = _request.BeginGetResponse(HandleResponse, _request);
            ThreadPool.RegisterWaitForSingleObject(asyncResult.AsyncWaitHandle, HandleTimeoutTimer, _request, Timeout, true);
        }

        private void HandleTimeoutTimer(object state, bool timeout)
        {
            if (!timeout) return;

            // Clean up the current request if it is still going
            if (null != _request)
            {
                VLog.D("Request timed out after " + (DateTime.UtcNow - requestStartTime));
                _request.Abort();
            }

            isActive = false;

            // Close any open stream resources and clean up streaming state flags
            CloseRequestStream();

            // Update the error state to indicate the request timed out
            statusCode = ERROR_CODE_TIMEOUT;
            statusDescription = "Request timed out.";

            SafeInvoke(onResponse);
        }

        private void HandleResponse(IAsyncResult asyncResult)
        {
            bool sentResponse = false;
            string stringResponse = "";
            responseStarted = true;
            try
            {
                WebResponse response = _request.EndGetResponse(asyncResult);
                try
                {
                    HttpWebResponse httpResponse = response as HttpWebResponse;
                    statusCode = (int) httpResponse.StatusCode;
                    statusDescription = httpResponse.StatusDescription;
                    using (var responseStream = httpResponse.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string chunk;
                            while ((chunk = ReadToDelimiter(reader, WitConstants.ENDPOINT_JSON_DELIMITER)) != null)
                            {
                                stringResponse = chunk;
                                sentResponse |= ProcessStringResponse(stringResponse);
                            }
                            reader.Close();
                        }
                        // Call raw response for final
                        if (stringResponse.Length > 0 && null != responseData)
                        {
                            MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
                        }
                        responseStream.Close();
                    }
                }
                catch (JSONParseException e)
                {
                    VLog.E("Server returned invalid data: " + e.Message + "\n" +
                                   stringResponse);
                    statusCode = ERROR_CODE_INVALID_DATA_FROM_SERVER;
                    statusDescription = "Server returned invalid data.";
                }
                catch (WebException e)
                {
                    // Ensure was not cancelled
                    if (e.Status != WebExceptionStatus.RequestCanceled)
                    {
                        VLog.E(
                            $"{e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                        statusCode = (int) e.Status;
                        statusDescription = e.Message;
                    }
                }
                catch (Exception e)
                {
                    VLog.E(
                        $"{e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                    statusCode = ERROR_CODE_GENERAL;
                    statusDescription = e.Message;
                }
                finally
                {
                    response.Close();
                }
            }
            catch (WebException e)
            {
                statusCode = (int) e.Status;
                if (e.Response is HttpWebResponse errorResponse)
                {
                    statusCode = (int) errorResponse.StatusCode;
                    try
                    {
                        using (var errorStream = errorResponse.GetResponseStream())
                        {
                            if (errorStream != null)
                            {
                                using (StreamReader errorReader = new StreamReader(errorStream))
                                {
                                    stringResponse = errorReader.ReadToEnd();
                                    MainThreadCallback(() => onRawResponse?.Invoke(stringResponse));
                                    sentResponse = ProcessStringResponses(stringResponse);
                                }
                            }
                        }
                    }
                    catch (JSONParseException)
                    {
                        // Response wasn't encoded error, ignore it.
                    }
                    catch (Exception errorResponseError)
                    {
                        // We've already caught that there is an error, we'll ignore any errors
                        // reading error response data and use the status/original error for validation
                        VLog.W(errorResponseError);
                    }
                }

                statusDescription = e.Message;
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    VLog.E(
                        $"Http Request Failed [{statusCode}]: {e.Message}\nRequest Stack Trace:\n{callingStackTrace}\nResponse Stack Trace:\n{e.StackTrace}");
                }
                if (e.Response != null)
                {
                    e.Response.Close();
                }
            }
            finally
            {
                isActive = false;
            }

            CloseRequestStream();

            if (null != responseData)
            {
                var error = responseData["error"];
                if (!string.IsNullOrEmpty(error))
                {
                    statusDescription = $"Error: {responseData["code"]}. {error}";
                    statusCode = statusCode == 200 ? ERROR_CODE_GENERAL : statusCode;
                }
            }
            else if (statusCode == 200)
            {
                statusCode = ERROR_CODE_NO_DATA_FROM_SERVER;
                statusDescription = "Server did not return a valid json response.";
                VLog.W("No valid data was received from the server even though the request was successful. Actual potential response data: \n" +
                    stringResponse);
            }

            // Send final response if have not yet
            if (!sentResponse)
            {
                // Final transcription if not already sent
                string transcription = responseData.GetTranscription();
                if (!string.IsNullOrEmpty(transcription) && !responseData.GetIsFinal())
                {
                    MainThreadCallback(() => onFullTranscription?.Invoke(transcription));
                }
                // Final response
                SafeInvoke(onResponse);
            }

            // Complete
            responseStarted = false;
        }
        private string ReadToDelimiter(StreamReader reader, string delimiter)
        {
            // Allocate all vars
            StringBuilder results = new StringBuilder();
            int delLength = delimiter.Length;
            int i;
            bool found;
            char nextChar;

            // Iterate each byte in the stream
            while (reader != null && !reader.EndOfStream)
            {
                // Continue until found
                if (reader.Peek() == 0)
                {
                    continue;
                }

                // Append next character
                nextChar = (char)reader.Read();
                results.Append(nextChar);

                // Continue until long as delimiter
                if (results.Length < delLength)
                {
                    continue;
                }

                // Check if string builder ends with delimiter
                found = true;
                for (i=0;i<delLength;i++)
                {
                    // Stop checking if not delimiter
                    if (delimiter[i] != results[results.Length - delLength + i])
                    {
                        found = false;
                        break;
                    }
                }

                // Found delimiter
                if (found)
                {
                    return results.ToString(0, results.Length - delLength);
                }
            }

            // If no delimiter is found, return the rest of the chunk
            return results.Length == 0 ? null : results.ToString();
        }
        // Process individual piece
        private bool ProcessStringResponses(string stringResponse)
        {
            // Split by delimiter
            foreach (var stringPart in stringResponse.Split(new string[]{WitConstants.ENDPOINT_JSON_DELIMITER}, StringSplitOptions.RemoveEmptyEntries))
            {
                if (ProcessStringResponse(stringPart))
                {
                    return true;
                }
            }
            return false;
        }
        // Safely handles
        private bool ProcessStringResponse(string stringResponse)
        {
            // Decode full response
            responseData = WitResponseNode.Parse(stringResponse);

            // Handle responses
            bool hasResponse = responseData.HasResponse();
            bool final = responseData.GetIsFinal();

            // Return transcription
            string transcription = responseData.GetTranscription();
            if (!string.IsNullOrEmpty(transcription) && (!hasResponse || final))
            {
                // Call partial transcription
                if (!final)
                {
                    MainThreadCallback(() => onPartialTranscription?.Invoke(transcription));
                }
                // Call full transcription
                else
                {
                    MainThreadCallback(() => onFullTranscription?.Invoke(transcription));
                }
            }

            // No response
            if (!hasResponse)
            {
                return false;
            }

            // Call partial response
            SafeInvoke(onPartialResponse);

            // Call final response
            if (final)
            {
                SafeInvoke(onResponse);
            }

            // Return final
            return final;
        }
        private void HandleRequestStream(IAsyncResult ar)
        {
            try
            {
                StartResponse();
                var stream = _request.EndGetRequestStream(ar);
                bytesWritten = 0;

                if (null != postData)
                {
                    bytesWritten += postData.Length;
                    stream.Write(postData, 0, postData.Length);
                    CloseRequestStream();
                }
                else
                {
                    if (null == onInputStreamReady)
                    {
                        CloseRequestStream();
                    }
                    else
                    {
                        isRequestStreamActive = true;
                        SafeInvoke(onInputStreamReady);
                    }
                }

                _writeStream = stream;
            }
            catch (WebException e)
            {
                if (e.Status != WebExceptionStatus.RequestCanceled)
                {
                    statusCode = (int) e.Status;
                    statusDescription = e.Message;
                    SafeInvoke(onResponse);
                }
            }
        }

        private void SafeInvoke(Action<WitRequest> action)
        {
            if (action == null)
            {
                return;
            }
            MainThreadCallback(() =>
            {
                // We want to allow each invocation to run even if there is an exception thrown by one
                // of the callbacks in the invocation list. This protects shared invocations from
                // clients blocking things like UI updates from other parts of the sdk being invoked.
                foreach (Action<WitRequest> responseDelegate in action.GetInvocationList())
                {
                    try
                    {
                        responseDelegate.DynamicInvoke(this);
                    }
                    catch (Exception e)
                    {
                        VLog.E(e);
                    }
                }
            });
        }

        public void AbortRequest()
        {
            CloseActiveStream();
            if (null != _request)
            {
                _request.Abort();
                _request = null;
            }
            if (statusCode == 0)
            {
                statusCode = ERROR_CODE_ABORTED;
                statusDescription = "Request was aborted";
            }
            isActive = false;
        }

        /// <summary>
        /// Method to close the input stream of data being sent during the lifecycle of this request
        ///
        /// If a post method was used, this will need to be called before the request will complete.
        /// </summary>
        public void CloseRequestStream()
        {
            if (requestRequiresBody && bytesWritten == 0)
            {
                AbortRequest();
            }
            else
            {
                CloseActiveStream();
            }
        }

        private void CloseActiveStream()
        {
            lock (streamLock)
            {
                isRequestStreamActive = false;
                if (null != _writeStream)
                {
                    try
                    {
                        _writeStream.Close();
                    }
                    catch (Exception e)
                    {
                        VLog.W($"Write Stream - Close Failed\n{e}");
                    }
                    _writeStream = null;
                }
            }
        }

        /// <summary>
        /// Write request data to the Wit.ai post's body input stream
        ///
        /// Note: If the stream is not open (IsActive) this will throw an IOException.
        /// Data will be written synchronously. This should not be called from the main thread.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="offset"></param>
        /// <param name="length"></param>
        public void Write(byte[] data, int offset, int length)
        {
            // Ignore without write stream
            if (_writeStream == null)
            {
                return;
            }
            try
            {
                _writeStream.Write(data, offset, length);
                bytesWritten += length;
            }
            catch (ObjectDisposedException e)
            {
                // Handling edge case where stream is closed remotely
                // This problem occurs when the Web server resets or closes the connection after
                // the client application sends the HTTP header.
                // https://support.microsoft.com/en-us/topic/fix-you-receive-a-system-objectdisposedexception-exception-when-you-try-to-access-a-stream-object-that-is-returned-by-the-endgetrequeststream-method-in-the-net-framework-2-0-bccefe57-0a61-517a-5d5f-2dce0cc63265
                VLog.W($"Stream already disposed. It is likely the server reset the connection before streaming started.\n{e}");
                // This prevents a very long holdup on _writeStream.Close
                _writeStream = null;
            }
            catch (IOException e)
            {
                VLog.W(e.Message);
            }
            catch (Exception e)
            {
                VLog.E(e);
            }

            if (requestRequiresBody && bytesWritten == 0)
            {
                VLog.W("Stream was closed with no data written. Aborting request.");
                AbortRequest();
            }
        }

        #region CALLBACKS
        // Check performing
        private CoroutineUtility.CoroutinePerformer _performer = null;
        // All actions
        private ConcurrentQueue<Action> _mainThreadCallbacks = new ConcurrentQueue<Action>();

        // Called from background thread
        private void MainThreadCallback(Action action)
        {
            _mainThreadCallbacks.Enqueue(action);
        }
        // While active, perform any sent callbacks
        private void WatchMainThreadCallbacks()
        {
            // Ifnore if already performing
            if (_performer != null)
            {
                return;
            }

            // Check callbacks every frame (editor or runtime)
            _performer = CoroutineUtility.StartCoroutine(PerformMainThreadCallbacks());
        }
        // Every frame check for callbacks & perform any found
        private System.Collections.IEnumerator PerformMainThreadCallbacks()
        {
            // While checking, continue
            while (HasMainThreadCallbacks())
            {
                // Wait for frame
                if (Application.isPlaying && !Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }
                // Wait for a tick
                else
                {
                    yield return null;
                }

                // Perform if possible
                while (_mainThreadCallbacks.Count > 0 && _mainThreadCallbacks.TryDequeue(out var result))
                {
                    result();
                }
            }

            // Done performing
            _performer = null;
        }
        // Check actions
        private bool HasMainThreadCallbacks()
        {
            return IsActive || isRequestStreamActive || HasResponseStarted || _mainThreadCallbacks.Count > 0;
        }
        #endregion
    }
}
