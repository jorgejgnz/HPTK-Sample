/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Meta.WitAi.Json;
using Meta.WitAi.Speech;
using UnityEngine;
using UnityEngine.Serialization;
using Meta.Voice.Audio;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.TTS.Interfaces;
using UnityEngine.Events;

namespace Meta.WitAi.TTS.Utilities
{
    public class TTSSpeaker : MonoBehaviour, ISpeechEventProvider
    {
        [Header("Event Settings")]
        [Tooltip("All speaker load and playback events")]
        [SerializeField] private TTSSpeakerEvents _events = new TTSSpeakerEvents();
        public TTSSpeakerEvents Events => _events;
        public VoiceSpeechEvents SpeechEvents => _events;

        [Header("Text Settings")]
        [Tooltip("Text that is added to the front of any Speech() request")]
        [TextArea] [FormerlySerializedAs("prependedText")]
        public string PrependedText;

        [Tooltip("Text that is added to the end of any Speech() text")]
        [TextArea] [FormerlySerializedAs("appendedText")]
        public string AppendedText;

        [Header("Load Settings")]
        [Tooltip("Optional TTSService reference to be used for text-to-speech loading.  If missing, it will check the component.  If that is also missing then it will use the current singleton")]
        [SerializeField] private TTSService _ttsService;
        public TTSService TTSService
        {
            get
            {
                if (!_ttsService)
                {
                    _ttsService = GetComponent<TTSService>();
                    if (!_ttsService)
                    {
                        _ttsService = TTSService.Instance;
                    }
                }
                return _ttsService;
            }
        }

        [Tooltip("Preset voice setting id of TTSService voice settings")]
        [HideInInspector] [SerializeField] public string presetVoiceID;

        [Tooltip("Custom wit specific voice settings used if the preset is null or empty")]
        [HideInInspector] [SerializeField] public TTSWitVoiceSettings customWitVoiceSettings;

        // Override voice settings
        private TTSVoiceSettings _overrideVoiceSettings;

        /// <summary>
        /// The voice settings to be used for this TTSSpeaker
        /// </summary>
        public TTSVoiceSettings VoiceSettings
        {
            get
            {
                // Use override if exists & runtime
                if (Application.isPlaying && _overrideVoiceSettings != null)
                {
                    return _overrideVoiceSettings;
                }
                // Attempts to use custom voice settings
                if (string.IsNullOrEmpty(presetVoiceID) && customWitVoiceSettings != null)
                {
                    return customWitVoiceSettings;
                }
                // Uses preset voice id
                return TTSService.GetPresetVoiceSettings(presetVoiceID);
            }
        }

        // Log category name
        protected virtual string LogCategory => GetType().Name;

        /// <summary>
        /// Whether a clip is currently playing for this speaker
        /// </summary>
        public bool IsSpeaking => SpeakingClip != null;
        /// <summary>
        /// The data for the currently playing clip
        /// </summary>
        public TTSClipData SpeakingClip => _speakingRequest.ClipData;

        /// <summary>
        /// Whether there are any clips in the loading queue
        /// </summary>
        public bool IsLoading => _queuedRequests.Count > 0;
        /// <summary>
        /// Whether any queued clips are still not ready for playback
        /// </summary>
        public bool IsPreparing
        {
            get
            {
                foreach (var request in _queuedRequests)
                {
                    if (request.ClipData != null && request.ClipData.loadState == TTSClipLoadState.Preparing)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
        // Loading clip queue
        public List<TTSClipData> QueuedClips
        {
            get
            {
                List<TTSClipData> clips = new List<TTSClipData>();
                foreach (var request in _queuedRequests)
                {
                    clips.Add(request.ClipData);
                }
                return clips;
            }
        }

        /// <summary>
        /// Whether the speaker currently has currently speaking clip or a playback queue
        /// </summary>
        public bool IsActive => IsSpeaking || IsLoading;

        // Current clip to be played
        protected TTSSpeakerRequestData _speakingRequest;
        // Full clip data list
        private Queue<TTSSpeakerRequestData> _queuedRequests = new Queue<TTSSpeakerRequestData>();
        protected struct TTSSpeakerRequestData
        {
            public TTSClipData ClipData;
            public DateTime StartTime;
            public bool StopQueueOnLoad;
            public TTSSpeakerClipEvents PlaybackEvents;
        }

        // Check if queued
        private bool _hasQueue = false;
        private bool _willHaveQueue = false;

        // Text processors
        private ISpeakerTextPreprocessor[] _textPreprocessors;
        private ISpeakerTextPostprocessor[] _textPostprocessors;

        /// <summary>
        /// The script used to perform audio playback of IAudioClipStreams.
        /// 1. Gets IAudioPlayer component if applied to this speaker
        /// 2. If no IAudioPlayer component is found, the TTSService's audio system
        /// will be used to generate an audio player.
        /// 3. If still not found, adds a UnityAudioPlayer.
        /// </summary>
        public IAudioPlayer AudioPlayer
        {
            get
            {
                if (_audioPlayer == null)
                {
                    _audioPlayer = gameObject.GetComponent<IAudioPlayer>();
                    if (_audioPlayer == null)
                    {
                        _audioPlayer = TTSService?.AudioSystem?.GetAudioPlayer(gameObject);
                        if (_audioPlayer == null)
                        {
                            _audioPlayer = gameObject.AddComponent<UnityAudioPlayer>();
                        }
                    }
                }
                return _audioPlayer;
            }
        }
        private IAudioPlayer _audioPlayer;

        // Unity audio source if used by the unity player
        public AudioSource AudioSource
        {
            get
            {
                if (AudioPlayer is IAudioSourceProvider uap)
                {
                    return uap.AudioSource;
                }
                return null;
            }
        }

        #region LIFECYCLE
        // Automatically generate source if needed
        protected virtual void Start()
        {
            // Initialize audio
            AudioPlayer.Init();

            // Get text processors
            RefreshProcessors();
        }
        // Refresh processors
        protected virtual void RefreshProcessors()
        {
            // Get preprocessors
            if (_textPreprocessors == null)
            {
                _textPreprocessors = GetComponents<ISpeakerTextPreprocessor>();
            }
            // Get postprocessors
            if (_textPostprocessors == null)
            {
                _textPostprocessors = GetComponents<ISpeakerTextPostprocessor>();
            }
            // Fix prepend text to ensure it has a space
            if (!string.IsNullOrEmpty(PrependedText) && PrependedText.Length > 0 && !PrependedText.EndsWith(" "))
            {
                PrependedText = PrependedText + " ";
            }
            // Fix append text to ensure it is spaced correctly
            if (!string.IsNullOrEmpty(AppendedText) && AppendedText.Length > 0 && !AppendedText.StartsWith(" "))
            {
                AppendedText = " " + AppendedText;
            }
        }
        // Stop
        protected virtual void OnDestroy()
        {
            Stop();
            _queuedRequests = null;
            _speakingRequest = new TTSSpeakerRequestData();
        }
        // Add listener for clip unload
        protected virtual void OnEnable()
        {
            if (!TTSService)
            {
                return;
            }
            TTSService.Events.OnClipUnloaded.AddListener(HandleClipUnload);
            TTSService.Events.Stream.OnStreamClipUpdate.AddListener(HandleClipUpdate);
        }
        // Stop speaking & remove listener
        protected virtual void OnDisable()
        {
            Stop();
            if (!TTSService)
            {
                return;
            }
            TTSService.Events.OnClipUnloaded.RemoveListener(HandleClipUnload);
            TTSService.Events.Stream.OnStreamClipUpdate.RemoveListener(HandleClipUpdate);
        }
        // Clip unloaded externally
        protected virtual void HandleClipUnload(TTSClipData clipData)
        {
            Stop(clipData, true);
        }
        // Clip stream complete
        protected virtual void HandleClipUpdate(TTSClipData clipData)
        {
            // Ignore if not speaking clip
            if (!clipData.Equals(SpeakingClip))
            {
                return;
            }

            // Apply new clip data
            _speakingRequest.ClipData = clipData;
            // Get current elapsed samples
            int elapsedSamples = AudioPlayer.ElapsedSamples;
            // Begin playback from elapsed sample
            AudioPlayer.Play(_speakingRequest.ClipData.clipStream, elapsedSamples);

            // Pause if desired
            if (IsPaused)
            {
                AudioPlayer.Pause();
            }

            // Clip updated callback
            OnPlaybackClipUpdated(_speakingRequest);
        }
        // Check queue
        private TTSSpeakerRequestData GetQueuedRequest(TTSClipData clipData)
        {
            if (_queuedRequests != null)
            {
                foreach (var requestData in _queuedRequests)
                {
                    if (string.Equals(clipData?.clipID, requestData.ClipData?.clipID))
                    {
                        return requestData;
                    }
                }
            }
            return new TTSSpeakerRequestData();
        }
        // Check queue
        private bool QueueContainsClip(TTSClipData clipData)
        {
            TTSSpeakerRequestData requestData = GetQueuedRequest(clipData);
            return requestData.ClipData != null;
        }
        // Refresh queue
        private void RefreshQueueEvents()
        {
            bool newHasQueueStatus = IsActive || _willHaveQueue;
            if (_hasQueue != newHasQueueStatus)
            {
                _hasQueue = newHasQueueStatus;
                if (_hasQueue)
                {
                    OnPlaybackQueueBegin();
                }
                else
                {
                    OnPlaybackQueueComplete();
                }
            }
        }
        // Check if clip request is active
        protected bool IsClipRequestActive(TTSSpeakerRequestData requestData)
        {
            return IsClipRequestLoading(requestData) || IsClipRequestSpeaking(requestData);
        }
        // Check if clip request is active
        protected bool IsClipRequestLoading(TTSSpeakerRequestData requestData)
        {
            return _queuedRequests.Contains(requestData);
        }
        // Check if clip request is active
        protected bool IsClipRequestSpeaking(TTSSpeakerRequestData requestData)
        {
            return _speakingRequest.Equals(requestData);
        }
        // Waits for all requests to complete
        protected IEnumerator WaitForCompletion(List<TTSSpeakerRequestData> requestData)
        {
            // All done
            int count = requestData?.Count ?? 0;
            if (count == 0)
            {
                yield break;
            }

            // Current active requests
            int activeRequests = 0;
            UnityAction<TTSSpeaker, TTSClipData> onComplete = (speaker, clip) => activeRequests--;

            // Add event delegates
            for (int r = 0; r < count; r++)
            {
                TTSSpeakerRequestData request = requestData[r];
                if (!IsClipRequestActive(request))
                {
                    continue;
                }
                activeRequests++;
                request.PlaybackEvents.OnComplete.AddListener(onComplete);
            }

            // Wait for active requests to be complete
            yield return new WaitWhile(() => activeRequests > 0);

            // Remove event delegates
            for (int r = 0; r < count; r++)
            {
                TTSSpeakerRequestData request = requestData[r];
                request.PlaybackEvents?.OnComplete.RemoveListener(onComplete);
            }
        }
        #endregion

        #region TEXT
        /// <summary>
        /// Gets final text following prepending/appending & any special formatting
        /// </summary>
        /// <param name="textToSpeak">The base text to be spoken</param>
        /// <returns>Returns an array of split texts to be spoken</returns>
        public virtual List<string> GetFinalText(string textToSpeak)
        {
            // Get processors
            RefreshProcessors();

            // Get results
            List<string> phrases = new List<string>();
            phrases.Add(textToSpeak);

            // Pre-processor
            if (_textPreprocessors != null)
            {
                foreach (var preprocessor in _textPreprocessors)
                {
                    preprocessor.OnPreprocessTTS(this, phrases);
                }
            }

            // Add prepend & appended text to each item
            for (int i = 0; i < phrases.Count; i++)
            {
                if (string.IsNullOrEmpty(phrases[i].Trim())) continue;

                string phrase = phrases[i];
                phrase = $"{PrependedText}{phrase}{AppendedText}".Trim();
                phrases[i] = phrase;
            }

            // Post-processors
            if (_textPostprocessors != null)
            {
                foreach (var postprocessor in _textPostprocessors)
                {
                    postprocessor.OnPostprocessTTS(this, phrases);
                }
            }

            // Return all text items
            return phrases;
        }
        /// <summary>
        /// Obtain final text list from format & text list
        /// </summary>
        /// <param name="format">The format to be used</param>
        /// <param name="textsToSpeak">The array of strings to be inserted into the format</param>
        /// <returns>Returns a list of formatted texts</returns>
        public virtual List<string> GetFinalTextFormatted(string format, params string[] textsToSpeak)
        {
            return GetFinalText(GetFormattedText(format, textsToSpeak));
        }
        /// <summary>
        /// Formats text using an initial format string parameter and additional text items to
        /// be inserted into the format
        /// </summary>
        /// <param name="format">The format to be used</param>
        /// <param name="textsToSpeak">The array of strings to be inserted into the format</param>
        /// <returns>A formatted text string</returns>
        public string GetFormattedText(string format, params string[] textsToSpeak)
        {
            if (textsToSpeak != null && !string.IsNullOrEmpty(format))
            {
                object[] objects = new object[textsToSpeak.Length];
                textsToSpeak.CopyTo(objects, 0);
                return string.Format(format, objects);
            }
            return null;
        }
        #endregion

        #region REQUESTS
        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events.
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents) =>
            Speak(textToSpeak, diskCacheSettings, playbackEvents, false);

        /// <summary>
        /// Load a tts clip using the specified text & playback events.  Cancels all previous clips
        /// when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void Speak(string textToSpeak, TTSSpeakerClipEvents playbackEvents) =>
            Speak(textToSpeak, null, playbackEvents);

        /// <summary>
        /// Load a tts clip using the specified text & disk cache settings.  Cancels all previous clips
        /// when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) =>
            Speak(textToSpeak, diskCacheSettings, null);

        /// <summary>
        /// Load a tts clip using the specified text.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        public void Speak(string textToSpeak) =>
            Speak(textToSpeak, null, null);

        /// <summary>
        /// Loads a formated phrase to be spoken.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        /// <param name="textsToSpeak">Texts to be inserted into the formatter</param>
        public void SpeakFormat(string format, params string[] textsToSpeak) =>
            Speak(GetFormattedText(format, textsToSpeak), null, null);

        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Speak text
            List<TTSSpeakerRequestData> requests = Speak(textToSpeak, diskCacheSettings, playbackEvents, false);
            // Wait while loading/speaking
            yield return WaitForCompletion(requests);
        }

        /// <summary>
        /// Load a tts clip using the specified text & playback events and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSSpeakerClipEvents playbackEvents)
        {
            yield return SpeakAsync(textToSpeak, null, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text & disk cache settings and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            yield return SpeakAsync(textToSpeak, diskCacheSettings, null);
        }

        /// <summary>
        /// Load a tts clip using the specified text and then waits for the file to load & play.
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        public IEnumerator SpeakAsync(string textToSpeak)
        {
            yield return SpeakAsync(textToSpeak, null, null);
        }

        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events.
        /// Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void SpeakQueued(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents) =>
            Speak(textToSpeak, diskCacheSettings, playbackEvents, true);

        /// <summary>
        /// Load a tts clip using the specified text & playback events.  Adds clip to playback queue and will
        /// speak once queue has completed all playback.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void SpeakQueued(string textToSpeak, TTSSpeakerClipEvents playbackEvents) =>
            SpeakQueued(textToSpeak, null, playbackEvents);

        /// <summary>
        /// Load a tts clip using the specified text & disk cache settings events.  Adds clip
        /// to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void SpeakQueued(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) =>
            SpeakQueued(textToSpeak, diskCacheSettings, null);

        /// <summary>
        /// Load a tts clip using the specified text.  Adds clip to playback queue and will speak
        /// once queue has completed all playback.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        public void SpeakQueued(string textToSpeak) =>
            SpeakQueued(textToSpeak, null, null);

        /// <summary>
        /// Loads a formated phrase to be spoken.  Adds clip to playback queue and will speak
        /// once queue has completed all playback.
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        /// <param name="textsToSpeak">Texts to be inserted into the formatter</param>
        public void SpeakFormatQueued(string format, params string[] textsToSpeak) =>
            SpeakQueued(GetFormattedText(format, textsToSpeak), null, null);

        /// <summary>
        /// Load a tts clip using the specified text phrases, disk cache settings & playback events and then
        /// waits for the files to load & play.  Adds clip to playback queue and will speak once queue has
        /// completed all playback.
        /// </summary>
        /// <param name="textsToSpeak">Multiple texts to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Speak each queued
            List<TTSSpeakerRequestData> requestList = new List<TTSSpeakerRequestData>();
            foreach (var textToSpeak in textsToSpeak)
            {
                List<TTSSpeakerRequestData> newRequests = Speak(textToSpeak, diskCacheSettings, playbackEvents, true);
                if (newRequests != null && newRequests.Count > 0)
                {
                    requestList.AddRange(newRequests);
                }
            }
            // Wait while loading/speaking
            yield return WaitForCompletion(requestList);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases & playback events and then waits for the files to load &
        /// play.  Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textsToSpeak">Multiple texts to be spoken</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSSpeakerClipEvents playbackEvents)
        {
            yield return SpeakQueuedAsync(textsToSpeak, null, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases & disk cache settings and then waits for the files to
        /// load & play.  Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textsToSpeak">Multiple texts to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            yield return SpeakQueuedAsync(textsToSpeak, diskCacheSettings, null);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases and then waits for the files to load & play.
        /// Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textsToSpeak">Multiple texts to be spoken</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak)
        {
            yield return SpeakQueuedAsync(textsToSpeak, null, null);
        }

        /// <summary>
        /// Loads a tts clip & handles playback
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        /// <param name="addToQueue">Whether or not this phrase should be enqueued into the playback queue</param>
        /// <returns>Speaker request data for request</returns>
        private List<TTSSpeakerRequestData> Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents, bool addToQueue)
        {
            // Ensure voice settings exist
            TTSVoiceSettings voiceSettings = VoiceSettings;
            if (voiceSettings == null)
            {
                VLog.E($"No voice found with preset id: {presetVoiceID}");
                return null;
            }

            // Get final text phrases to be spoken
            List<string> phrases = GetFinalText(textToSpeak);
            if (phrases == null || phrases.Count == 0)
            {
                VLog.W($"All phrases removed\nSource Phrase: {textToSpeak}");
                return null;
            }

            // Cancel previous loading queue
            if (!addToQueue)
            {
                _willHaveQueue = true;
                StopLoading();
                _willHaveQueue = false;
            }

            // Iterate voices
            List<TTSSpeakerRequestData> results = new List<TTSSpeakerRequestData>();
            foreach (var phrase in phrases)
            {
                TTSSpeakerRequestData requestData = HandleLoad(phrase, voiceSettings, diskCacheSettings, playbackEvents, addToQueue);
                results.Add(requestData);

                // Add additional to queue
                if (!addToQueue)
                {
                    addToQueue = true;
                }
            }
            return results;
        }

        /// <summary>
        /// Stop load & playback of a specific clip
        /// </summary>
        /// <param name="clipData">The clip to be stopped & removed from the queue</param>
        /// <param name="allInstances">Whether to remove the first instance of this clip or all instances</param>
        public virtual void Stop(string textToSpeak, bool allInstances = false)
        {
            // Found speaking clip
            if (string.Equals(SpeakingClip?.textToSpeak, textToSpeak))
            {
                Stop(SpeakingClip, allInstances);
                return;
            }

            // Find all clips that match & stop them
            foreach (var clipData in QueuedClips)
            {
                if (string.Equals(clipData?.textToSpeak, textToSpeak))
                {
                    Stop(clipData, allInstances);
                    if (!allInstances)
                    {
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Stop load & playback of a specific clip
        /// </summary>
        /// <param name="clipData">The clip to be stopped & removed from the queue</param>
        /// <param name="allInstances">Whether to remove the first instance of this clip or all instances</param>
        public virtual void Stop(TTSClipData clipData, bool allInstances = false)
        {
            // Check if speaking
            bool isSpeakingClip = SpeakingClip != null && clipData.Equals(SpeakingClip);

            // Cancel queue
            if (!isSpeakingClip || allInstances)
            {
                // Unload all instances
                if (allInstances)
                {
                    if (QueueContainsClip(clipData))
                    {
                        HandleUnload(clipData, string.Empty);
                    }
                }
                // Unload a single request
                else
                {
                    HandleUnload(GetQueuedRequest(clipData), string.Empty);
                }
            }

            // Cancel playback
            if (isSpeakingClip)
            {
                StopSpeaking();
            }
        }

        /// <summary>
        /// Abort loading of all items in the load queue
        /// </summary>
        public virtual void StopLoading()
        {
            // Ignore if not loading
            if (!IsLoading)
            {
                return;
            }

            // Cancel each clip from loading
            while (_queuedRequests.Count > 0)
            {
                OnLoadAborted(_queuedRequests.Dequeue());
            }

            // Refresh in queue check
            RefreshQueueEvents();
        }

        /// <summary>
        /// Stop playback of currently played audio clip
        /// </summary>
        public virtual void StopSpeaking()
        {
            // Cannot stop speaking when not currently speaking
            if (!IsSpeaking)
            {
                return;
            }

            // Cancel playback
            HandlePlaybackComplete(true);
        }

        /// <summary>
        /// Stops loading & playback immediately
        /// </summary>
        public virtual void Stop()
        {
            StopLoading();
            StopSpeaking();
        }
        #endregion

        #region VOICE OVERRIDE
        /// <summary>
        /// Set a voice override for future requests
        /// </summary>
        /// <param name="overrideSettings">The settings to be applied to upcoming requests</param>
        public void SetVoiceOverride(TTSVoiceSettings overrideVoiceSettings)
        {
            _overrideVoiceSettings = overrideVoiceSettings;
        }

        /// <summary>
        /// Clears the current voice override
        /// </summary>
        public void ClearVoiceOverride() => SetVoiceOverride(null);

        /// <summary>
        /// Decode a response node into text to be spoken or a specific voice setting
        /// Example Data:
        /// {
        ///    "q": "Text to be spoken"
        ///    "voice": "Charlie
        /// }
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="textToSpeak">The text to be spoken output</param>
        /// <param name="voiceSettings">The output for voice settings</param>
        /// <returns>True if decode was successful</returns>
        private bool DecodeResponse(WitResponseNode responseNode, out string textToSpeak, out TTSVoiceSettings voiceSettings)
        {
            // Wit settings
            if (TTSWitVoiceSettings.CanDecode(responseNode))
            {
                TTSWitVoiceSettings witVoice = JsonConvert.DeserializeObject<TTSWitVoiceSettings>(responseNode);
                if (witVoice != null)
                {
                    textToSpeak = responseNode[WitConstants.ENDPOINT_TTS_PARAM];
                    voiceSettings = witVoice;
                    voiceSettings.SettingsId = "OVERRIDE";
                    return true;
                }
            }

            // Default application
            textToSpeak = null;
            voiceSettings = null;
            return false;
        }

        /// <summary>
        /// Load a tts clip using the specified response node, disk cache settings & playback events.
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="overrideVoiceSettings">Custom voice settings to be used for this and upcoming requests</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void Speak(string textToSpeak, TTSVoiceSettings overrideVoiceSettings, TTSDiskCacheSettings diskCacheSettings,
            TTSSpeakerClipEvents playbackEvents)
        {
            // Apply voice override
            SetVoiceOverride(overrideVoiceSettings);

            // Speak
            Speak(textToSpeak, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified response node, disk cache settings & playback events.
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool Speak(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings,
            TTSSpeakerClipEvents playbackEvents)
        {
            // Decode text to speak & voice settings
            if (!DecodeResponse(responseNode, out var textToSpeak, out var voiceSettings))
            {
                return false;
            }
            // Speak
            Speak(textToSpeak, voiceSettings, diskCacheSettings, playbackEvents);
            return true;
        }

        /// <summary>
        /// Load a tts clip using the specified response node & disk cache settings
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool Speak(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings) =>
            Speak(responseNode, diskCacheSettings, null);

        /// <summary>
        /// Load a tts clip using the specified response node & playback events
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool Speak(WitResponseNode responseNode, TTSSpeakerClipEvents playbackEvents) =>
            Speak(responseNode, null, playbackEvents);

        /// <summary>
        /// Load a tts clip using the specified response node & playback events
        /// Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool Speak(WitResponseNode responseNode) => Speak(responseNode, null, null);

        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="overrideVoiceSettings">Custom voice settings to be used for this and upcoming requests</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSVoiceSettings overrideVoiceSettings, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Set voice override
            SetVoiceOverride(overrideVoiceSettings);

            // Wait while loading/speaking
            yield return SpeakAsync(textToSpeak, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakAsync(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Decode text to speak & voice settings
            if (!DecodeResponse(responseNode, out var textToSpeak, out var voiceSettings))
            {
                yield break;
            }

            // Wait while loading/speaking
            yield return SpeakAsync(textToSpeak, voiceSettings, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text & playback events and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakAsync(WitResponseNode responseNode, TTSSpeakerClipEvents playbackEvents)
        {
            yield return SpeakAsync(responseNode, null, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text & disk cache settings and then waits
        /// for the file to load & play.  Cancels all previous clips when loaded & then plays.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakAsync(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings)
        {
            yield return SpeakAsync(responseNode, diskCacheSettings, null);
        }

        /// <summary>
        /// Load a tts clip using the specified response node, disk cache settings & playback events.
        /// Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="overrideVoiceSettings">Custom voice settings to be used for this and upcoming requests</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public void SpeakQueued(string textToSpeak, TTSVoiceSettings overrideVoiceSettings, TTSDiskCacheSettings diskCacheSettings,
            TTSSpeakerClipEvents playbackEvents)
        {
            // Apply voice override
            SetVoiceOverride(overrideVoiceSettings);

            // Speak
            SpeakQueued(textToSpeak, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text, disk cache settings & playback events.
        /// Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool SpeakQueued(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings,
            TTSSpeakerClipEvents playbackEvents)
        {
            // Decode text to speak & voice settings
            if (!DecodeResponse(responseNode, out var textToSpeak, out var voiceSettings))
            {
                return false;
            }
            // Speak queued
            SpeakQueued(textToSpeak, voiceSettings, diskCacheSettings, playbackEvents);
            return true;
        }

        /// <summary>
        /// Load a tts clip using the specified text & playback events.  Adds clip to playback queue and will
        /// speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool SpeakQueued(WitResponseNode responseNode, TTSSpeakerClipEvents playbackEvents) =>
            SpeakQueued(responseNode, null, playbackEvents);

        /// <summary>
        /// Load a tts clip using the specified text & disk cache settings events.  Adds clip
        /// to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool SpeakQueued(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings) =>
            SpeakQueued(responseNode, diskCacheSettings, null);

        /// <summary>
        /// Load a tts clip using the specified text.  Adds clip to playback queue and will speak
        /// once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <returns>True if responseNode is decoded successfully</returns>
        public bool SpeakQueued(WitResponseNode responseNode) =>
            SpeakQueued(responseNode, null, null);

        /// <summary>
        /// Load a tts clip using the specified text phrases, disk cache settings & playback events and then
        /// waits for the files to load & play.  Adds clip to playback queue and will speak once queue has
        /// completed all playback.
        /// </summary>
        /// <param name="textsToSpeak">Multiple texts to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSVoiceSettings overrideVoiceSettings, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Set override
            SetVoiceOverride(overrideVoiceSettings);
            // Wait while loading/speaking
            yield return SpeakQueuedAsync(textsToSpeak, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases, disk cache settings & playback events and then
        /// waits for the files to load & play.  Adds clip to playback queue and will speak once queue has
        /// completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakQueuedAsync(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents)
        {
            // Decode text to speak & voice settings
            if (!DecodeResponse(responseNode, out var textToSpeak, out var voiceSettings))
            {
                yield break;
            }
            // Wait while loading/speaking
            yield return SpeakQueuedAsync(new string[] {textToSpeak}, voiceSettings, diskCacheSettings, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases & playback events and then waits for the files to load &
        /// play.  Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="playbackEvents">Events to be called for this specific tts playback request</param>
        public IEnumerator SpeakQueuedAsync(WitResponseNode responseNode, TTSSpeakerClipEvents playbackEvents)
        {
            yield return SpeakQueuedAsync(responseNode, null, playbackEvents);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases & disk cache settings and then waits for the files to
        /// load & play.  Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakQueuedAsync(WitResponseNode responseNode, TTSDiskCacheSettings diskCacheSettings)
        {
            yield return SpeakQueuedAsync(responseNode, diskCacheSettings, null);
        }

        /// <summary>
        /// Load a tts clip using the specified text phrases and then waits for the files to load & play.
        /// Adds clip to playback queue and will speak once queue has completed all playback.
        /// </summary>
        /// <param name="responseNode">Parsed data that includes text to be spoken & voice settings</param>
        public IEnumerator SpeakQueuedAsync(WitResponseNode responseNode)
        {
            yield return SpeakQueuedAsync(responseNode, null, null);
        }
        #endregion

        #region LOAD
        // Handles speaking depending on the state of the specified audio
        private TTSSpeakerRequestData HandleLoad(string textToSpeak, TTSVoiceSettings voiceSettings,
            TTSDiskCacheSettings diskCacheSettings, TTSSpeakerClipEvents playbackEvents,
            bool addToQueue)
        {
            // Generate request data
            TTSSpeakerRequestData requestData = new TTSSpeakerRequestData();
            requestData.StartTime = DateTime.Now;
            requestData.StopQueueOnLoad = !addToQueue;
            requestData.PlaybackEvents = playbackEvents ?? new TTSSpeakerClipEvents();

            // Perform load request (Always waits a frame to ensure callbacks occur first)
            string clipId = TTSService.GetClipID(textToSpeak, voiceSettings);
            requestData.ClipData = TTSService.Load(textToSpeak, clipId, voiceSettings, diskCacheSettings, (clipData, error) => HandleLoadComplete(requestData, error));

            // Ignore without clip
            if (requestData.ClipData == null)
            {
                return requestData;
            }

            // Enqueue
            _queuedRequests.Enqueue(requestData);

            // Initialized, possibly started queue & load began
            OnInit(requestData);
            RefreshQueueEvents();
            OnLoadBegin(requestData);

            // Return data
            return requestData;
        }
        // Load complete
        private void HandleLoadComplete(TTSSpeakerRequestData requestData, string error)
        {
            // Not queued
            if (_queuedRequests != null && !_queuedRequests.Contains(requestData))
            {
                return;
            }

            // Check for other errors
            if (string.IsNullOrEmpty(error) && !string.IsNullOrEmpty(requestData.ClipData.textToSpeak))
            {
                if (requestData.ClipData == null)
                {
                    error = "No TTSClipData found";
                }
                else if (requestData.ClipData.clipStream == null)
                {
                    error = "No AudioClip found";
                }
                else if (requestData.ClipData.loadState == TTSClipLoadState.Error)
                {
                    error = "Error without message";
                }
                else if (requestData.ClipData.loadState == TTSClipLoadState.Unloaded)
                {
                    error = WitConstants.CANCEL_ERROR;
                }
            }

            // Load failed
            if (!string.IsNullOrEmpty(error))
            {
                // Remove clip
                HandleUnload(requestData, error);
            }
            // Load success
            else
            {
                // Load success event
                OnLoadSuccess(requestData);

                // Playback is ready
                OnPlaybackReady(requestData);
            }

            // Stop previously spoken clip & play next
            if (requestData.StopQueueOnLoad && IsSpeaking)
            {
                StopSpeaking();
            }
            // Attempt to play next in queue
            else
            {
                RefreshPlayback();
            }
        }
        #endregion

        #region PLAYBACK
        // Wait for playback completion
        private Coroutine _waitForCompletion;

        /// <summary>
        /// Refreshes playback queue to play next available clip if possible
        /// </summary>
        private void RefreshPlayback()
        {
            // Ignore if currently playing or nothing in uque
            if (SpeakingClip != null ||  _queuedRequests == null || _queuedRequests.Count == 0 || _audioPlayer == null)
            {
                return;
            }
            // Peek next request
            TTSSpeakerRequestData requestData = _queuedRequests.Peek();
            if (requestData.ClipData == null)
            {
                HandleLoadComplete(requestData, "TTSClipData no longer exists");
                return;
            }
            // Still preparing
            if (requestData.ClipData.loadState == TTSClipLoadState.Preparing)
            {
                return;
            }
            if (requestData.ClipData.loadState != TTSClipLoadState.Loaded)
            {
                HandleLoadComplete(requestData, $"TTSClipData is {requestData.ClipData.loadState}");
                return;
            }
            // No audio source
            string errors = AudioPlayer.GetPlaybackErrors();
            if (!string.IsNullOrEmpty(errors))
            {
                HandleLoadComplete(requestData, errors);
                return;
            }

            // Resume prior to playback
            if (requestData.StopQueueOnLoad && IsPaused)
            {
                Resume();
            }

            if (!string.IsNullOrEmpty(requestData.ClipData.textToSpeak))
            {
                // Somehow clip unloaded
                if (requestData.ClipData.clipStream == null)
                {
                    HandleLoadComplete(requestData, "AudioClipStream no longer exists");
                    return;
                }

                // Dequeue & apply
                _speakingRequest = _queuedRequests.Dequeue();

                // Started speaking
                AudioPlayer.Play(_speakingRequest.ClipData.clipStream, 0);

                // Call playback start events
                OnPlaybackStart(_speakingRequest);

                // Wait for completion
                if (_waitForCompletion != null)
                {
                    StopCoroutine(_waitForCompletion);
                    _waitForCompletion = null;
                }
                _waitForCompletion = StartCoroutine(WaitForPlaybackComplete());
            }
            else
            {
                // If we're sending an empty string we're really just potentially queuing an event so we can trigger it
                // between audio clips. Trigger start/stop events.
                _speakingRequest = _queuedRequests.Dequeue();
                OnPlaybackStart(_speakingRequest);
                HandlePlaybackComplete(false);
            }
        }
        // Wait for clip completion
        private IEnumerator WaitForPlaybackComplete()
        {
            // Use delta time to wait for completion
            float elapsedTime = 0f;
            while (!IsPlaybackComplete(elapsedTime))
            {
                yield return new WaitForEndOfFrame();

                // Fix audio source, paused/resumed externally
                bool playerPaused = !AudioPlayer.IsPlaying;
                if (IsPaused != playerPaused)
                {
                    if (IsPaused)
                    {
                        AudioPlayer.Pause();
                    }
                    else
                    {
                        AudioPlayer.Resume();
                    }
                }

                // Only increment if playing
                if (!IsPaused)
                {
                    elapsedTime += Time.deltaTime;
                }
            }

            // Playback completed
            HandlePlaybackComplete(false);
        }
        // Check for playback completion
        protected virtual bool IsPlaybackComplete(float elapsedTime)
        {
            return elapsedTime >= AudioPlayer?.ClipStream.Length || (!AudioPlayer.IsPlaying && !IsPaused);
        }
        // Completed playback
        protected virtual void HandlePlaybackComplete(bool stopped)
        {
            // Stop playback handler
            if (_waitForCompletion != null)
            {
                StopCoroutine(_waitForCompletion);
                _waitForCompletion = null;
            }

            // Keep last request data
            TTSSpeakerRequestData lastRequestData = _speakingRequest;
            // Clear speaking request
            _speakingRequest = new TTSSpeakerRequestData();

            // Stop audio source playback
            AudioPlayer.Stop();

            // Stopped
            if (stopped)
            {
                OnPlaybackCancelled(lastRequestData, "Playback stopped manually");
            }
            // No clip found
            else if (lastRequestData.ClipData == null)
            {
                OnPlaybackCancelled(lastRequestData, "TTSClipData no longer exists");
            }
            // Clip unloaded
            else if (lastRequestData.ClipData.loadState == TTSClipLoadState.Unloaded)
            {
                OnPlaybackCancelled(lastRequestData, "TTSClipData was unloaded");
            }
            // Clip destroyed
            else if (lastRequestData.ClipData.clipStream == null)
            {
                OnPlaybackCancelled(lastRequestData, "AudioClip no longer exists");
            }
            // Success
            else
            {
                OnPlaybackComplete(lastRequestData);
            }

            // Refresh in queue check
            RefreshQueueEvents();

            // Attempt to play next in queue if all playback was not just stopped
            RefreshPlayback();
        }
        #endregion

        #region PAUSE
        /// <summary>
        /// Whether playback is currently paused or not
        /// </summary>
        public bool IsPaused { get; private set; }

        /// <summary>
        /// Pause any current or future loaded audio playback
        /// </summary>
        public void Pause() => SetPause(true);

        /// <summary>
        /// Resume playback for current and future audio clips
        /// </summary>
        public void Resume() => SetPause(false);

        // Set's the current pause state
        protected virtual void SetPause(bool toPaused)
        {
            // Ignore if same
            if (IsPaused == toPaused)
            {
                return;
            }

            // Apply
            IsPaused = toPaused;
            Log($"Speak Audio {(IsPaused ? "Paused" : "Resumed")}");

            // Adjust if speaking
            if (IsSpeaking)
            {
                if (IsPaused)
                {
                    AudioPlayer.Pause();
                }
                else if (!IsPaused)
                {
                    AudioPlayer.Resume();
                }
            }
        }
        #endregion

        #region UNLOAD
        // Handles unload of all requests using a specific clip
        private void HandleUnload(TTSClipData clipData, string error)
        {
            HandleUnload((checkRequest) => !string.Equals(checkRequest.ClipData.clipID, clipData?.clipID), error);
        }
        // Handles unload of specific request
        private void HandleUnload(TTSSpeakerRequestData requestData, string error)
        {
            HandleUnload((checkRequest) => !checkRequest.Equals(requestData), error);
        }
        // Handles unload of requests with specified should keep lookup
        private void HandleUnload(Func<TTSSpeakerRequestData, bool> shouldKeep, string error)
        {
            // Ignore if destroyed
            if (_queuedRequests == null)
            {
                return;
            }

            // Otherwise create discard queue
            Queue<TTSSpeakerRequestData> discard = _queuedRequests;
            _queuedRequests = new Queue<TTSSpeakerRequestData>();

            // Iterate all items
            while (discard.Count > 0)
            {
                // Dequeue from discard
                TTSSpeakerRequestData check = discard.Dequeue();

                // Clip data missing
                if (check.ClipData == null)
                {
                    OnLoadFailed(check, "TTSClipData missing");
                }
                // Do not keep
                else if (shouldKeep != null && !shouldKeep(check))
                {
                    // Cancelled
                    if (string.IsNullOrEmpty(error) || string.Equals(error, WitConstants.CANCEL_ERROR))
                    {
                        OnLoadAborted(check);
                    }
                    // Failure
                    else
                    {
                        OnLoadFailed(check, error);
                    }
                }
                // Keep all others
                else
                {
                    _queuedRequests.Enqueue(check);
                }
            }

            // Refresh in queue check
            RefreshQueueEvents();
        }
        #endregion

        #region QUEUE EVENTS
        // Log comment with request
        protected virtual void Log(string comment)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine(comment);
            log.AppendLine($"Voice: {VoiceSettings?.SettingsId}");
            VLog.I(LogCategory, log);
        }
        // Perform start of playback queue
        protected virtual void OnPlaybackQueueBegin()
        {
            Log("Playback Queue Begin");
            Events?.OnPlaybackQueueBegin?.Invoke();
        }
        // Perform end of playback queue
        protected virtual void OnPlaybackQueueComplete()
        {
            Log("Playback Queue Complete");
            Events?.OnPlaybackQueueComplete?.Invoke();
        }
        #endregion

        #region PLAYBACK EVENTS
        // Log comment with request
        protected virtual void LogRequestData(string comment, TTSSpeakerRequestData requestData, bool warning = false)
        {
            StringBuilder log = new StringBuilder();
            log.AppendLine(comment);
            log.AppendLine($"Voice: {requestData.ClipData?.voiceSettings?.SettingsId}");
            log.AppendLine($"Cache: {requestData.ClipData?.diskCacheSettings?.DiskCacheLocation.ToString()}");
            log.AppendLine($"Text: {requestData.ClipData?.textToSpeak}");
            log.AppendLine($"Audio Player Type: {(_audioPlayer == null ? "NULL" : _audioPlayer.GetType().ToString())}");
            log.AppendLine($"Audio Clip Stream Type: {(requestData.ClipData?.clipStream == null ? "NULL" : requestData.ClipData?.clipStream.GetType().ToString())}");
            log.AppendLine($"Elapsed: {(DateTime.Now - requestData.StartTime).TotalMilliseconds:0.0}ms");
            if (warning)
            {
                VLog.W(LogCategory, log);
            }
            else
            {
                VLog.I(LogCategory, log);
            }
        }
        // Initial callback as soon as the audio clip speak request is generated
        protected virtual void OnInit(TTSSpeakerRequestData requestData)
        {
            Events?.OnInit?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnInit?.Invoke(this, requestData.ClipData);
        }
        // Perform load begin events
        protected virtual void OnLoadBegin(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Load Begin", requestData);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataQueued?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnClipDataLoadBegin?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnClipLoadBegin?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnLoadBegin?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnLoadBegin?.Invoke(this, requestData.ClipData);
        }
        // Perform load begin abort events
        protected virtual void OnLoadAborted(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Load Aborted", requestData);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataLoadAbort?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnClipLoadAbort?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnLoadAbort?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnLoadAbort?.Invoke(this, requestData.ClipData);

            // Complete
            OnComplete(requestData);
        }
        // Perform load failed events
        protected virtual void OnLoadFailed(TTSSpeakerRequestData requestData, string error)
        {
            LogRequestData($"Load Failed\nError: {error}", requestData, true);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataLoadFailed?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnClipLoadFailed?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnLoadFailed?.Invoke(this, requestData.ClipData, error);
            requestData.PlaybackEvents?.OnLoadFailed?.Invoke(this, requestData.ClipData, error);

            // Complete
            OnComplete(requestData);
        }
        // Perform load success events
        protected virtual void OnLoadSuccess(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Load Success", requestData);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataLoadSuccess?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnClipLoadSuccess?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnLoadSuccess?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnLoadSuccess?.Invoke(this, requestData.ClipData);
        }
        // Perform events for playback being ready
        protected virtual void OnPlaybackReady(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Playback Ready", requestData);

            // Speaker playback events
            Events?.OnAudioClipPlaybackReady?.Invoke(requestData.ClipData?.clip);
            requestData.PlaybackEvents?.OnAudioClipPlaybackReady?.Invoke(requestData.ClipData?.clip);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataPlaybackReady?.Invoke(requestData.ClipData);

            // Speaker clip events
            Events?.OnPlaybackReady?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnPlaybackReady?.Invoke(this, requestData.ClipData);
        }
        // Perform events for playback start
        protected virtual void OnPlaybackStart(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Playback Begin", requestData);

            // Speaker playback events
            Events?.OnTextPlaybackStart?.Invoke(requestData.ClipData?.textToSpeak);
            requestData.PlaybackEvents?.OnTextPlaybackStart?.Invoke(requestData.ClipData?.textToSpeak);
            Events?.OnAudioClipPlaybackStart?.Invoke(requestData.ClipData?.clip);
            requestData.PlaybackEvents?.OnAudioClipPlaybackStart?.Invoke(requestData.ClipData?.clip);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataPlaybackStart?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnStartSpeaking?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnPlaybackStart?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnPlaybackStart?.Invoke(this, requestData.ClipData);
        }
        // Perform events for playback cancelation
        protected virtual void OnPlaybackCancelled(TTSSpeakerRequestData requestData, string reason)
        {
            LogRequestData($"Playback Cancelled\nReason: {reason}", requestData);

            // Speaker playback events
            Events?.OnTextPlaybackCancelled?.Invoke(requestData.ClipData?.textToSpeak);
            requestData.PlaybackEvents?.OnTextPlaybackCancelled?.Invoke(requestData.ClipData?.textToSpeak);
            Events?.OnAudioClipPlaybackCancelled?.Invoke(requestData.ClipData?.clip);
            requestData.PlaybackEvents?.OnAudioClipPlaybackCancelled?.Invoke(requestData.ClipData?.clip);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataPlaybackCancelled?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnCancelledSpeaking?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnPlaybackCancelled?.Invoke(this, requestData.ClipData, reason);
            requestData.PlaybackEvents?.OnPlaybackCancelled?.Invoke(this, requestData.ClipData, reason);

            // Complete
            OnComplete(requestData);
        }
        // Perform audio clip update during streaming playback
        protected virtual void OnPlaybackClipUpdated(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Playback Clip Updated", requestData);

            // Speaker clip events
            Events?.OnPlaybackClipUpdated?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnPlaybackClipUpdated?.Invoke(this, requestData.ClipData);
        }
        // Perform events for playback completion
        protected virtual void OnPlaybackComplete(TTSSpeakerRequestData requestData)
        {
            LogRequestData("Playback Complete", requestData);

            // Speaker playback events
            Events?.OnTextPlaybackFinished?.Invoke(requestData.ClipData?.textToSpeak);
            requestData.PlaybackEvents?.OnTextPlaybackFinished?.Invoke(requestData.ClipData?.textToSpeak);
            Events?.OnAudioClipPlaybackFinished?.Invoke(requestData.ClipData?.clip);
            requestData.PlaybackEvents?.OnAudioClipPlaybackFinished?.Invoke(requestData.ClipData?.clip);

            // Deprecated speaker events
#pragma warning disable CS0618
            Events?.OnClipDataPlaybackFinished?.Invoke(requestData.ClipData);
#pragma warning disable CS0618
            Events?.OnFinishedSpeaking?.Invoke(this, requestData.ClipData?.textToSpeak);

            // Speaker clip events
            Events?.OnPlaybackComplete?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnPlaybackComplete?.Invoke(this, requestData.ClipData);

            // Complete
            OnComplete(requestData);
        }
        // Final call for a 'Speak' request that is called following a load failure, load abort, playback cancellation or playback completion
        protected virtual void OnComplete(TTSSpeakerRequestData requestData)
        {
            Events?.OnComplete?.Invoke(this, requestData.ClipData);
            requestData.PlaybackEvents?.OnComplete?.Invoke(this, requestData.ClipData);
        }
        #endregion
    }
}
