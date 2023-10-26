/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Linq;
using Meta.Voice.Audio;
using Meta.WitAi.Requests;
using UnityEngine;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Events;
using Meta.WitAi.TTS.Interfaces;

namespace Meta.WitAi.TTS
{
    public abstract class TTSService : MonoBehaviour
    {
        #region SETUP
        // Accessor
        public static TTSService Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Get all services
                    TTSService[] services = Resources.FindObjectsOfTypeAll<TTSService>();
                    if (services != null)
                    {
                        // Set as first instance that isn't a prefab
                        _instance = Array.Find(services, (o) => o.gameObject.scene.rootCount != 0);
                    }
                }
                return _instance;
            }
        }
        private static TTSService _instance;

        /// <summary>
        /// Audio system to be used for streaming & playback
        /// </summary>
        public IAudioSystem AudioSystem
        {
            get
            {
                if (_audioSystem == null && Application.isPlaying)
                {
                    // Search on TTS Service
                    _audioSystem = gameObject.GetComponent<IAudioSystem>();
                    if (_audioSystem == null)
                    {
                        // Add default unity audio system if not found
                        _audioSystem = gameObject.AddComponent<UnityAudioSystem>();
                    }
                }
                return _audioSystem;
            }
            set => _audioSystem = value;
        }
        private IAudioSystem _audioSystem;

        // Handles TTS runtime cache
        public abstract ITTSRuntimeCacheHandler RuntimeCacheHandler { get; }
        // Handles TTS cache requests
        public abstract ITTSDiskCacheHandler DiskCacheHandler { get; }
        // Handles TTS web requests
        public abstract ITTSWebHandler WebHandler { get; }
        // Handles TTS voice presets
        public abstract ITTSVoiceProvider VoiceProvider { get; }

        /// <summary>
        /// Static event called whenever any TTSService.Awake is called
        /// </summary>
        public static event Action<TTSService> OnServiceStart;
        /// <summary>
        /// Static event called whenever any TTSService.OnDestroy is called
        /// </summary>
        public static event Action<TTSService> OnServiceDestroy;

        /// <summary>
        /// Returns error if invalid
        /// </summary>
        public virtual string GetInvalidError()
        {
            if (WebHandler == null)
            {
                return "Web Handler Missing";
            }
            if (VoiceProvider == null)
            {
                return "Voice Provider Missing";
            }
            return string.Empty;
        }

        // Handles TTS events
        public TTSServiceEvents Events => _events;
        [Header("Event Settings")]
        [SerializeField] private TTSServiceEvents _events = new TTSServiceEvents();

        // Set instance
        protected virtual void Awake()
        {
            _instance = this;
            _delegates = false;
        }
        // Call event
        protected virtual void Start()
        {
            OnServiceStart?.Invoke(this);
        }
        // Log if invalid
        protected virtual void OnEnable()
        {
            string validError = GetInvalidError();
            if (!string.IsNullOrEmpty(validError))
            {
                VLog.W(validError);
            }
        }
        // Remove delegates
        protected virtual void OnDisable()
        {
            RemoveDelegates();
        }
        // Add delegates
        private bool _delegates = false;
        protected virtual void AddDelegates()
        {
            // Ignore if already added
            if (_delegates)
            {
                return;
            }
            _delegates = true;

            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.OnClipAdded.AddListener(OnRuntimeClipAdded);
                RuntimeCacheHandler.OnClipRemoved.AddListener(OnRuntimeClipRemoved);
            }
            if (DiskCacheHandler != null)
            {
                DiskCacheHandler.DiskStreamEvents.OnStreamBegin.AddListener(OnDiskStreamBegin);
                DiskCacheHandler.DiskStreamEvents.OnStreamCancel.AddListener(OnDiskStreamCancel);
                DiskCacheHandler.DiskStreamEvents.OnStreamReady.AddListener(OnDiskStreamReady);
                DiskCacheHandler.DiskStreamEvents.OnStreamError.AddListener(OnDiskStreamError);
            }
            if (WebHandler != null)
            {
                WebHandler.WebStreamEvents.OnStreamBegin.AddListener(OnWebStreamBegin);
                WebHandler.WebStreamEvents.OnStreamCancel.AddListener(OnWebStreamCancel);
                WebHandler.WebStreamEvents.OnStreamReady.AddListener(OnWebStreamReady);
                WebHandler.WebStreamEvents.OnStreamError.AddListener(OnWebStreamError);
                WebHandler.WebDownloadEvents.OnDownloadBegin.AddListener(OnWebDownloadBegin);
                WebHandler.WebDownloadEvents.OnDownloadCancel.AddListener(OnWebDownloadCancel);
                WebHandler.WebDownloadEvents.OnDownloadSuccess.AddListener(OnWebDownloadSuccess);
                WebHandler.WebDownloadEvents.OnDownloadError.AddListener(OnWebDownloadError);
            }
        }
        // Remove delegates
        protected virtual void RemoveDelegates()
        {
            // Ignore if not yet added
            if (!_delegates)
            {
                return;
            }
            _delegates = false;

            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.OnClipAdded.RemoveListener(OnRuntimeClipAdded);
                RuntimeCacheHandler.OnClipRemoved.RemoveListener(OnRuntimeClipRemoved);
            }
            if (DiskCacheHandler != null)
            {
                DiskCacheHandler.DiskStreamEvents.OnStreamBegin.RemoveListener(OnDiskStreamBegin);
                DiskCacheHandler.DiskStreamEvents.OnStreamCancel.RemoveListener(OnDiskStreamCancel);
                DiskCacheHandler.DiskStreamEvents.OnStreamReady.RemoveListener(OnDiskStreamReady);
                DiskCacheHandler.DiskStreamEvents.OnStreamError.RemoveListener(OnDiskStreamError);
            }
            if (WebHandler != null)
            {
                WebHandler.WebStreamEvents.OnStreamBegin.RemoveListener(OnWebStreamBegin);
                WebHandler.WebStreamEvents.OnStreamCancel.RemoveListener(OnWebStreamCancel);
                WebHandler.WebStreamEvents.OnStreamReady.RemoveListener(OnWebStreamReady);
                WebHandler.WebStreamEvents.OnStreamError.RemoveListener(OnWebStreamError);
                WebHandler.WebDownloadEvents.OnDownloadBegin.RemoveListener(OnWebDownloadBegin);
                WebHandler.WebDownloadEvents.OnDownloadCancel.RemoveListener(OnWebDownloadCancel);
                WebHandler.WebDownloadEvents.OnDownloadSuccess.RemoveListener(OnWebDownloadSuccess);
                WebHandler.WebDownloadEvents.OnDownloadError.RemoveListener(OnWebDownloadError);
            }
        }
        // Remove instance
        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
            UnloadAll();
            OnServiceDestroy?.Invoke(this);
        }

        /// <summary>
        /// Get clip log data
        /// </summary>
        protected virtual string GetClipLog(string logMessage, TTSClipData clipData)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(logMessage);
            if (clipData != null)
            {
                builder.AppendLine($"Voice: {(clipData.voiceSettings == null ? "Default" : clipData.voiceSettings.SettingsId)}");
                builder.AppendLine($"Text: {clipData.textToSpeak}");
                builder.AppendLine($"ID: {clipData.clipID}");
                TTSDiskCacheLocation cacheLocation = TTSDiskCacheLocation.Stream;
                if (DiskCacheHandler != null)
                {
                    TTSDiskCacheSettings settings = clipData.diskCacheSettings;
                    if (settings == null)
                    {
                        settings = DiskCacheHandler.DiskCacheDefaultSettings;
                    }
                    if (settings != null)
                    {
                        cacheLocation = settings.DiskCacheLocation;
                    }
                }
                builder.AppendLine($"Cache: {cacheLocation}");
                builder.AppendLine($"Type: {clipData.audioType}");
                if (clipData.clipStream != null)
                {
                    builder.AppendLine($"Length: {clipData.clipStream.Length:0.00} seconds");
                }
            }
            return builder.ToString();
        }
        #endregion

        #region HELPERS
        // Frequently used keys
        private const string CLIP_ID_DELIM = "|";
        private readonly SHA256 CLIP_HASH = SHA256.Create();

        /// <summary>
        /// Gets the text to be spoken after applying all relevant voice settings.
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken by a particular voice</param>
        /// <param name="voiceSettings">Voice settings to be used</param>
        /// <returns>Returns a the final text to be spoken</returns>
        public string GetFinalText(string textToSpeak, TTSVoiceSettings voiceSettings)
        {
            StringBuilder result = new StringBuilder();
            AppendFinalText(result, textToSpeak, voiceSettings);
            return result.ToString();
        }
        // Finalize text using a string builder
        protected virtual void AppendFinalText(StringBuilder builder, string textToSpeak, TTSVoiceSettings voiceSettings)
        {
            if (!string.IsNullOrEmpty(voiceSettings?.PrependedText))
            {
                builder.Append(voiceSettings.PrependedText);
            }
            builder.Append(textToSpeak);
            if (!string.IsNullOrEmpty(voiceSettings?.AppendedText))
            {
                builder.Append(voiceSettings.AppendedText);
            }
        }

        /// <summary>
        /// Obtain unique id for clip data
        /// </summary>
        public virtual string GetClipID(string textToSpeak, TTSVoiceSettings voiceSettings)
        {
            // Get a text string for a unique id
            StringBuilder uniqueId = new StringBuilder();
            // Add all data items
            if (VoiceProvider != null)
            {
                Dictionary<string, string> data = VoiceProvider.EncodeVoiceSettings(voiceSettings);
                foreach (var key in data.Keys)
                {
                    string keyClean = data[key].Replace(CLIP_ID_DELIM, "");
                    uniqueId.Append(keyClean);
                    uniqueId.Append(CLIP_ID_DELIM);
                }
            }
            // Finally, add unique id
            AppendFinalText(uniqueId, textToSpeak, voiceSettings);
            // Return id
            return GetSha256Hash(CLIP_HASH, uniqueId.ToString().ToLower());
        }

        private string GetSha256Hash(SHA256 shaHash, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = shaHash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
        /// <summary>
        /// Creates new clip data or returns existing cached clip
        /// </summary>
        /// <param name="textToSpeak">Text to speak</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Voice settings</param>
        /// <param name="diskCacheSettings">Disk Cache settings</param>
        /// <returns>Clip data structure</returns>
        protected virtual TTSClipData CreateClipData(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings)
        {
            // Use default voice settings if none are set
            if (voiceSettings == null && VoiceProvider != null)
            {
                voiceSettings = VoiceProvider.VoiceDefaultSettings;
            }
            // Use default disk cache settings if none are set
            if (diskCacheSettings == null && DiskCacheHandler != null)
            {
                diskCacheSettings = DiskCacheHandler.DiskCacheDefaultSettings;
            }
            // Determine clip id if empty
            if (string.IsNullOrEmpty(clipID))
            {
                clipID = GetClipID(textToSpeak, voiceSettings);
            }

            // Get clip from runtime cache if applicable
            TTSClipData clipData = GetRuntimeCachedClip(clipID);
            if (clipData != null)
            {
                return clipData;
            }

            // Generate new clip data
            clipData = new TTSClipData()
            {
                clipID = clipID,
                audioType = GetAudioType(),
                textToSpeak = GetFinalText(textToSpeak, voiceSettings),
                voiceSettings = voiceSettings,
                diskCacheSettings = diskCacheSettings,
                loadState = TTSClipLoadState.Unloaded,
                loadProgress = 0f,
                queryParameters = VoiceProvider?.EncodeVoiceSettings(voiceSettings),
                queryStream = false,
                clipStream = CreateClipStream()
            };

            // Return generated clip
            return clipData;
        }
        // Generate a new audio clip stream
        protected virtual IAudioClipStream CreateClipStream()
        {
            // Default
            if (AudioSystem == null)
            {
                return new UnityAudioClipStream(WitConstants.ENDPOINT_TTS_CHANNELS, WitConstants.ENDPOINT_TTS_SAMPLE_RATE, 0.1f);
            }

            // Get audio clip via audio system
            return AudioSystem.GetAudioClipStream(WitConstants.ENDPOINT_TTS_CHANNELS,
                WitConstants.ENDPOINT_TTS_SAMPLE_RATE);
        }
        // Get audio type
        protected virtual AudioType GetAudioType()
        {
            return AudioType.WAV;
        }
        // Set clip state
        protected virtual void SetClipLoadState(TTSClipData clipData, TTSClipLoadState loadState)
        {
            clipData.loadState = loadState;
            clipData.onStateChange?.Invoke(clipData, clipData.loadState);
        }
        #endregion

        #region LOAD
        // TTS Request options
        public TTSClipData Load(string textToSpeak, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, null, null, onStreamReady);
        public TTSClipData Load(string textToSpeak, string presetVoiceId, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), null, onStreamReady);
        public TTSClipData Load(string textToSpeak, string presetVoiceId, TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), diskCacheSettings, onStreamReady);
        public TTSClipData Load(string textToSpeak, TTSVoiceSettings voiceSettings, TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady = null) => Load(textToSpeak, null, voiceSettings, diskCacheSettings, onStreamReady);

        /// <summary>
        /// Perform a request for a TTS audio clip
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom cache settings</param>
        /// <returns>Generated TTS clip data</returns>
        public virtual TTSClipData Load(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string> onStreamReady)
        {
            // Add delegates if needed
            AddDelegates();

            // Get clip data
            TTSClipData clipData = CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings);
            if (clipData == null)
            {
                VLog.E("No clip provided");
                onStreamReady?.Invoke(clipData, "No clip provided");
                return null;
            }

            if (string.IsNullOrEmpty(clipData.textToSpeak))
            {
                clipData.loadState = TTSClipLoadState.Loaded;
            }

            // From Runtime Cache
            if (clipData.loadState != TTSClipLoadState.Unloaded)
            {
                // Add callback
                if (onStreamReady != null)
                {
                    // Call once ready
                    if (clipData.loadState == TTSClipLoadState.Preparing)
                    {
                        clipData.onPlaybackReady += (e) => onStreamReady(clipData, e);
                    }
                    // Call after return
                    else
                    {
                        CoroutineUtility.StartCoroutine(CallAfterAMoment(() => onStreamReady(clipData,
                            clipData.loadState == TTSClipLoadState.Loaded ? string.Empty : "Error")));
                    }
                }

                // Return clip
                return clipData;
            }

            // Add to runtime cache if possible
            if (RuntimeCacheHandler != null)
            {
                if (!RuntimeCacheHandler.AddClip(clipData))
                {
                    // Add callback
                    if (onStreamReady != null)
                    {
                        // Call once ready
                        if (clipData.loadState == TTSClipLoadState.Preparing)
                        {
                            clipData.onPlaybackReady += (e) => onStreamReady(clipData, e);
                        }
                        // Call after return
                        else
                        {
                            CoroutineUtility.StartCoroutine(CallAfterAMoment(() => onStreamReady(clipData,
                                clipData.loadState == TTSClipLoadState.Loaded ? string.Empty : "Error")));
                        }
                    }

                    // Return clip
                    return clipData;
                }
            }
            // Load begin
            else
            {
                OnLoadBegin(clipData);
            }

            // Add on ready delegate
            clipData.onPlaybackReady += (error) => onStreamReady?.Invoke(clipData, error);

            // Wait a moment and load
            CoroutineUtility.StartCoroutine(CallAfterAMoment(() =>
            {
                // Check for invalid text
                string invalidError = WebHandler.IsTextValid(clipData.textToSpeak);
                if (!string.IsNullOrEmpty(invalidError))
                {
                    OnWebStreamError(clipData, invalidError);
                    return;
                }

                // If should cache to disk, attempt to do so
                if (ShouldCacheToDisk(clipData))
                {
                    // Download was canceled before starting
                    if (clipData.loadState != TTSClipLoadState.Preparing)
                    {
                        string downloadPath = DiskCacheHandler.GetDiskCachePath(clipData);
                        OnWebDownloadBegin(clipData, downloadPath);
                        OnWebDownloadCancel(clipData, downloadPath);
                        OnWebStreamBegin(clipData);
                        OnWebStreamCancel(clipData);
                        return;
                    }

                    // Download
                    DownloadToDiskCache(clipData, (clipData2, downloadPath, error) =>
                    {
                        // Download was canceled before starting
                        if (string.Equals(error, WitConstants.CANCEL_ERROR))
                        {
                            OnWebStreamBegin(clipData);
                            OnWebStreamCancel(clipData);
                            return;
                        }
                        // Not in cache & cannot download
                        if (string.Equals(error, WitConstants.ERROR_TTS_CACHE_DOWNLOAD))
                        {
                            WebHandler?.RequestStreamFromWeb(clipData);
                            return;
                        }
                        // Download failed, throw error
                        if (!string.IsNullOrEmpty(error))
                        {
                            OnWebStreamBegin(clipData);
                            OnWebStreamError(clipData, error);
                            return;
                        }

                        // Stream from Cache
                        DiskCacheHandler?.StreamFromDiskCache(clipData);
                    });
                }
                // Simply stream from the web
                else
                {
                    // Stream was canceled before starting
                    if (clipData.loadState != TTSClipLoadState.Preparing)
                    {
                        OnWebStreamBegin(clipData);
                        OnWebStreamCancel(clipData);
                        return;
                    }

                    // Stream
                    WebHandler?.RequestStreamFromWeb(clipData);
                }
            }));

            // Return data
            return clipData;
        }
        // Wait a moment
        private IEnumerator CallAfterAMoment(Action call)
        {
            if (Application.isPlaying)
            {
                yield return new WaitForEndOfFrame();
            }
            else
            {
                yield return null;
            }
            call();
        }
        // Load begin
        private void OnLoadBegin(TTSClipData clipData)
        {
            // Now preparing
            SetClipLoadState(clipData, TTSClipLoadState.Preparing);

            // Begin load
            VLog.I(GetClipLog("Load Clip", clipData));
            Events?.OnClipCreated?.Invoke(clipData);
        }
        // Handle begin of disk cache streaming
        private void OnDiskStreamBegin(TTSClipData clipData) => OnStreamBegin(clipData, true);
        private void OnWebStreamBegin(TTSClipData clipData) => OnStreamBegin(clipData, false);
        private void OnStreamBegin(TTSClipData clipData, bool fromDisk)
        {
            // Set delegates for clip stream update/completion
            if (clipData.clipStream != null)
            {
                clipData.clipStream.OnStreamUpdated = (cs) => OnStreamUpdated(clipData, cs, fromDisk);
                clipData.clipStream.OnStreamComplete = (cs) => OnStreamComplete(clipData, cs, fromDisk);
            }

            // Callback delegate
            VLog.I(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Begin", clipData));
            Events?.Stream?.OnStreamBegin?.Invoke(clipData);
        }
        // Handle cancel of disk cache streaming
        private void OnDiskStreamCancel(TTSClipData clipData) => OnStreamCancel(clipData, true);
        private void OnWebStreamCancel(TTSClipData clipData) => OnStreamCancel(clipData, false);
        private void OnStreamCancel(TTSClipData clipData, bool fromDisk)
        {
            // Handled as an error
            SetClipLoadState(clipData, TTSClipLoadState.Error);

            // Invoke
            clipData.onPlaybackReady?.Invoke(WitConstants.CANCEL_ERROR);
            clipData.onPlaybackReady = null;

            // Callback delegate
            VLog.I(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Canceled", clipData));
            Events?.Stream?.OnStreamCancel?.Invoke(clipData);

            // Unload clip
            Unload(clipData);
        }
        // Handle disk cache streaming error
        private void OnDiskStreamError(TTSClipData clipData, string error) => OnStreamError(clipData, error, true);
        private void OnWebStreamError(TTSClipData clipData, string error) => OnStreamError(clipData, error, false);
        private void OnStreamError(TTSClipData clipData, string error, bool fromDisk)
        {
            // Cancelled
            if (error.Equals(WitConstants.CANCEL_ERROR))
            {
                OnStreamCancel(clipData, fromDisk);
                return;
            }

            // Error
            SetClipLoadState(clipData, TTSClipLoadState.Error);

            // Invoke playback is ready
            clipData.onPlaybackReady?.Invoke(error);
            clipData.onPlaybackReady = null;

            // Stream error
            VLog.E(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Error\nError: {error}", clipData));
            Events?.Stream?.OnStreamError?.Invoke(clipData, error);

            // Unload clip
            Unload(clipData);
        }
        // Handle successful completion of disk cache streaming
        private void OnDiskStreamReady(TTSClipData clipData) => OnStreamReady(clipData, true);
        private void OnWebStreamReady(TTSClipData clipData) => OnStreamReady(clipData, false);
        private void OnStreamReady(TTSClipData clipData, bool fromDisk)
        {
            // Refresh cache for file size
            if (RuntimeCacheHandler != null)
            {
                // Stop forcing an unload if runtime cache update fails
                RuntimeCacheHandler.OnClipRemoved.RemoveListener(OnRuntimeClipRemoved);
                bool failed = !RuntimeCacheHandler.AddClip(clipData);
                RuntimeCacheHandler.OnClipRemoved.AddListener(OnRuntimeClipRemoved);

                // Handle fail directly
                if (failed)
                {
                    OnStreamError(clipData, "Removed from runtime cache due to file size", fromDisk);
                    OnRuntimeClipRemoved(clipData);
                    return;
                }
            }

            // Set delegates again since the stream may have changed during setup
            if (clipData.clipStream != null)
            {
                clipData.clipStream.OnStreamUpdated = (cs) => OnStreamUpdated(clipData, cs, fromDisk);
                clipData.clipStream.OnStreamComplete = (cs) => OnStreamComplete(clipData, cs, fromDisk);
            }

            // Set clip stream state
            SetClipLoadState(clipData, TTSClipLoadState.Loaded);
            VLog.I(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Ready", clipData));

            // Invoke playback is ready
            clipData.onPlaybackReady?.Invoke(string.Empty);
            clipData.onPlaybackReady = null;

            // Callback delegate
            Events?.Stream?.OnStreamReady?.Invoke(clipData);
        }
        // Stream clip update
        private void OnStreamUpdated(TTSClipData clipData, IAudioClipStream clipStream, bool fromDisk)
        {
            // Ignore invalid
            if (clipStream == null || clipData == null || clipStream != clipData.clipStream)
            {
                return;
            }

            // Log & call event
            VLog.I(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Updated", clipData));
            Events?.Stream?.OnStreamClipUpdate?.Invoke(clipData);
        }
        // Stream complete
        private void OnStreamComplete(TTSClipData clipData, IAudioClipStream clipStream, bool fromDisk)
        {
            // Ignore invalid
            if (clipStream == null || clipData == null || clipStream != clipData.clipStream)
            {
                return;
            }

            // Log & call event
            VLog.I(GetClipLog($"{(fromDisk ? "Disk" : "Web")} Stream Complete", clipData));
            Events?.Stream?.OnStreamComplete?.Invoke(clipData);

            // Web request completion
            if (!fromDisk)
            {
                Events?.WebRequest?.OnRequestComplete.Invoke(clipData);
            }
        }
        #endregion

        #region UNLOAD
        /// <summary>
        /// Unload all audio clips from the runtime cache
        /// </summary>
        public void UnloadAll()
        {
            // Failed
            TTSClipData[] clips = RuntimeCacheHandler?.GetClips();
            if (clips == null)
            {
                return;
            }

            // Copy array
            HashSet<TTSClipData> remaining = new HashSet<TTSClipData>(clips);

            // Unload all clips
            foreach (var clip in remaining)
            {
                Unload(clip);
            }
        }
        /// <summary>
        /// Force a runtime cache unload
        /// </summary>
        public void Unload(TTSClipData clipData)
        {
            if (RuntimeCacheHandler != null)
            {
                RuntimeCacheHandler.RemoveClip(clipData.clipID);
            }
            else
            {
                OnUnloadBegin(clipData);
            }
        }
        /// <summary>
        /// Perform clip unload
        /// </summary>
        /// <param name="clipID"></param>
        protected virtual void OnUnloadBegin(TTSClipData clipData)
        {
            // Abort if currently preparing
            if (clipData.loadState == TTSClipLoadState.Preparing)
            {
                // Cancel web stream
                WebHandler?.CancelWebStream(clipData);
                // Cancel web download to cache
                WebHandler?.CancelWebDownload(clipData, GetDiskCachePath(clipData.textToSpeak, clipData.clipID, clipData.voiceSettings, clipData.diskCacheSettings));
                // Cancel disk cache stream
                DiskCacheHandler?.CancelDiskCacheStream(clipData);
            }

            // Unloads clip stream
            clipData.clipStream = null;

            // Clip is now unloaded
            SetClipLoadState(clipData, TTSClipLoadState.Unloaded);

            // Unload
            VLog.I(GetClipLog($"Unload Clip", clipData));
            Events?.OnClipUnloaded?.Invoke(clipData);
        }
        #endregion

        #region RUNTIME CACHE
        /// <summary>
        /// Obtain a clip from the runtime cache, if applicable
        /// </summary>
        public TTSClipData GetRuntimeCachedClip(string clipID) => RuntimeCacheHandler?.GetClip(clipID);
        /// <summary>
        /// Obtain all clips from the runtime cache, if applicable
        /// </summary>
        public TTSClipData[] GetAllRuntimeCachedClips() => RuntimeCacheHandler?.GetClips();

        /// <summary>
        /// Called when runtime cache adds a clip
        /// </summary>
        /// <param name="clipData"></param>
        protected virtual void OnRuntimeClipAdded(TTSClipData clipData) => OnLoadBegin(clipData);

        /// <summary>
        /// Called when runtime cache unloads a clip
        /// </summary>
        /// <param name="clipData">Clip to be unloaded</param>
        protected virtual void OnRuntimeClipRemoved(TTSClipData clipData) => OnUnloadBegin(clipData);
        #endregion

        #region DISK CACHE
        /// <summary>
        /// Whether a specific clip should be cached
        /// </summary>
        /// <param name="clipData">Clip data</param>
        /// <returns>True if should be cached</returns>
        public bool ShouldCacheToDisk(TTSClipData clipData) =>
            DiskCacheHandler != null && DiskCacheHandler.ShouldCacheToDisk(clipData);

        /// <summary>
        /// Get disk cache
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom disk cache settings</param>
        /// <returns></returns>
        public string GetDiskCachePath(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings) =>
            DiskCacheHandler?.GetDiskCachePath(CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings));

        // Download options
        public TTSClipData DownloadToDiskCache(string textToSpeak,
            Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, null, null, onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, string presetVoiceId,
            Action<TTSClipData, string, string> onDownloadComplete = null) => DownloadToDiskCache(textToSpeak, null,
            GetPresetVoiceSettings(presetVoiceId), null, onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, string presetVoiceId,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, GetPresetVoiceSettings(presetVoiceId), diskCacheSettings,
                onDownloadComplete);
        public TTSClipData DownloadToDiskCache(string textToSpeak, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null) =>
            DownloadToDiskCache(textToSpeak, null, voiceSettings, diskCacheSettings, onDownloadComplete);

        /// <summary>
        /// Perform a download for a TTS audio clip
        /// </summary>
        /// <param name="textToSpeak">Text to be spoken in clip</param>
        /// <param name="clipID">Unique clip id</param>
        /// <param name="voiceSettings">Custom voice settings</param>
        /// <param name="diskCacheSettings">Custom disk cache settings</param>
        /// <param name="onDownloadComplete">Callback when file has finished downloading</param>
        /// <returns>Generated TTS clip data</returns>
        public TTSClipData DownloadToDiskCache(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, Action<TTSClipData, string, string> onDownloadComplete = null)
        {
            TTSClipData clipData = CreateClipData(textToSpeak, clipID, voiceSettings, diskCacheSettings);
            DownloadToDiskCache(clipData, onDownloadComplete);
            return clipData;
        }

        // Performs download to disk cache
        protected virtual void DownloadToDiskCache(TTSClipData clipData, Action<TTSClipData, string, string> onDownloadComplete)
        {
            // Add delegates if needed
            AddDelegates();

            // Check if cached to disk & log
            string downloadPath = DiskCacheHandler.GetDiskCachePath(clipData);
            DiskCacheHandler.CheckCachedToDisk(clipData, (clip, found) =>
            {
                // Cache checked
                VLog.I(GetClipLog($"Disk Cache {(found ? "Found" : "Missing")}\nPath: {downloadPath}", clipData));

                // Already downloaded, return successful
                if (found)
                {
                    onDownloadComplete?.Invoke(clipData, downloadPath, string.Empty);
                    return;
                }

                // Preload selected but not in disk cache, return an error
                if (Application.isPlaying && clipData.diskCacheSettings.DiskCacheLocation == TTSDiskCacheLocation.Preload)
                {
                    onDownloadComplete?.Invoke(clipData, downloadPath, WitConstants.ERROR_TTS_CACHE_DOWNLOAD);
                    return;
                }

                // Add download completion callback
                clipData.onDownloadComplete += (error) => onDownloadComplete?.Invoke(clipData, downloadPath, error);

                // Download to cache
                WebHandler.RequestDownloadFromWeb(clipData, downloadPath);
            });
        }
        // On web download begin
        private void OnWebDownloadBegin(TTSClipData clipData, string downloadPath)
        {
            VLog.I(GetClipLog($"Download Clip - Begin\nPath: {downloadPath}", clipData));
            Events?.Download?.OnDownloadBegin?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadSuccess(TTSClipData clipData, string downloadPath)
        {
            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(string.Empty);
            clipData.onDownloadComplete = null;

            // Log
            VLog.I(GetClipLog($"Download Clip - Success\nPath: {downloadPath}", clipData));
            Events?.Download?.OnDownloadSuccess?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadCancel(TTSClipData clipData, string downloadPath)
        {
            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(WitConstants.CANCEL_ERROR);
            clipData.onDownloadComplete = null;

            // Log
            VLog.I(GetClipLog($"Download Clip - Canceled\nPath: {downloadPath}", clipData));
            Events?.Download?.OnDownloadCancel?.Invoke(clipData, downloadPath);
        }
        // On web download complete
        private void OnWebDownloadError(TTSClipData clipData, string downloadPath, string error)
        {
            // Cancelled
            if (error.Equals(WitConstants.CANCEL_ERROR))
            {
                OnWebDownloadCancel(clipData, downloadPath);
                return;
            }

            // Invoke clip callback & clear
            clipData.onDownloadComplete?.Invoke(error);
            clipData.onDownloadComplete = null;

            // Log
            VLog.E(GetClipLog($"Download Clip - Failed\nPath: {downloadPath}\nError: {error}", clipData));
            Events?.Download?.OnDownloadError?.Invoke(clipData, downloadPath, error);
        }
        #endregion

        #region VOICES
        /// <summary>
        /// Return all preset voice settings
        /// </summary>
        /// <returns></returns>
        public TTSVoiceSettings[] GetAllPresetVoiceSettings() => VoiceProvider?.PresetVoiceSettings;

        /// <summary>
        /// Return preset voice settings for a specific id
        /// </summary>
        /// <param name="presetVoiceId"></param>
        /// <returns></returns>
        public TTSVoiceSettings GetPresetVoiceSettings(string presetVoiceId)
        {
            if (VoiceProvider == null || VoiceProvider.PresetVoiceSettings == null)
            {
                return null;
            }
            return Array.Find(VoiceProvider.PresetVoiceSettings, (v) => string.Equals(v.SettingsId, presetVoiceId, StringComparison.CurrentCultureIgnoreCase));
        }
        #endregion
    }
}
