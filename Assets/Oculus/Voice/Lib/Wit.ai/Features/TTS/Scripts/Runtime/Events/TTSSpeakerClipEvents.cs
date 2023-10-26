/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Speech;
using UnityEngine;
using UnityEngine.Events;
using Meta.WitAi.TTS.Data;

namespace Meta.WitAi.TTS.Utilities
{
    /// <summary>
    /// A unity event that returns a TTSSpeaker & TTSClipData
    /// for a specific speaker playback request.
    /// </summary>
    [Serializable]
    public class TTSSpeakerClipEvent : UnityEvent<TTSSpeaker, TTSClipData> { }
    /// <summary>
    /// A unity event that returns a TTSSpeaker, TTSClipData & text
    /// for a specific speaker playback request
    /// </summary>
    [Serializable]
    public class TTSSpeakerClipMessageEvent : UnityEvent<TTSSpeaker, TTSClipData, string> { }

    /// <summary>
    /// A collection of events used for speaker tts playback.
    /// </summary>
    [Serializable]
    public class TTSSpeakerClipEvents : VoiceSpeechEvents
    {
        [Header("Speaker Lifecycle Events")]
        [SerializeField] [Tooltip("Initial callback as soon as the audio clip speak request is generated")]
        private TTSSpeakerClipEvent _onInit = new TTSSpeakerClipEvent();
        /// <summary>
        /// Initial callback as soon as the audio clip speak request is generated
        /// </summary>
        public TTSSpeakerClipEvent OnInit => _onInit;

        [SerializeField] [Tooltip("Final call for a 'Speak' request that is called following a load failure, load abort, playback cancellation or playback completion")]
        private TTSSpeakerClipEvent _onComplete = new TTSSpeakerClipEvent();
        /// <summary>
        /// Final call for a 'Speak' request that is called following a load failure,
        /// load abort, playback cancellation or playback completion
        /// </summary>
        public TTSSpeakerClipEvent OnComplete => _onComplete;

        [Header("Speaker Loading Events")]
        [SerializeField] [Tooltip("Called when TTS audio clip load begins")]
        private TTSSpeakerClipEvent _onLoadBegin = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip load begins
        /// </summary>
        public TTSSpeakerClipEvent OnLoadBegin => _onLoadBegin;

        [SerializeField] [Tooltip("Called when TTS audio clip load is cancelled")]
        private TTSSpeakerClipEvent _onLoadAbort = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip load is cancelled
        /// </summary>
        public TTSSpeakerClipEvent OnLoadAbort => _onLoadAbort;

        [SerializeField] [Tooltip("Called when TTS audio clip load fails due to a network or load error")]
        private TTSSpeakerClipMessageEvent _onLoadFailed = new TTSSpeakerClipMessageEvent();
        /// <summary>
        /// Called when TTS audio clip load fails due to a network or load error
        /// </summary>
        public TTSSpeakerClipMessageEvent OnLoadFailed => _onLoadFailed;

        [SerializeField] [Tooltip("Called when TTS audio clip load successfully")]
        private TTSSpeakerClipEvent _onLoadSuccess = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip load successfully
        /// </summary>
        public TTSSpeakerClipEvent OnLoadSuccess => _onLoadSuccess;

        [Header("Speaker Playback Events")]
        [SerializeField] [Tooltip("Called when TTS audio clip playback is ready")]
        private TTSSpeakerClipEvent _onPlaybackReady = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip playback is ready
        /// </summary>
        public TTSSpeakerClipEvent OnPlaybackReady => _onPlaybackReady;

        [SerializeField] [Tooltip("Called when TTS audio clip playback has begun")]
        private TTSSpeakerClipEvent _onPlaybackStart = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip playback has begun
        /// </summary>
        public TTSSpeakerClipEvent OnPlaybackStart => _onPlaybackStart;

        [SerializeField] [Tooltip("Called when TTS audio clip playback been cancelled")]
        private TTSSpeakerClipMessageEvent _onPlaybackCancelled = new TTSSpeakerClipMessageEvent();
        /// <summary>
        /// Called when TTS audio clip playback been cancelled
        /// </summary>
        public TTSSpeakerClipMessageEvent OnPlaybackCancelled => _onPlaybackCancelled;

        [SerializeField] [Tooltip("Called when TTS audio clip is updated during streamed playback")]
        private TTSSpeakerClipEvent _onPlaybackClipUpdated = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip is updated during streamed playback
        /// </summary>
        public TTSSpeakerClipEvent OnPlaybackClipUpdated => _onPlaybackClipUpdated;

        [SerializeField] [Tooltip("Called when TTS audio clip playback completed successfully")]
        private TTSSpeakerClipEvent _onPlaybackComplete = new TTSSpeakerClipEvent();
        /// <summary>
        /// Called when TTS audio clip playback completed successfully
        /// </summary>
        public TTSSpeakerClipEvent OnPlaybackComplete => _onPlaybackComplete;
    }
}
