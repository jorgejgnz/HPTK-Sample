/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.WitAi;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using UnityEngine;
using UnityEngine.UI;

namespace Oculus.VoiceSDK.UX
{
    [RequireComponent(typeof(Text))] [ExecuteInEditMode]
    public class VoiceTranscriptionLabel : MonoBehaviour
    {
        // The label to be updated
        public Text Label
        {
            get
            {
                if (_label == null)
                {
                    _label = gameObject.GetComponent<Text>();
                }
                return _label;
            }
        }
        private Text _label;

        [Header("Listen Settings")]
        [Tooltip("Various voice services to be observed")]
        [SerializeField] private VoiceService[] _voiceServices;
        [Tooltip("Text color while receiving text")]
        [SerializeField] private Color _transcriptionColor = Color.black;

        [Header("Prompt Settings")]
        [Tooltip("Color to be used for prompt text")]
        [SerializeField] private Color _promptColor = new Color(0.2f, 0.2f, 0.2f);
        [Tooltip("Prompt text that displays while listening but prior to completion")]
        [SerializeField] private string _promptDefault = "Press activate to begin listening";
        [Tooltip("Prompt text that displays while listening but prior to completion")]
        [SerializeField] private string _promptListening = "Listening...";

        [Header("Error Settings")]
        [Tooltip("Color to be used for error text")]
        [SerializeField] private Color _errorColor = new Color(0.8f, 0.2f, 0.2f);

        // If none found, grab all voice services
        private void Awake()
        {
            if (_voiceServices == null || _voiceServices.Length == 0)
            {
                _voiceServices = FindObjectsOfType<VoiceService>();
            }
        }

        // Add service delegates
        private void OnEnable()
        {
            if (_voiceServices != null)
            {
                foreach (var service in _voiceServices)
                {
                    service.VoiceEvents.OnStartListening.AddListener(OnStartListening);
                    service.VoiceEvents.OnPartialTranscription.AddListener(OnTranscriptionChange);
                    service.VoiceEvents.OnFullTranscription.AddListener(OnTranscriptionChange);
                    service.VoiceEvents.OnError.AddListener(OnError);
                    service.VoiceEvents.OnComplete.AddListener(OnComplete);
                }
            }
        }
        // Remove service delegates
        private void OnDisable()
        {
            if (_voiceServices != null)
            {
                foreach (var service in _voiceServices)
                {
                    service.VoiceEvents.OnStartListening.RemoveListener(OnStartListening);
                    service.VoiceEvents.OnPartialTranscription.RemoveListener(OnTranscriptionChange);
                    service.VoiceEvents.OnFullTranscription.RemoveListener(OnTranscriptionChange);
                    service.VoiceEvents.OnError.RemoveListener(OnError);
                    service.VoiceEvents.OnComplete.RemoveListener(OnComplete);
                }
            }
        }

        #if UNITY_EDITOR
        // Refresh prompt
        private void Update()
        {
            if (!Application.isPlaying)
            {
                SetText(_promptDefault, _promptColor);
            }
        }
        #endif

        // Set listening
        private void OnStartListening()
        {
            SetText(_promptListening, _promptColor);
        }
        // Set text change
        private void OnTranscriptionChange(string text)
        {
            SetText(text, _transcriptionColor);
        }
        // Apply error
        private void OnError(string status, string error)
        {
            SetText($"[{status}] {error}", _errorColor);
        }
        // If no text came through, show prompt
        private void OnComplete(VoiceServiceRequest request)
        {
            if (Label != null && string.Equals(Label?.text, _promptListening))
            {
                SetText(_promptDefault, _promptColor);
            }
        }

        // Refresh text
        private void SetText(string newText, Color newColor)
        {
            // Ignore if same
            if (Label == null || string.Equals(newText, Label.text) && newColor == Label.color)
            {
                return;
            }

            // Apply text & color
            _label.text = newText;
            _label.color = newColor;
        }
    }
}
