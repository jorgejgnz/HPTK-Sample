/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS.Utilities;

namespace Meta.Voice.Samples.TTSVoices
{
    public class TTSSpeakerInput : MonoBehaviour
    {
        // Speaker
        [SerializeField] private TTSSpeaker _speaker;

        // Default input
        [SerializeField] private InputField _input;
        [SerializeField] private Button _stopButton;
        [SerializeField] private Button _pauseButton;
        [SerializeField] private Button _speakButton;

        // Queue button that will not stop previous clip
        [SerializeField] private Toggle _queueButton;
        // Async toggle that will play a clip on completion
        [SerializeField] private Toggle _asyncToggle;
        [SerializeField] private AudioClip _asyncClip;

        [SerializeField] private string _dateId = "[DATE]";
        [SerializeField] private string[] _queuedText;

        // States
        private string _voice;
        private bool _loading;
        private bool _speaking;
        private bool _paused;

        // Add delegates
        private void OnEnable()
        {
            RefreshStopButton();
            RefreshPauseButton();
            _stopButton.onClick.AddListener(StopClick);
            _pauseButton.onClick.AddListener(PauseClick);
            _speakButton.onClick.AddListener(SpeakClick);
        }
        // Stop click
        private void StopClick() => _speaker.Stop();
        // Pause click
        private void PauseClick()
        {
            if (_speaker.IsPaused)
            {
                _speaker.Resume();
            }
            else
            {
                _speaker.Pause();
            }
        }
        // Speak phrase click
        private void SpeakClick()
        {
            // Speak phrase
            string phrase = FormatText(_input.text);
            bool queued = _queueButton != null && _queueButton.isOn;
            bool async = _asyncToggle != null && _asyncToggle.isOn;

            // Speak async
            if (async)
            {
                StartCoroutine(SpeakAsync(phrase, queued));
            }
            // Speak queued
            else if (queued)
            {
                _speaker.SpeakQueued(phrase);
            }
            // Speak
            else
            {
                _speaker.Speak(phrase);
            }

            // Queue additional phrases
            if (_queuedText != null && _queuedText.Length > 0 && queued)
            {
                foreach (var text in _queuedText)
                {
                    _speaker.SpeakQueued(FormatText(text));
                }
            }
        }
        // Speak async
        private IEnumerator SpeakAsync(string phrase, bool queued)
        {
            // Queue
            if (queued)
            {
                yield return _speaker.SpeakQueuedAsync(new string[] { phrase });
            }
            // Default
            else
            {
                yield return _speaker.SpeakAsync(phrase);
            }

            // Play complete clip
            if (_asyncClip != null)
            {
                _speaker.AudioSource.PlayOneShot(_asyncClip);
            }
        }
        // Format text with current datetime
        private string FormatText(string text)
        {
            string result = text;
            if (result.Contains(_dateId))
            {
                DateTime now = DateTime.Now;
                string dateString = $"{now.ToLongDateString()} at {now.ToLongTimeString()}";
                result = text.Replace(_dateId, dateString);
            }
            return result;
        }
        // Remove delegates
        private void OnDisable()
        {
            _stopButton.onClick.RemoveListener(StopClick);
            _speakButton.onClick.RemoveListener(SpeakClick);
        }

        // Preset text fields
        private void Update()
        {
            // On preset voice id update
            if (!string.Equals(_voice, _speaker.presetVoiceID))
            {
                _voice = _speaker.presetVoiceID;
                _input.placeholder.GetComponent<Text>().text = $"Write something to say in {_voice}'s voice";
            }
            // On state changes
            if (_loading != _speaker.IsLoading)
            {
                RefreshStopButton();
            }
            if (_speaking != _speaker.IsSpeaking)
            {
                RefreshStopButton();
            }
            if (_paused != _speaker.IsPaused)
            {
                RefreshPauseButton();
            }
        }
        // Refresh interactable based on states
        private void RefreshStopButton()
        {
            _loading = _speaker.IsLoading;
            _speaking = _speaker.IsSpeaking;
            _stopButton.interactable = _loading || _speaking;
        }
        // Refresh text based on pause state
        private void RefreshPauseButton()
        {
            _paused = _speaker.IsPaused;
            _pauseButton.GetComponentInChildren<Text>().text = _paused ? "Resume" : "Pause";
        }
    }
}
