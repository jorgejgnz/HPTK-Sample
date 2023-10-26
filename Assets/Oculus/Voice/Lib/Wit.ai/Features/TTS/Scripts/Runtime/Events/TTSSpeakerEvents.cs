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
using UnityEngine.Serialization;

namespace Meta.WitAi.TTS.Utilities
{
    [Serializable]
    public class TTSSpeakerEvent : UnityEvent<TTSSpeaker, string> { }
    [Serializable]
    public class TTSSpeakerClipDataEvent : UnityEvent<TTSClipData> { }
    [Serializable]
    public class TTSSpeakerEvents : TTSSpeakerClipEvents
    {
        [Header("Queue Events")]
        [Tooltip("Called when a tts request is added to an empty queue")]
        [SerializeField] [FormerlySerializedAs("OnPlaybackQueueBegin")]
        private UnityEvent _onPlaybackQueueBegin = new UnityEvent();
        public UnityEvent OnPlaybackQueueBegin => _onPlaybackQueueBegin;

        [Tooltip("Called the final request is removed from a queue")]
        [SerializeField] [FormerlySerializedAs("OnPlaybackQueueComplete")]
        private UnityEvent _onPlaybackQueueComplete = new UnityEvent();
        public UnityEvent OnPlaybackQueueComplete => _onPlaybackQueueComplete;

        [Header("Deprecated Events")]
        [Obsolete("Use 'OnLoadBegin' event")]
        public TTSSpeakerClipDataEvent OnClipDataQueued;
        [Obsolete("Use 'OnLoadBegin' event")]
        public TTSSpeakerEvent OnClipLoadBegin;
        [Obsolete("Use 'OnLoadBegin' event")]
        public TTSSpeakerClipDataEvent OnClipDataLoadBegin;
        [Obsolete("Use 'OnLoadAbort' event")]
        public TTSSpeakerEvent OnClipLoadAbort;
        [Obsolete("Use 'OnLoadAbort' event")]
        public TTSSpeakerClipDataEvent OnClipDataLoadAbort;
        [Obsolete("Use 'OnLoadFailed' event")]
        public TTSSpeakerEvent OnClipLoadFailed;
        [Obsolete("Use 'OnLoadFailed' event")]
        public TTSSpeakerClipDataEvent OnClipDataLoadFailed;
        [Obsolete("Use 'OnLoadSuccess' event")]
        public TTSSpeakerEvent OnClipLoadSuccess;
        [Obsolete("Use 'OnLoadSuccess' event")]
        public TTSSpeakerClipDataEvent OnClipDataLoadSuccess;
        [Obsolete("Use 'OnPlaybackReady' event")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackReady;
        [Obsolete("Use 'OnPlaybackStart' event")]
        public TTSSpeakerEvent OnStartSpeaking;
        [Obsolete("Use 'OnPlaybackStart' event")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackStart;
        [Obsolete("Use 'OnPlaybackCancelled' event")]
        public TTSSpeakerEvent OnCancelledSpeaking;
        [Obsolete("Use 'OnPlaybackCancelled' event")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackCancelled;
        [Obsolete("Use 'OnPlaybackComplete' event")]
        public TTSSpeakerEvent OnFinishedSpeaking;
        [Obsolete("Use 'OnPlaybackComplete' event")]
        public TTSSpeakerClipDataEvent OnClipDataPlaybackFinished;
    }
}
