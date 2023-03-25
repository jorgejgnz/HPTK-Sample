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
    internal interface IWitInfoVRequest
    {
        bool RequestAppId(VRequest.RequestCompleteDelegate<string> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestApps(int limit, int offset, VRequest.RequestCompleteDelegate<WitAppInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestAppInfo(string applicationId, VRequest.RequestCompleteDelegate<WitAppInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestClientAppToken(string applicationId, VRequest.RequestCompleteDelegate<string> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestIntentList(VRequest.RequestCompleteDelegate<WitIntentInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestIntentInfo(string intentId, VRequest.RequestCompleteDelegate<WitIntentInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestEntityList(VRequest.RequestCompleteDelegate<WitEntityInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestEntityInfo(string entityId, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestTraitList(VRequest.RequestCompleteDelegate<WitTraitInfo[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestTraitInfo(string traitId, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        bool RequestVoiceList(VRequest.RequestCompleteDelegate<Dictionary<string, WitVoiceInfo[]>> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Timeout in seconds
        /// </summary>
        int Timeout { get; set; }

        /// <summary>
        /// If request is currently being performed
        /// </summary>
        bool IsPerforming { get; }

        /// <summary>
        /// Current progress for get requests
        /// </summary>
        float Progress { get; }

        /// <summary>
        /// The configuration used for voice requests
        /// </summary>
        IWitRequestConfiguration Configuration { get; }

        /// <summary>
        /// Perform a generic request
        /// </summary>
        /// <param name="unityRequest">The unity request</param>
        /// <param name="onProgress"></param>
        /// <param name="onComplete"></param>
        /// <returns></returns>
        bool Request(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<UnityWebRequest> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

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
        bool RequestFile(Uri uri, VRequest.RequestCompleteDelegate<byte[]> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Download a file using a unityrequest
        /// </summary>
        /// <param name="unityRequest">The unity request to add a download handler to</param>
        /// <param name="onComplete">Called once download has completed</param>
        /// <param name="onProgress">Download progress delegate</param>
        bool RequestFileDownload(string downloadPath, UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<bool> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Checks if a file exists at a specified location
        /// </summary>
        /// <param name="checkPath">The local file path to be checked</param>
        /// <param name="onComplete">Called once check has completed.  Returns true if file exists</param>
        bool RequestFileExists(string checkPath, VRequest.RequestCompleteDelegate<bool> onComplete);

        /// <summary>
        /// Performs a text request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        bool RequestText(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<string> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Performs a json request & handles the resultant text
        /// </summary>
        /// <param name="unityRequest">The unity request performing the post or get</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestJson<TData>(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Perform a json get request with a specified uri
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        /// <returns></returns>
        bool RequestJson<TData>(Uri uri, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postData">The data to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestJson<TData>(Uri uri, byte[] postData, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Performs a json request by posting byte data
        /// </summary>
        /// <param name="uri">The uri to be requested</param>
        /// <param name="postText">The string to be uploaded</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The data upload progress</param>
        /// <typeparam name="TData">The struct or class to be deserialized to</typeparam>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestJson<TData>(Uri uri, string postText, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Request audio clip with url, type, progress delegate & ready delegate
        /// </summary>
        /// <param name="unityRequest">The unity request to add a download handler to</param>
        /// <param name="onClipReady">Called when the clip is ready for playback or has failed to load</param>
        /// <param name="audioType">The audio type requested (Wav, MP3, etc.)</param>
        /// <param name="audioStream">Whether or not audio should be streamed</param>
        /// <param name="onProgress">Clip progress callback</param>
        bool RequestAudioClip(UnityWebRequest unityRequest, VRequest.RequestCompleteDelegate<AudioClip> onClipReady,
            AudioType audioType = AudioType.UNKNOWN, bool audioStream = true,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Request audio clip with url, type, progress delegate & ready delegate
        /// </summary>
        /// <param name="uri">The url to be called</param>
        /// <param name="onClipReady">Called when the clip is ready for playback or has failed to load</param>
        /// <param name="audioType">The audio type requested (Wav, MP3, etc.)</param>
        /// <param name="audioStream">Whether or not audio should be streamed</param>
        /// <param name="onProgress">Clip progress callback</param>
        bool RequestAudioClip(Uri uri, VRequest.RequestCompleteDelegate<AudioClip> onClipReady,
            AudioType audioType = AudioType.UNKNOWN, bool audioStream = true,
            VRequest.RequestProgressDelegate onProgress = null);

        Uri GetUri(string path, Dictionary<string, string> queryParams = null);

        /// <summary>
        /// Get request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The download progress</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestWit<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);

        /// <summary>
        /// Post text request to a wit endpoint
        /// </summary>
        /// <param name="uriEndpoint">Endpoint name</param>
        /// <param name="uriParams">Endpoint url parameters</param>
        /// <param name="postText">Text to be sent to endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The upload progress</param>
        /// <returns>False if the request cannot be performed</returns>
        bool RequestWit<TData>(string uriEndpoint,
            Dictionary<string, string> uriParams, string postText, VRequest.RequestCompleteDelegate<TData> onComplete,
            VRequest.RequestProgressDelegate onProgress = null);
    }
}
