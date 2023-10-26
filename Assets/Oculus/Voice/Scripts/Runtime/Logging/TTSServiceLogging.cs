/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using Meta.WitAi;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using Meta.WitAi.TTS;
using Meta.WitAi.TTS.Data;
using Oculus.Voice.Core.Utilities;
using Oculus.Voice.Core.Bindings.Android.PlatformLogger;
using Oculus.Voice.Core.Bindings.Interfaces;
using UnityEngine;

namespace Oculus.Voice.Logging
{
    /// <summary>
    /// Script for logging various TTSService events
    /// </summary>
    public class TTSServiceLogging : MonoBehaviour
    {
        #region LOGGING
        /// <summary>
        /// Log output to console
        /// </summary>
        public bool EnableConsoleLogging = false;

        /// <summary>
        /// The service being observed
        /// </summary>
        public TTSService Service { get; private set; }

        // Logger
        private IVoiceSDKLogger _voiceSDKLoggerImpl;
        // Requests per request id
        private Dictionary<string, TTSServiceRequestLog> _requests = new Dictionary<string, TTSServiceRequestLog>();
        // Requests
        private struct TTSServiceRequestLog
        {
            public DateTime startTime;
            public Dictionary<string, string> annotations;
        }

        // Annotation ids
        private const string TTS_FILETYPE_ANNOTATION = "ttsFileType";
        private const string TTS_FILESTREAM_ANNOTATION = "ttsFileStream";
        private const string TTS_START_TIME_ANNOTATION = "ttsStartTime";
        private const string TTS_FIRST_TIME_ANNOTATION = "ttsFirstResponseTime";
        private const string TTS_READY_TIME_ANNOTATION = "ttsReadyTime";
        private const string TTS_FINISH_TIME_ANNOTATION = "ttsFinishedTime";
        private const string TTS_ERROR_ANNOTATION = "ttsError";

        // Add to every TTSService
        private void Awake()
        {
            Service = gameObject.GetComponent<TTSService>();
            InitLogger();
        }
        // Init logger
        private void InitLogger()
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            // PI Logger
            var loggerImpl = new VoiceSDKPlatformLoggerImpl();
            loggerImpl.Connect(VoiceSDKConstants.SdkVersion);
            _voiceSDKLoggerImpl = loggerImpl;
            #else
            // Console Logger
            _voiceSDKLoggerImpl = new VoiceSDKConsoleLoggerImpl();
            #endif

            // Get configuration
            WitConfiguration witConfig = Service.GetComponent<IWitConfigurationProvider>()?.Configuration;
            if (witConfig != null)
            {
                _voiceSDKLoggerImpl.WitApplication = witConfig.GetLoggerAppId();
            }
        }
        // Add events
        private void OnEnable()
        {
            if (_voiceSDKLoggerImpl != null)
            {
                _voiceSDKLoggerImpl.ShouldLogToConsole = EnableConsoleLogging;
            }
            if (Service)
            {
                Service.Events.WebRequest.OnRequestBegin.AddListener(OnRequestBegin);
                Service.Events.WebRequest.OnRequestCancel.AddListener(OnRequestCancel);
                Service.Events.WebRequest.OnRequestError.AddListener(OnRequestError);
                Service.Events.WebRequest.OnRequestFirstResponse.AddListener(OnRequestFirstResponse);
                Service.Events.WebRequest.OnRequestReady.AddListener(OnRequestReady);
                Service.Events.WebRequest.OnRequestComplete.AddListener(OnRequestComplete);
            }
        }
        // Remove events
        private void OnDisable()
        {
            if (Service)
            {
                Service.Events.WebRequest.OnRequestBegin.RemoveListener(OnRequestBegin);
                Service.Events.WebRequest.OnRequestCancel.RemoveListener(OnRequestCancel);
                Service.Events.WebRequest.OnRequestError.RemoveListener(OnRequestError);
                Service.Events.WebRequest.OnRequestFirstResponse.RemoveListener(OnRequestFirstResponse);
                Service.Events.WebRequest.OnRequestReady.RemoveListener(OnRequestReady);
                Service.Events.WebRequest.OnRequestComplete.RemoveListener(OnRequestComplete);
            }
        }

        private void OnRequestBegin(TTSClipData clipData)
        {
            LogStart(clipData);
        }
        private void OnRequestCancel(TTSClipData clipData)
        {
            LogComplete(clipData, "aborted");
        }
        private void OnRequestError(TTSClipData clipData, string error)
        {
            LogComplete(clipData, error);
        }
        private void OnRequestFirstResponse(TTSClipData clipData)
        {
            LogTimestamp(clipData, TTS_FIRST_TIME_ANNOTATION);
        }
        private void OnRequestReady(TTSClipData clipData)
        {
            LogTimestamp(clipData, TTS_READY_TIME_ANNOTATION);
        }
        private void OnRequestComplete(TTSClipData clipData)
        {
            LogComplete(clipData);
        }
        #endregion

        #region LOG HELPERS
        // Generate request data & apply initial annotations
        private void LogStart(TTSClipData clipData)
        {
            TTSServiceRequestLog requestData = GetRequestData(clipData);
            requestData.startTime = DateTime.Now;
            requestData.annotations = new Dictionary<string, string>();
            LogTimestamp(requestData, TTS_START_TIME_ANNOTATION);
            LogAnnotate(requestData, TTS_FILETYPE_ANNOTATION, AudioStreamHandler.GetDecodeType(clipData.audioType).ToString());
            LogAnnotate(requestData, TTS_FILESTREAM_ANNOTATION, clipData.queryStream.ToString(CultureInfo.InvariantCulture));
            _requests[clipData.queryRequestId] = requestData;
        }
        // Get request data
        private TTSServiceRequestLog GetRequestData(TTSClipData clipData)
        {
            if (_requests.ContainsKey(clipData.queryRequestId))
            {
                return _requests[clipData.queryRequestId];
            }
            return new TTSServiceRequestLog();
        }
        // Append the elapsed ms
        private void LogTimestamp(TTSClipData clipData, string key)
        {
            LogTimestamp(GetRequestData(clipData), key);
        }
        // Append the elapsed ms
        private void LogTimestamp(TTSServiceRequestLog requestData, string key)
        {
            LogAnnotate(requestData, key, DateTimeUtility.ElapsedMilliseconds.ToString());
        }
        // Append if possible
        private void LogAnnotate(TTSServiceRequestLog requestData, string key, string value)
        {
            if (requestData.annotations != null)
            {
                requestData.annotations[key] = value;
            }
        }
        // Append if possible
        private void LogComplete(TTSClipData clipData, string error = null)
        {
            // Get data & ensure annotations exist
            TTSServiceRequestLog requestData = GetRequestData(clipData);
            if (requestData.annotations == null)
            {
                return;
            }

            // Add error & completion time
            if (!string.IsNullOrEmpty(error))
            {
                LogAnnotate(requestData, TTS_ERROR_ANNOTATION, error);
            }
            LogTimestamp(requestData, TTS_FINISH_TIME_ANNOTATION);

            // Send full log
            if (_voiceSDKLoggerImpl != null)
            {
                _voiceSDKLoggerImpl.LogInteractionStart(clipData.queryRequestId, WitConstants.ENDPOINT_TTS);
                foreach (var key in requestData.annotations.Keys)
                {
                    _voiceSDKLoggerImpl.LogAnnotation(key, requestData.annotations[key]);
                }
                if (string.IsNullOrEmpty(error))
                {
                    _voiceSDKLoggerImpl.LogInteractionEndSuccess();
                }
                else
                {
                    _voiceSDKLoggerImpl.LogInteractionEndFailure(error);
                }
            }

            // Remove cache
            _requests.Remove(clipData.queryRequestId);
        }
        #endregion

        #region LOG GENERATION
        // Whether
        private static bool _initialized = false;

        // On load, add callback
        [RuntimeInitializeOnLoadMethod]
        private static void Init()
        {
            // Only initialize if not previously done
            if (_initialized)
            {
                return;
            }
            _initialized = true;

            // Add generation methods
            TTSService.OnServiceStart += OnServiceStart;
        }

        // When a TTSService is generated, add a TTSServiceLogging script
        private static void OnServiceStart(TTSService service)
        {
            if (service != null && service.GetComponent<TTSServiceLogging>() == null)
            {
                service.gameObject.AddComponent<TTSServiceLogging>();
            }
        }
        #endregion
    }
}
