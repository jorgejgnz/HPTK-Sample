/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace Meta.WitAi.Dictation.Events
{
    [Serializable]
    public class DictationEvents : EventRegistry, ITranscriptionEvent, IAudioInputEvents
    {
        private const string EVENT_CATEGORY_TRANSCRIPTION_EVENTS = "Transcription Events";
        private const string EVENT_CATEGORY_MIC_EVENTS = "Mic Events";
        private const string EVENT_CATEGORY_DICTATION_EVENTS = "Dictation Events";
        private const string EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS = "Activation Result Events";

        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [FormerlySerializedAs("OnPartialTranscription")]
        [Tooltip("Message fired when a partial transcription has been received.")]
        public WitTranscriptionEvent onPartialTranscription = new WitTranscriptionEvent();

        [EventCategory(EVENT_CATEGORY_TRANSCRIPTION_EVENTS)]
        [FormerlySerializedAs("OnFullTranscription")]
        [Tooltip("Message received when a complete transcription is received.")]
        public WitTranscriptionEvent onFullTranscription = new WitTranscriptionEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        [Tooltip("Called when a response from Wit.ai has been received")]
        public WitResponseEvent onResponse = new WitResponseEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        public UnityEvent onStart = new UnityEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        public UnityEvent onStopped = new UnityEvent();

        [EventCategory(EVENT_CATEGORY_ACTIVATION_RESULT_EVENTS)]
        public WitErrorEvent onError = new WitErrorEvent();

        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        public DictationSessionEvent onDictationSessionStarted = new DictationSessionEvent();

        [EventCategory(EVENT_CATEGORY_DICTATION_EVENTS)]
        public DictationSessionEvent onDictationSessionStopped = new DictationSessionEvent();

        [EventCategory(EVENT_CATEGORY_MIC_EVENTS)]
        public WitMicLevelChangedEvent onMicAudioLevel = new WitMicLevelChangedEvent();

        #region Shared Event API - Transcription

        public WitTranscriptionEvent OnPartialTranscription => onPartialTranscription;
        public WitTranscriptionEvent OnFullTranscription => onFullTranscription;

        #endregion

        #region Shared Event API - Microphone

        public WitMicLevelChangedEvent OnMicAudioLevelChanged => onMicAudioLevel;
        public UnityEvent OnMicStartedListening => onStart;
        public UnityEvent OnMicStoppedListening => onStopped;

        #endregion
    }
}
