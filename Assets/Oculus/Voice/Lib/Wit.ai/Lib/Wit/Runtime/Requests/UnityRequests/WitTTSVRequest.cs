/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

// Uncomment when added to Wit.ai
//#define OGG_SUPPORT

using System;
using System.Text;
using System.Collections.Generic;
using Meta.Voice.Audio;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    // Supported audio types
    public enum TTSWitAudioType
    {
        PCM = 0,
        MPEG = 1,
        #if OGG_SUPPORT
        OGG = 3,
        #endif
        WAV = 2
    }
    public class WitTTSVRequest : WitVRequest
    {
        // The text to be requested
        public string TextToSpeak { get; }
        // The text settings
        public Dictionary<string, string> TtsData { get; }

        // The audio type to be used
        public TTSWitAudioType FileType { get; }
        // Whether audio should stream or not
        public bool Stream { get; }

        /// <summary>
        /// Constructor for wit based text-to-speech VRequests
        /// </summary>
        /// <param name="configuration">The configuration interface to be used</param>
        /// <param name="requestId">A unique identifier that can be used to track the request</param>
        /// <param name="textToSpeak">The text to be spoken by the request</param>
        /// <param name="ttsData">The text parameters used for the request</param>
        /// <param name="audioFileType">The expected audio file type of the request</param>
        /// <param name="audioStream">Whether the audio should be played while streaming or should wait until completion.</param>
        /// <param name="onDownloadProgress">The callback for progress related to downloading</param>
        /// <param name="onFirstResponse">The callback for the first response of data from a request</param>
        public WitTTSVRequest(IWitRequestConfiguration configuration, string requestId, string textToSpeak,
            Dictionary<string, string> ttsData, TTSWitAudioType audioFileType, bool audioStream = false,
            RequestProgressDelegate onDownloadProgress = null,
            RequestFirstResponseDelegate onFirstResponse = null)
            : base(configuration, requestId, false, onDownloadProgress, onFirstResponse)
        {
            TextToSpeak = textToSpeak;
            TtsData = ttsData;
            FileType = audioFileType;
            Stream = audioStream;
            Timeout = WitConstants.ENDPOINT_TTS_TIMEOUT;
        }

        // Add headers to all requests
        protected override Dictionary<string, string> GetHeaders()
        {
            Dictionary<string, string> headers = base.GetHeaders();
            headers[WitConstants.HEADER_POST_CONTENT] = "application/json";
            headers[WitConstants.HEADER_GET_CONTENT] = GetAudioMimeType(FileType);
            return headers;
        }

        /// <summary>
        /// Streams text to speech audio clip
        /// </summary>
        /// <param name="onClipReady">Clip ready to be played</param>
        /// <param name="onProgress">Clip load progress</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestStream(IAudioClipStream clipStream,
            RequestCompleteDelegate<IAudioClipStream> onClipReady)
        {
            // Error if no text is provided
            if (string.IsNullOrEmpty(TextToSpeak))
            {
                onClipReady?.Invoke(null, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }
            // Warn if incompatible with streaming
            if (Stream && !CanStreamAudio(FileType))
            {
                VLog.W($"Wit cannot stream {FileType} files please use {TTSWitAudioType.PCM} instead.");
            }

            // Async encode
            EncodePostBytesAsync(TextToSpeak, TtsData, (bytes) =>
            {
                // Get tts unity request
                UnityWebRequest unityRequest = GetUnityRequest(FileType, bytes);

                // Perform an audio stream request
                RequestAudioStream(clipStream, unityRequest, onClipReady, GetAudioType(FileType), Stream);
            });
            return true;
        }

        /// <summary>
        /// TTS streaming audio request
        /// </summary>
        /// <param name="downloadPath">Download path</param>
        /// <param name="onComplete">Clip completed download</param>
        /// <returns>False if request cannot be called</returns>
        public bool RequestDownload(string downloadPath,
            RequestCompleteDelegate<bool> onComplete)
        {
            // Error
            if (string.IsNullOrEmpty(TextToSpeak))
            {
                onComplete?.Invoke(false, WitConstants.ENDPOINT_TTS_NO_TEXT);
                return false;
            }

            // Async encode
            EncodePostBytesAsync(TextToSpeak, TtsData, (bytes) =>
            {
                // Get tts unity request
                UnityWebRequest unityRequest = GetUnityRequest(FileType, bytes);

                // Perform an audio stream request
                RequestFileDownload(downloadPath, unityRequest, onComplete);
            });
            return true;
        }

        // Encode post bytes async
        private void EncodePostBytesAsync(string textToSpeak, Dictionary<string, string> ttsData,
            Action<byte[]> onEncoded) => ThreadUtility.PerformInBackground(() => EncodePostData(textToSpeak, ttsData),
            (bytes, error) => onEncoded(bytes));

        // Encode tts post bytes
        private byte[] EncodePostData(string textToSpeak, Dictionary<string, string> ttsData)
        {
            ttsData[WitConstants.ENDPOINT_TTS_PARAM] = textToSpeak;
            string jsonString = JsonConvert.SerializeObject(ttsData);
            return Encoding.UTF8.GetBytes(jsonString);
        }

        // Internal base method for tts request
        private UnityWebRequest GetUnityRequest(TTSWitAudioType audioType, byte[] postData)
        {
            // Get uri
            Uri uri = GetUri(Configuration.GetEndpointInfo().Synthesize);

            // Generate request
            UnityWebRequest unityRequest = new UnityWebRequest(uri, UnityWebRequest.kHttpVerbPOST);

            // Add upload handler
            unityRequest.uploadHandler = new UploadHandlerRaw(postData);

            // Perform json request
            return unityRequest;
        }

        // Cast audio type
        public static AudioType GetAudioType(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                #if OGG_SUPPORT
                case TTSWitAudioType.OGG:
                    return AudioType.OGGVORBIS;
                #endif
                case TTSWitAudioType.MPEG:
                    return AudioType.MPEG;
                case TTSWitAudioType.WAV:
                    return AudioType.WAV;
                // Custom implementation
                case TTSWitAudioType.PCM:
                default:
                    return AudioType.UNKNOWN;
            }
        }
        // Get audio type
        public static string GetAudioMimeType(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                // PCM
                case TTSWitAudioType.PCM:
                    return "audio/raw";
                #if OGG_SUPPORT
                // OGG
                case TTSWitAudioType.OGG:
                #endif
                // MP3 & WAV
                case TTSWitAudioType.MPEG:
                case TTSWitAudioType.WAV:
                default:
                    return $"audio/{witAudioType.ToString().ToLower()}";
            }
        }
        // Get audio extension
        public static string GetAudioExtension(TTSWitAudioType witAudioType) => GetAudioExtension(GetAudioType(witAudioType));
        // Get audio extension
        public static string GetAudioExtension(AudioType audioType)
        {
            switch (audioType)
            {
                // PCM
                case AudioType.UNKNOWN:
                    return "raw";
                // OGG
                case AudioType.OGGVORBIS:
                    return "ogg";
                // MP3
                case AudioType.MPEG:
                    return "mp3";
                // WAV
                case AudioType.WAV:
                    return "wav";
                default:
                    VLog.W($"Attempting to process unsupported audio type: {audioType}");
                    return audioType.ToString().ToLower();
            }
        }
        // Whether streamed audio is allowed by unity
        public static bool CanStreamAudio(TTSWitAudioType witAudioType)
        {
            switch (witAudioType)
            {
                // Raw PCM: Supported by Wit.ai & custom unity implementation (DownloadHandlerRawPCM)
                case TTSWitAudioType.PCM:
                    return true;
                #if OGG_SUPPORT
                // OGG: Supported by Unity (DownloadHandlerAudioClip) but not by Wit.ai
                case TTSWitAudioType.OGG:
                    return true;
                #endif
                // MP3: Supported by Wit.ai but not by Unity (DownloadHandlerAudioClip)
                case TTSWitAudioType.MPEG:
                    return false;
                // WAV: does not support streaming
                case TTSWitAudioType.WAV:
                default:
                    return false;
            }
        }
    }
}
