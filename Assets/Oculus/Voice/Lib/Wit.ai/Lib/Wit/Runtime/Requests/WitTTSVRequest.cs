/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using System.Collections.Generic;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    public class WitTTSVRequest : WitVRequest
    {
        // Audio type for tts
        public static AudioType TTSAudioType = AudioType.WAV;

        // Constructor
        public WitTTSVRequest(IWitRequestConfiguration configuration) : base(configuration, false)
        {
            Timeout = WitConstants.ENDPOINT_TTS_TIMEOUT;
        }

        // Internal base method for tts request
        private UnityWebRequest GetUnityRequest(string textToSpeak,
            Dictionary<string, string> ttsData)
        {
            // Get uri
            Uri uri = GetUri(WitConstants.ENDPOINT_TTS);

            // Generate request
            UnityWebRequest unityRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);
            unityRequest.SetRequestHeader(WitConstants.HEADER_POST_CONTENT, "application/json");
            unityRequest.SetRequestHeader(WitConstants.HEADER_GET_CONTENT, $"audio/{TTSAudioType.ToString().ToLower()}");

            // Add upload handler
            ttsData[WitConstants.ENDPOINT_TTS_PARAM] = textToSpeak;
            string jsonString = JsonConvert.SerializeObject(ttsData);
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonString);
            unityRequest.uploadHandler = new UploadHandlerRaw(jsonBytes);

            // Perform json request
            return unityRequest;
        }

        /// <summary>
        /// TTS streaming audio request
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken</param>
        /// <param name="ttsData">Info on tts voice settings</param>
        /// <param name="onClipReady">Clip ready to be played</param>
        /// <param name="onProgress">Clip load progress</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestStream(string textToSpeak, Dictionary<string, string> ttsData,
            RequestCompleteDelegate<AudioClip> onClipReady,
            RequestProgressDelegate onProgress = null)
        {
            // Error
            if (string.IsNullOrEmpty(textToSpeak))
            {
                onClipReady?.Invoke(null, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }

            // Get tts unity request
            UnityWebRequest unityRequest = GetUnityRequest(textToSpeak, ttsData);

            // Perform an audio stream request
            return RequestAudioClip(unityRequest, onClipReady, TTSAudioType, true, onProgress);
        }

        /// <summary>
        /// TTS streaming audio request
        /// </summary>
        /// <param name="downloadPath">Download path</param>
        /// <param name="textToSpeak">Text to be spoken</param>
        /// <param name="ttsData">Info on tts voice settings</param>
        /// <param name="onComplete">Clip completed download</param>
        /// <param name="onProgress">Clip load progress</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestDownload(string downloadPath,
            string textToSpeak, Dictionary<string, string> ttsData,
            RequestCompleteDelegate<bool> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Error
            if (string.IsNullOrEmpty(textToSpeak))
            {
                onComplete?.Invoke(false, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }

            // Get tts unity request
            UnityWebRequest unityRequest = GetUnityRequest(textToSpeak, ttsData);

            // Perform a file download request
            return RequestFileDownload(downloadPath, unityRequest, onComplete, onProgress);
        }
    }
}
