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
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi.TTS.Data;
using UnityEngine.Serialization;

namespace Meta.WitAi.TTS.Utilities
{
    [Serializable]
    public class TTSSpeakerEvent : UnityEvent<TTSSpeaker, string> { }
    [Serializable]
    public class TTSSpeakerTextEvent : UnityEvent<string> { }
    [Serializable]
    public class TTSSpeakerClipEvent : UnityEvent<AudioClip> { }
    [Serializable]
    public class TTSSpeakerClipDataEvent : UnityEvent<TTSClipData> { }
    [Serializable]
    public class TTSSpeakerEvents
    {
        [Header("Text Events")]
        [Tooltip("Called when a audio clip playback begins")]
        public TTSSpeakerTextEvent OnTextPlaybackStart;
        [Tooltip("Called when a audio clip playback completes or is cancelled")]
        public TTSSpeakerTextEvent OnTextPlaybackFinished;
        [Tooltip("Called when a audio clip playback completes or is cancelled")]
        public TTSSpeakerTextEvent OnTextPlaybackCancelled;

        [Header("Audio Clip Events")]
        [Tooltip("Called when a clip is ready for playback")]
        public TTSSpeakerClipEvent OnAudioClipPlaybackReady;
        [Tooltip("Called when a clip playback has begun")]
        public TTSSpeakerClipEvent OnAudioClipPlaybackStart;
        [Tooltip("Called when a clip playback has completed successfully")]
        public TTSSpeakerClipEvent OnAudioClipPlaybackFinished;
        [Tooltip("Called when a clip playback has been cancelled")]
        public TTSSpeakerClipEvent OnAudioClipPlaybackCancelled;

        [Header("TTSClip Data Events")]
        [Tooltip("Called when a new clip is added to the playback queue")]
        public TTSSpeakerClipDataEvent OnClipDataQueued;
        [Tooltip("Called when TTS audio clip load begins")]
        public TTSSpeakerClipDataEvent OnClipDataLoadBegin;
        [Tooltip("Called when TTS audio clip load fails")]
        public TTSSpeakerClipDataEvent OnClipDataLoadFailed;
        [Tooltip("Called when TTS audio clip load successfully")]
        public TTSSpeakerClipDataEvent OnClipDataLoadSuccess;
        [Tooltip("Called when TTS audio clip load is cancelled")]
        public TTSSpeakerClipDataEvent OnClipDataLoadAbort;
        [Tooltip("Called when a clip is ready for playback")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackReady;
        [Tooltip("Called when a clip playback has begun")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackStart;
        [Tooltip("Called when a clip playback has completed successfully")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackFinished;
        [Tooltip("Called when a clip playback has been cancelled")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackCancelled;

        [Header("Speaker Events")]
        [Tooltip("Called when a speaking begins")]
        public TTSSpeakerEvent OnStartSpeaking;
        [Tooltip("Called when a speaking finishes")]
        public TTSSpeakerEvent OnFinishedSpeaking;
        [Tooltip("Called when a speaking is cancelled")]
        public TTSSpeakerEvent OnCancelledSpeaking;
        [Tooltip("Called when TTS audio clip load begins")]
        public TTSSpeakerEvent OnClipLoadBegin;
        [Tooltip("Called when TTS audio clip load fails")]
        public TTSSpeakerEvent OnClipLoadFailed;
        [Tooltip("Called when TTS audio clip load successfully")]
        public TTSSpeakerEvent OnClipLoadSuccess;
        [Tooltip("Called when TTS audio clip load is cancelled")]
        public TTSSpeakerEvent OnClipLoadAbort;
    }

    public class TTSSpeaker : MonoBehaviour
    {
        #region LIFECYCLE
        // Preset voice id
        [HideInInspector] [SerializeField] public string presetVoiceID;
        public TTSVoiceSettings VoiceSettings => _tts.GetPresetVoiceSettings(presetVoiceID);
        // Audio source
        [SerializeField] [FormerlySerializedAs("_source")]
        public AudioSource AudioSource;

        [Tooltip("Text that is added to the front of any Speech() request")]
        [TextArea]
        [SerializeField] private string prependedText;
        [TextArea]
        [Tooltip("Text that is added to the end of any Speech() text")]
        [SerializeField] private string appendedText;

        // Events
        [SerializeField] private TTSSpeakerEvents _events;
        public TTSSpeakerEvents Events => _events;

        // Current clip to be played
        public TTSClipData SpeakingClip { get; private set; }
        // Whether currently speaking or not
        public bool IsSpeaking => SpeakingClip != null;

        // Loading clip queue
        public TTSClipData[] QueuedClips => _queuedClips.ToArray();
        // Full clip data list
        private Queue<TTSClipData> _queuedClips = new Queue<TTSClipData>();
        // Whether currently loading or not
        public bool IsLoading => _queuedClips.Count > 0;

        // Current tts service
        private TTSService _tts;

        // Automatically generate source if needed
        protected virtual void Awake()
        {
            if (AudioSource == null)
            {
                AudioSource = gameObject.GetComponentInChildren<AudioSource>();
                if (AudioSource == null)
                {
                    AudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            AudioSource.playOnAwake = false;
            _tts = TTSService.Instance;
        }
        // Add listener for clip unload
        protected virtual void OnEnable()
        {
            if (_tts == null)
            {
                return;
            }
            _tts.Events.OnClipUnloaded.AddListener(OnClipUnload);
        }
        // Stop speaking & remove listener
        protected virtual void OnDisable()
        {
            Stop();
            if (_tts == null)
            {
                return;
            }
            _tts.Events.OnClipUnloaded.RemoveListener(OnClipUnload);
        }
        #endregion

        #region HELPERS
        // Format text
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
        // Clip unloaded externally
        protected virtual void OnClipUnload(TTSClipData clipData)
        {
            // Cancel load
            if (_queuedClips.Contains(clipData))
            {
                // Remove all references of the clip
                RemoveLoadingClip(clipData, true);
                // Perform cancell callbacks
                OnLoadCancel(clipData);
            }
            // Cancel playback
            if (clipData == SpeakingClip)
            {
                StopSpeaking();
            }
        }
        #endregion

        #region INTERACTIONS
        /// <summary>
        /// Load a tts clip using the specified text & cache settings.
        /// Plays clip immediately upon load & will cancel all previously loading/spoken phrases.
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) => Speak(textToSpeak, diskCacheSettings, false);
        public void Speak(string textToSpeak) => Speak(textToSpeak, null);
        /// <summary>
        /// Load a tts clip using the specified text & cache settings.
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public void SpeakQueued(string textToSpeak, TTSDiskCacheSettings diskCacheSettings) => Speak(textToSpeak, diskCacheSettings, true);
        public void SpeakQueued(string textToSpeak) => SpeakQueued(textToSpeak, null);

        /// <summary>
        /// Loads a formated phrase to be spoken
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        public void SpeakFormat(string format, params string[] textsToSpeak) =>
            Speak(GetFormattedText(format, textsToSpeak), null, false);
        /// <summary>
        /// Loads a formated phrase to be spoken
        /// Adds clip to speak queue and will speak once previously spoken phrases are complete
        /// </summary>
        /// <param name="format">Format string to be filled in with texts</param>
        public void SpeakFormatQueued(string format, params string[] textsToSpeak) =>
            Speak(GetFormattedText(format, textsToSpeak), null, true);

        /// <summary>
        /// Speak and wait for load/playback completion
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakAsync(string textToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            Stop();
            yield return SpeakQueuedAsync(new string[] {textToSpeak}, diskCacheSettings);
        }
        public IEnumerator SpeakAsync(string textToSpeak)
        {
            yield return SpeakAsync(textToSpeak, null);
        }
        /// <summary>
        /// Speak and wait for load/playback completion
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak, TTSDiskCacheSettings diskCacheSettings)
        {
            // Speak each queued
            foreach (var textToSpeak in textsToSpeak)
            {
                SpeakQueued(textToSpeak, diskCacheSettings);
            }
            // Wait while loading/speaking
            yield return new WaitWhile(() => IsLoading || IsSpeaking);
        }
        public IEnumerator SpeakQueuedAsync(string[] textsToSpeak)
        {
            yield return SpeakQueuedAsync(textsToSpeak, null);
        }

        /// <summary>
        /// Loads a tts clip & handles playback
        /// </summary>
        /// <param name="textToSpeak">The text to be spoken</param>
        /// <param name="diskCacheSettings">Specific tts load caching settings</param>
        /// <param name="addToQueue">Whether or not this phrase should be enqueued into the speak queue</param>
        protected virtual void Speak(string textToSpeak, TTSDiskCacheSettings diskCacheSettings, bool addToQueue)
        {
            if (prependedText.Length > 0 && !prependedText.EndsWith(" "))
            {
                prependedText += " ";
            }
            if (appendedText.Length > 0 && !appendedText.StartsWith(" "))
            {
                appendedText = " " + appendedText;
            }
            textToSpeak = prependedText + textToSpeak + appendedText;

            // Ensure voice settings exist
            TTSVoiceSettings voiceSettings = VoiceSettings;
            if (voiceSettings == null)
            {
                VLog.E($"No voice found with preset id: {presetVoiceID}");
                return;
            }
            // Log if empty text
            if (string.IsNullOrEmpty(textToSpeak))
            {
                VLog.E("No text to speak provided");
                return;
            }

            // Get new clip if possible
            string newClipID = _tts.GetClipID(textToSpeak, voiceSettings);
            TTSClipData newClipData = _tts.GetRuntimeCachedClip(newClipID);

            // Cancel previous loading queue
            if (!addToQueue)
            {
                StopLoading();
            }

            // Begin playback
            if (newClipData != null && newClipData.loadState == TTSClipLoadState.Loaded)
            {
                // Cancel all playing clips
                if (!addToQueue)
                {
                    StopSpeaking();
                }

                // Add to queue
                _queuedClips.Enqueue(newClipData);
                Events?.OnClipDataQueued?.Invoke(newClipData);

                // Begin playback
                OnPlaybackReady(newClipData);
            }
            // Begin load/add load completion callback
            else
            {
                OnLoadBegin(textToSpeak, newClipID, voiceSettings, diskCacheSettings, addToQueue);
            }
        }
        // Stop loading all items in the queue
        public virtual void StopLoading()
        {
            // Ignore if not loading
            if (!IsLoading)
            {
                return;
            }

            // Cancel each clip from loading
            while (_queuedClips.Count > 0)
            {
                OnLoadCancel(_queuedClips.Dequeue());
            }
        }
        // Stop playback if possible
        public virtual void StopSpeaking()
        {
            // Cannot stop speaking when not currently speaking
            if (!IsSpeaking)
            {
                return;
            }

            // Cancel playback
            OnPlaybackComplete(true);
        }
        // Stops loading & speaking immediately
        public virtual void Stop()
        {
            StopLoading();
            StopSpeaking();
        }
        #endregion

        #region LOAD
        // Begin a load
        protected virtual void OnLoadBegin(string textToSpeak, string clipID, TTSVoiceSettings voiceSettings, TTSDiskCacheSettings diskCacheSettings, bool addToQueue)
        {
            // Perform load request (Always waits a frame to ensure callbacks occur first)
            DateTime startTime = DateTime.Now;
            TTSClipData newClip = _tts.Load(textToSpeak, clipID, voiceSettings, diskCacheSettings, (clipData, error) => OnClipLoadComplete(clipData, error, addToQueue, startTime));
            _queuedClips.Enqueue(newClip);

            // Load begin
            VLog.D($"Load Begin\nText: {textToSpeak}");
            Events?.OnClipDataQueued?.Invoke(newClip);
            Events?.OnClipDataLoadBegin?.Invoke(newClip);
            Events?.OnClipLoadBegin?.Invoke(this, newClip.textToSpeak);
        }
        // Load complete
        protected virtual void OnClipLoadComplete(TTSClipData clipData, string error, bool addToQueue, DateTime startTime)
        {
            // Invalid clip, ignore
            if (!_queuedClips.Contains(clipData))
            {
                return;
            }

            // Get duration
            double loadDuration = (DateTime.Now - startTime).TotalMilliseconds;

            // No clip returned
            if (clipData.clip == null)
            {
                error = "No clip returned";
            }
            // Load failed
            if (!string.IsNullOrEmpty(error))
            {
                RemoveLoadingClip(clipData, false);
                VLog.E($"Load Failed\nText: {clipData?.textToSpeak}\nDuration: {loadDuration:0.00}ms\n{error}");
                Events?.OnClipDataLoadFailed?.Invoke(clipData);
                Events?.OnClipLoadFailed?.Invoke(this, clipData.textToSpeak);
                return;
            }

            // Load success event
            VLog.D($"Load Success\nText: {clipData?.textToSpeak}\nDuration: {loadDuration:0.00}ms");
            Events?.OnClipDataLoadSuccess?.Invoke(clipData);
            Events?.OnClipLoadSuccess?.Invoke(this, clipData.textToSpeak);

            // Stop speaking except for this clip
            if (!addToQueue)
            {
                StopSpeaking();
            }

            // Playback ready
            OnPlaybackReady(clipData);
        }
        // Load cancelled
        protected virtual void OnLoadCancel(TTSClipData clipData)
        {
            VLog.D($"Load Cancelled\nText: {clipData?.textToSpeak}");
            Events?.OnClipDataLoadAbort?.Invoke(clipData);
            Events?.OnClipLoadAbort?.Invoke(this, clipData.textToSpeak);
        }
        // Remove first instance or all instances of clip
        private void RemoveLoadingClip(TTSClipData clipData, bool allInstances)
        {
            // If first & does not need all, dequeue clip
            if (!allInstances && _queuedClips.Peek().Equals(clipData))
            {
                _queuedClips.Dequeue();
                return;
            }

            // Otherwise create discard queue
            Queue<TTSClipData> discard = _queuedClips;
            _queuedClips = new Queue<TTSClipData>();

            // Iterate all items
            bool found = false;
            while (discard.Count > 0)
            {
                // Dequeue from discard
                TTSClipData check = discard.Dequeue();

                // Matching clip
                if (check.Equals(clipData))
                {
                    // First
                    if (!found)
                    {
                        found = true;
                    }
                    // Enqueue Duplicate
                    else if (!allInstances)
                    {
                        _queuedClips.Enqueue(check);
                    }
                }
                // Enqueue if check matches & not equal
                else if (check != null)
                {
                    _queuedClips.Enqueue(check);
                }
            }
        }
        #endregion

        #region PLAY
        // Wait for playback completion
        private Coroutine _waitForCompletion;

        // Playback ready
        protected virtual void OnPlaybackReady(TTSClipData clipData)
        {
            // Invalid clip, ignore
            if (!_queuedClips.Contains(clipData))
            {
                return;
            }

            // Playback ready
            VLog.D($"Playback Queued\nText: {clipData.textToSpeak}");
            Events?.OnAudioClipPlaybackReady?.Invoke(clipData.clip);
            Events?.OnClipDataPlaybackReady?.Invoke(clipData);

            // Attempt to play next in queue
            OnPlaybackBegin();
        }
        // Play next
        protected virtual void OnPlaybackBegin()
        {
            // Ignore if currently playing or nothing in uque
            if (SpeakingClip != null ||  _queuedClips.Count == 0)
            {
                return;
            }
            // Peek next clip
            TTSClipData clipData = _queuedClips.Peek();
            if (clipData == null || clipData.loadState == TTSClipLoadState.Error || clipData.loadState == TTSClipLoadState.Unloaded)
            {
                OnLoadCancel(clipData);
                return;
            }
            // Still preparing
            if (clipData.loadState != TTSClipLoadState.Loaded)
            {
                return;
            }
            // Somehow clip unloaded
            if (clipData.clip == null)
            {
                OnLoadCancel(clipData);
                return;
            }

            // Dequeue & apply
            SpeakingClip = _queuedClips.Dequeue();

            // Started speaking
            VLog.D($"Playback Begin\nText: {SpeakingClip.textToSpeak}");
            AudioSource.PlayOneShot(SpeakingClip.clip);

            // Callback events
            Events?.OnStartSpeaking?.Invoke(this, SpeakingClip.textToSpeak);
            Events?.OnTextPlaybackStart?.Invoke(SpeakingClip.textToSpeak);
            Events?.OnAudioClipPlaybackStart?.Invoke(SpeakingClip.clip);
            Events?.OnClipDataPlaybackStart?.Invoke(SpeakingClip);

            // Wait for completion
            if (_waitForCompletion != null)
            {
                StopCoroutine(_waitForCompletion);
                _waitForCompletion = null;
            }
            _waitForCompletion = StartCoroutine(WaitForCompletion());
        }
        // Wait for clip completion
        protected virtual IEnumerator WaitForCompletion()
        {
            // Use delta time to wait
            float elapsedTime = 0f;
            while (SpeakingClip != null && SpeakingClip.clip != null && elapsedTime < SpeakingClip.clip.length)
            {
                yield return new WaitForEndOfFrame();
                elapsedTime += Time.deltaTime;
            }
            // Playback completed
            OnPlaybackComplete(false);
        }
        // Completed playback
        protected virtual void OnPlaybackComplete(bool cancelled)
        {
            // Invalid
            if (SpeakingClip == null)
            {
                return;
            }

            // Old clip
            TTSClipData lastClipData = SpeakingClip;

            // Clear speaking clip
            SpeakingClip = null;
            // Stop playback handler
            if (_waitForCompletion != null)
            {
                StopCoroutine(_waitForCompletion);
                _waitForCompletion = null;
            }
            // Stop audio source playback
            if (AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }

            // Completed successfully
            if (!cancelled)
            {
                VLog.D($"Playback Complete\nText: {lastClipData.textToSpeak}");
                Events?.OnFinishedSpeaking?.Invoke(this, lastClipData.textToSpeak);
                Events?.OnTextPlaybackFinished?.Invoke(lastClipData.textToSpeak);
                Events?.OnAudioClipPlaybackFinished?.Invoke(lastClipData.clip);
                Events?.OnClipDataPlaybackFinished?.Invoke(lastClipData);
            }
            // Cancelled
            else
            {
                VLog.D($"Playback Cancelled\nText: {lastClipData?.textToSpeak}");
                Events?.OnCancelledSpeaking?.Invoke(this, lastClipData.textToSpeak);
                Events?.OnTextPlaybackCancelled?.Invoke(lastClipData.textToSpeak);
                Events?.OnAudioClipPlaybackCancelled?.Invoke(lastClipData.clip);
                Events?.OnClipDataPlaybackCancelled?.Invoke(lastClipData);
            }

            // Attempt to play next in queue
            OnPlaybackBegin();
        }
        #endregion
    }
}
