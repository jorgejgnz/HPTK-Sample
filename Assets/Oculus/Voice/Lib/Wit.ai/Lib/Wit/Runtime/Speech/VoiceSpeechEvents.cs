/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.Speech
{
    [Serializable]
    public class VoiceTextEvent : UnityEvent<string> { }
    [Serializable]
    public class VoiceAudioEvent : UnityEvent<AudioClip> { }
    [Serializable]
    public class VoiceSpeechEvents
    {
        [Header("Text Events")]
        [Tooltip("Called when speech begins with the provided phrase")]
        public VoiceTextEvent OnTextPlaybackStart = new VoiceTextEvent();
        [Tooltip("Called when speech playback is cancelled")]
        public VoiceTextEvent OnTextPlaybackCancelled = new VoiceTextEvent();
        [Tooltip("Called when speech playback completes successfully")]
        public VoiceTextEvent OnTextPlaybackFinished = new VoiceTextEvent();

        [Header("Audio Clip Events")]
        [Tooltip("Called when a clip is ready for playback")]
        public VoiceAudioEvent OnAudioClipPlaybackReady = new VoiceAudioEvent();
        [Tooltip("Called when a clip playback has begun")]
        public VoiceAudioEvent OnAudioClipPlaybackStart = new VoiceAudioEvent();
        [Tooltip("Called when a clip playback has been cancelled")]
        public VoiceAudioEvent OnAudioClipPlaybackCancelled = new VoiceAudioEvent();
        [Tooltip("Called when a clip playback has completed successfully")]
        public VoiceAudioEvent OnAudioClipPlaybackFinished = new VoiceAudioEvent();
    }
}
