/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Data.Info;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    internal interface IWitVRequest
    {
        /// <summary>
        /// Timeout in seconds
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// If request is currently being performed
        /// </summary>
        bool IsPerforming { get; }

        /// <summary>
        /// The configuration used for voice requests
        /// </summary>
        IWitRequestConfiguration Configuration { get; }

        /// <summary>
        /// Perform a generic request
        /// </summary>
        /// <param name="unityRequest">The unity request</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool Request(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<UnityWebRequest> onComplete);

        /// <summary>
        /// Clean the url prior to use
        /// </summary>
        string CleanUrl(string url);

        void Cancel();

        /// <summary>
        /// Performs a simple http header request
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="onComplete">Called once header lookup has completed</param>
        /// <returns></returns>
        bool RequestFileHeaders(Uri uri, VRequest.RequestCompleteDelegate<Dictionary<string, string>> onComplete);

        /// <summary>
        /// Performs a simple http header request
        /// </summary>
        /// <param name="uri">Uri to get a file</param>
        /// <param name="onComplete">Called once file data has been loaded</param>
        /// <returns>False if cannot begin request</returns>
        bool RequestFile(Uri uri, VRequest.RequestCompleteDelegate<byte[]> onComplete);

        /// <summary>
        /// Download a file using a unityrequest
        /// </summary>
        /// <param name="unityRequest">The unity request to add a download handler to</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestFileDownload(string downloadPath, UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<bool> onComplete);

        /// <summary>
        /// Checks if a file exists at a specified location
        /// </summary>
        /// <param name="checkPath">The local file path to be checked</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestFileExists(string checkPath, VRequest.RequestCompleteDelegate<bool> onComplete);

        /// <summary>
        /// Performs a text request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestText(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<string> onComplete);

        /// <summary>
        /// Performs a json request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        bool RequestJson<TData>(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Perform a json get request with a specified uri
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        bool RequestJsonGet<TData>(Uri uri, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postData">The data to be uploaded</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        bool RequestJsonPost<TData>(Uri uri, byte[] postData, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postText">The string to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestJsonPost<TData>(Uri uri, string postText, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="putData">The data to be uploaded</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        bool RequestJsonPut<TData>(Uri uri, byte[] putData, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Performs a json request by putting string
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="putText">The string to be uploaded</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        bool RequestJsonPut<TData>(Uri uri, string putText, VRequest.RequestCompleteDelegate<TData> onComplete);

        Uri GetUri(string path, Dictionary<string, string> queryParams = null);

        /// <summary>
        /// Get request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestWitGet<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Post text request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="postText">Text to be sent to endpoint</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestWitPost<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, string postText, VRequest.RequestCompleteDelegate<TData> onComplete);

        /// <summary>
        /// Put text request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="putText">Text to be sent to endpoint</param>
        /// <param name="onComplete">The callback delegate on request completion</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestWitPut<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, string putText, VRequest.RequestCompleteDelegate<TData> onComplete);
    }
}
