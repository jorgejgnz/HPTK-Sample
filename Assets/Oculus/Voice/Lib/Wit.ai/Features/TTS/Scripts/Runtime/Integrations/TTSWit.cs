/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Meta.Voice.Audio;
using UnityEngine;
using UnityEngine.Serialization;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Json;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Events;
using Meta.WitAi.TTS.Interfaces;
using Meta.WitAi.Requests;

namespace Meta.WitAi.TTS.Integrations
{
    [Serializable]
    public class TTSWitVoiceSettings : TTSVoiceSettings
    {
        /// <summary>
        /// Default voice name used if no voice is provided
        /// </summary>
        public const string DEFAULT_VOICE = "Charlie";
        /// <summary>
        /// Default style used if no style is provided
        /// </summary>
        public const string DEFAULT_STYLE = "default";

        /// <summary>
        /// Unique voice name
        /// </summary>
        public string voice = DEFAULT_VOICE;
        /// <summary>
        /// Voice style (ex. formal, fast)
        /// </summary>
        public string style = DEFAULT_STYLE;
        /// <summary>
        /// Text-to-speech speed percentage
        /// </summary>
        [Range(50, 200)]
        public int speed = 100;
        /// <summary>
        /// Text-to-speech audio pitch percentage
        /// </summary>
        [Range(25, 200)]
        public int pitch = 100;

        /// <summary>
        /// Checks if request can be decoded for TTS data
        /// Example Data:
        /// {
        ///    "q": "Text to be spoken"
        ///    "voice": "Charlie
        /// }
        /// </summary>
        /// <param name="responseNode">The deserialized json data class</param>
        /// <returns>True if request can be decoded</returns>
        public static bool CanDecode(WitResponseNode responseNode)
        {
            return responseNode != null && responseNode.AsObject.HasChild(WitConstants.ENDPOINT_TTS_PARAM) && responseNode.AsObject.HasChild("voice");
        }
    }
    [Serializable]
    public struct TTSWitRequestSettings
    {
        public WitConfiguration configuration;
        public TTSWitAudioType audioType;
        public bool audioStream;
    }

    public class TTSWit : TTSService, ITTSVoiceProvider, ITTSWebHandler, IWitConfigurationProvider
    {
        #region TTSService
        // Voice provider
        public override ITTSVoiceProvider VoiceProvider => this;
        // Request handler
        public override ITTSWebHandler WebHandler => this;
        // Runtime cache handler
        public override ITTSRuntimeCacheHandler RuntimeCacheHandler
        {
            get
            {
                if (_runtimeCache == null)
                {
                    _runtimeCache = gameObject.GetComponent<ITTSRuntimeCacheHandler>();
                }
                return _runtimeCache;
            }
        }
        private ITTSRuntimeCacheHandler _runtimeCache;
        // Cache handler
        public override ITTSDiskCacheHandler DiskCacheHandler
        {
            get
            {
                if (_diskCache == null)
                {
                    _diskCache = gameObject.GetComponent<ITTSDiskCacheHandler>();
                }
                return _diskCache;
            }
        }
        private ITTSDiskCacheHandler _diskCache;

        // Web request events
        public TTSWebRequestEvents WebRequestEvents => Events.WebRequest;
        // Configuration provider
        public WitConfiguration Configuration => RequestSettings.configuration;

        // Use wit tts vrequest type
        protected override AudioType GetAudioType()
        {
            return WitTTSVRequest.GetAudioType(RequestSettings.audioType);
        }
        // Get tts request prior to transmission
        private WitTTSVRequest GetTtsRequest(TTSClipData clipData)
        {
            // Apply audio type
            clipData.audioType = GetAudioType();
            clipData.queryStream = RequestSettings.audioStream;

            // Return request
            return new WitTTSVRequest(RequestSettings.configuration, clipData.queryRequestId,
                clipData.textToSpeak, clipData.queryParameters,
                RequestSettings.audioType, clipData.queryStream,
                (progress) => OnRequestProgressUpdated(clipData, progress),
                () => OnRequestFirstResponse(clipData));
        }

        // Download progress callbacks
        private void OnRequestProgressUpdated(TTSClipData clipData, float newProgress)
        {
            if (clipData != null)
            {
                clipData.loadProgress = newProgress;
            }
        }

        // Progress callbacks
        private void OnRequestFirstResponse(TTSClipData clipData)
        {
            if (clipData != null)
            {
                WebRequestEvents?.OnRequestFirstResponse?.Invoke(clipData);
            }
        }
        #endregion

        #region ITTSWebHandler Streams
        // Request settings
        [Header("Web Request Settings")]
        [FormerlySerializedAs("_settings")]
        public TTSWitRequestSettings RequestSettings = new TTSWitRequestSettings
        {
            audioType = TTSWitAudioType.PCM,
            audioStream = true,
        };

        // Use settings web stream events
        public TTSStreamEvents WebStreamEvents { get; set; } = new TTSStreamEvents();

        // Requests bly clip id
        private Dictionary<string, VRequest> _webStreams = new Dictionary<string, VRequest>();

        // Whether TTSService is valid
        public override string GetInvalidError()
        {
            string invalidError = base.GetInvalidError();
            if (!string.IsNullOrEmpty(invalidError))
            {
                return invalidError;
            }
            if (RequestSettings.configuration == null)
            {
                return "No WitConfiguration Set";
            }
            if (string.IsNullOrEmpty(RequestSettings.configuration.GetClientAccessToken()))
            {
                return "No WitConfiguration Client Token";
            }
            return string.Empty;
        }
        // Ensures text can be sent to wit web service
        public string IsTextValid(string textToSpeak) => string.IsNullOrEmpty(textToSpeak) ? WitConstants.ENDPOINT_TTS_NO_TEXT : string.Empty;

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="onStreamSetupComplete">Stream setup complete: returns clip and error if applicable</param>
        public void RequestStreamFromWeb(TTSClipData clipData)
        {
            // Stream begin
            WebStreamEvents?.OnStreamBegin?.Invoke(clipData);

            // Check if valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebStreamEvents?.OnStreamError?.Invoke(clipData, validError);
                return;
            }
            // Ignore if already performing
            if (_webStreams.ContainsKey(clipData.clipID))
            {
                CancelWebStream(clipData);
            }

            // Begin request
            WebRequestEvents?.OnRequestBegin?.Invoke(clipData);

            // Whether to stream
            bool stream = Application.isPlaying && RequestSettings.audioStream;

            // Request tts
            WitTTSVRequest request = GetTtsRequest(clipData);
            request.RequestStream(clipData.clipStream,
                (clipStream, error) =>
                {
                    // Apply
                    _webStreams.Remove(clipData.clipID);

                    // Set new clip stream
                    clipData.clipStream = clipStream;

                    // Unloaded
                    if (clipData.loadState == TTSClipLoadState.Unloaded)
                    {
                        error = WitConstants.CANCEL_ERROR;
                        clipStream?.Unload();
                    }

                    // Error
                    if (!string.IsNullOrEmpty(error))
                    {
                        if (string.Equals(error, WitConstants.CANCEL_ERROR, StringComparison.CurrentCultureIgnoreCase))
                        {
                            WebStreamEvents?.OnStreamCancel?.Invoke(clipData);
                            WebRequestEvents?.OnRequestCancel?.Invoke(clipData);
                        }
                        else
                        {
                            WebStreamEvents?.OnStreamError?.Invoke(clipData, error);
                            WebRequestEvents?.OnRequestError?.Invoke(clipData, error);
                        }
                    }
                    // Success
                    else
                    {
                        WebStreamEvents?.OnStreamReady?.Invoke(clipData);
                        WebRequestEvents?.OnRequestReady?.Invoke(clipData);
                        if (!RequestSettings.audioStream || !WitTTSVRequest.CanStreamAudio(RequestSettings.audioType))
                        {
                            WebStreamEvents?.OnStreamComplete?.Invoke(clipData);
                            WebRequestEvents?.OnRequestComplete?.Invoke(clipData);
                        }
                    }
                });
            _webStreams[clipData.clipID] = request;
        }
        /// <summary>
        /// Cancel web stream
        /// </summary>
        /// <param name="clipID">Unique clip id</param>
        public bool CancelWebStream(TTSClipData clipData)
        {
            // Ignore without
            if (!_webStreams.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            VRequest request = _webStreams[clipData.clipID];
            _webStreams.Remove(clipData.clipID);

            // Destroy immediately
            request?.Cancel();
            request = null;

            // Call delegate
            WebStreamEvents?.OnStreamCancel?.Invoke(clipData);
            WebRequestEvents?.OnRequestCancel?.Invoke(clipData);

            // Success
            return true;
        }
        #endregion

        #region ITTSWebHandler Downloads
        // Use settings web download events
        public TTSDownloadEvents WebDownloadEvents { get; set; } = new TTSDownloadEvents();

        // Requests by clip id
        private Dictionary<string, WitVRequest> _webDownloads = new Dictionary<string, WitVRequest>();

        /// <summary>
        /// Method for performing a web load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        /// <param name="downloadPath">Path to save clip</param>
        public void RequestDownloadFromWeb(TTSClipData clipData, string downloadPath)
        {
            // Begin
            WebDownloadEvents?.OnDownloadBegin?.Invoke(clipData, downloadPath);

            // Ensure valid
            string validError = IsRequestValid(clipData, RequestSettings.configuration);
            if (!string.IsNullOrEmpty(validError))
            {
                WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, validError);
                return;
            }
            // Abort if already performing
            if (_webDownloads.ContainsKey(clipData.clipID))
            {
                CancelWebDownload(clipData, downloadPath);
            }

            // Begin request
            WebRequestEvents?.OnRequestBegin?.Invoke(clipData);

            // Request tts
            WitTTSVRequest request = GetTtsRequest(clipData);
            request.RequestDownload(downloadPath,
                (success, error) =>
                {
                    _webDownloads.Remove(clipData.clipID);
                    if (string.IsNullOrEmpty(error))
                    {
                        WebDownloadEvents?.OnDownloadSuccess?.Invoke(clipData, downloadPath);
                        WebRequestEvents?.OnRequestReady?.Invoke(clipData);
                    }
                    else
                    {
                        WebDownloadEvents?.OnDownloadError?.Invoke(clipData, downloadPath, error);
                        WebRequestEvents?.OnRequestError?.Invoke(clipData, error);
                    }
                    WebRequestEvents?.OnRequestComplete?.Invoke(clipData);
                });
            _webDownloads[clipData.clipID] = request;
        }
        /// <summary>
        /// Method for cancelling a running load request
        /// </summary>
        /// <param name="clipData">Clip request data</param>
        public bool CancelWebDownload(TTSClipData clipData, string downloadPath)
        {
            // Ignore if not performing
            if (!_webDownloads.ContainsKey(clipData.clipID))
            {
                return false;
            }

            // Get request
            WitVRequest request = _webDownloads[clipData.clipID];
            _webDownloads.Remove(clipData.clipID);

            // Destroy immediately
            request?.Cancel();
            request = null;

            // Download cancelled
            WebDownloadEvents?.OnDownloadCancel?.Invoke(clipData, downloadPath);
            WebRequestEvents?.OnRequestCancel?.Invoke(clipData);

            // Success
            return true;
        }
        #endregion

        #region ITTSVoiceProvider
        // Preset voice settings
        [Header("Voice Settings")]
        #if UNITY_2021_3_2 || UNITY_2021_3_3 || UNITY_2021_3_4 || UNITY_2021_3_5
        [NonReorderable]
        #endif
        [SerializeField] private TTSWitVoiceSettings[] _presetVoiceSettings;
        public TTSWitVoiceSettings[] PresetWitVoiceSettings => _presetVoiceSettings;

        // Cast to voice array
        public TTSVoiceSettings[] PresetVoiceSettings
        {
            get
            {
                if (_presetVoiceSettings == null)
                {
                    _presetVoiceSettings = new TTSWitVoiceSettings[] { };
                }
                return _presetVoiceSettings;
            }
        }
        // Default voice setting uses the first voice in the list
        public TTSVoiceSettings VoiceDefaultSettings => PresetVoiceSettings == null || PresetVoiceSettings.Length == 0 ? null : PresetVoiceSettings[0];

        #if UNITY_EDITOR
        // Apply settings
        public void SetVoiceSettings(TTSWitVoiceSettings[] newVoiceSettings)
        {
            _presetVoiceSettings = newVoiceSettings;
        }
        #endif

        // Convert voice settings into dictionary to be used with web requests
        private const string VOICE_KEY = "voice";
        private const string STYLE_KEY = "style";
        public Dictionary<string, string> EncodeVoiceSettings(TTSVoiceSettings voiceSettings)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            if (voiceSettings != null)
            {
                foreach (FieldInfo field in GetVoiceSettingsFields(voiceSettings))
                {
                    // Ensure field value exists
                    object fieldVal = field.GetValue(voiceSettings);
                    if (fieldVal != null)
                    {
                        // Clamp in between range
                        RangeAttribute range = field.GetCustomAttribute<RangeAttribute>();
                        if (range != null && field.FieldType == typeof(int))
                        {
                            int oldFloat = (int) fieldVal;
                            int newFloat = Mathf.Clamp(oldFloat, (int)range.min, (int)range.max);
                            if (oldFloat != newFloat)
                            {
                                fieldVal = newFloat;
                            }
                        }

                        // Apply
                        parameters[field.Name] = fieldVal.ToString();
                    }
                }

                // Set default if no voice is provided
                if (!parameters.ContainsKey(VOICE_KEY) || string.IsNullOrEmpty(parameters[VOICE_KEY]))
                {
                    parameters[VOICE_KEY] = TTSWitVoiceSettings.DEFAULT_VOICE;
                }
                // Set default if no style is given
                if (!parameters.ContainsKey(STYLE_KEY) || string.IsNullOrEmpty(parameters[STYLE_KEY]))
                {
                    parameters[STYLE_KEY] = TTSWitVoiceSettings.DEFAULT_STYLE;
                }
            }
            return parameters;
        }
        // Obtain all fields for a specific voice settings
        private static readonly Dictionary<Type, FieldInfo[]> _settingsFields = new Dictionary<Type, FieldInfo[]>();
        private static FieldInfo[] GetVoiceSettingsFields(TTSVoiceSettings voiceSettings)
        {
            // Return fields if already found
            Type settingsType = voiceSettings.GetType();
            if (_settingsFields.ContainsKey(settingsType))
            {
                return _settingsFields[settingsType];
            }

            // Get public/instance fields
            FieldInfo[] fields = settingsType.GetFields(BindingFlags.Public | BindingFlags.Instance);

            // Remove fields from TTSVoiceSettings
            Type baseType = typeof(TTSVoiceSettings);
            fields = fields.ToList().FindAll((field) => field.DeclaringType != baseType).ToArray();

            // Apply & return
            _settingsFields[settingsType] = fields;
            return fields;
        }
        // Returns an error if request is not valid
        private string IsRequestValid(TTSClipData clipData, WitConfiguration configuration)
        {
            // Invalid tts
            string invalidError = GetInvalidError();
            if (!string.IsNullOrEmpty(invalidError))
            {
                return invalidError;
            }
            // Invalid clip
            if (clipData == null)
            {
                return "No clip data provided";
            }
            // Success
            return string.Empty;
        }
        #endregion
    }
}
