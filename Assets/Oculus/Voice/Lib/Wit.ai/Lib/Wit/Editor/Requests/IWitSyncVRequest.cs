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
    internal interface IWitSyncVRequest
    {
        /// <summary>
        /// Submits an intent to be added to the current wit app
        /// </summary>
        /// <param name="intentInfo">The intent data to be submitted</param>
        /// <param name="onComplete">On completion that returns an intent with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddIntent(WitIntentInfo intentInfo, VRequest.RequestCompleteDelegate<WitIntentInfo> onComplete);

        /// <summary>
        /// Submits an entity to be added to the current wit app
        /// </summary>
        /// <param name="entityInfo">The entity info to be submitted</param>
        /// <param name="onComplete">On completion that returns an entity with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddEntity(WitEntityInfo entityInfo, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a keyword to be added to an entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity this keyword should be added to</param>
        /// <param name="keywordInfo">The keyword & synonyms submitted</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddEntityKeyword(string entityId,
            WitEntityKeywordInfo keywordInfo, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a synonym to be added to a keyword on the specified entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity that holds the keyword</param>
        /// <param name="keyword">The keyword we're adding the synonym to</param>
        /// <param name="synonym">The synonym we're adding</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddSynonym(string entityId, string keyword, string synonym,
            VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        /// <summary>
        /// Submits a trait to be added to the current wit app
        /// </summary>
        /// <param name="traitInfo">The trait data to be submitted</param>
        /// <param name="onComplete">On completion that returns a trait with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddTrait(WitTraitInfo traitInfo, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete);

        /// <summary>
        /// Submits a trait value to be added to the current wit app
        /// </summary>
        /// <param name="traitId">The trait id to be submitted</param>
        /// <param name="traitValue">The trait value to be submitted</param>
        /// <param name="onComplete">On completion callback that returns updated trait if successful</param>
        /// <returns>False if fails to make request</returns>
        bool RequestAddTraitValue(string traitId,
            string traitValue, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete);

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
