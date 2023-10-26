/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Requests;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Inspectors
{
    public class WitInspector : Editor
    {
        // Text invocation message
        private string _activationMessage;

        // Target
        private IVoiceActivationHandler _activationHandler;
        private IVoiceEventProvider _eventProvider;

        // Current service request
        private VoiceServiceRequest _request;

        // Transcription data tracking
        private string _lastTranscription;

        // Mic data tracking
        private float _micMin;
        private float _micMax;
        private float _micCurrent;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // Ignore if not playing
            if (!Application.isPlaying)
            {
                return;
            }
            if (target is IVoiceEventProvider)
            {
                _eventProvider = (IVoiceEventProvider) target;
            }
            if (target is IVoiceActivationHandler)
            {
                _activationHandler = (IVoiceActivationHandler) target;
            }
            if (_activationHandler == null)
            {
                return;
            }

            // Header
            EditorGUILayout.Space();
            GUILayout.Label("Editor Requests", EditorStyles.boldLabel);

            // Add activation button
            if (_request != null && _request.IsActive)
            {
                // Deactivates current target
                GUILayout.BeginHorizontal();
                if (_request.InputType == NLPRequestInputType.Audio && GUILayout.Button("Deactivate"))
                {
                    _request.DeactivateAudio();
                }
                // Deactivate & abort
                if (GUILayout.Button("Deactivate & Abort"))
                {
                    _request.Cancel("Deactivated");
                }
                GUILayout.EndHorizontal();

                // Current request state
                GUILayout.Label($"State: {_request.State}");
            }
            else
            {
                // Activates via voice
                if (GUILayout.Button("Activate"))
                {
                    _request = _activationHandler.Activate(GetRequestOptions(), GetRequestEvents());
                }

                // Activates via text
                GUILayout.BeginHorizontal();
                _activationMessage = GUILayout.TextField(_activationMessage);
                if (GUILayout.Button("Send", GUILayout.Width(50)))
                {
                    _request = _activationHandler.Activate(_activationMessage, GetRequestOptions(), GetRequestEvents());
                }
                GUILayout.EndHorizontal();
            }
            EditorGUILayout.Space();

            // Transcription data
            GUILayout.Label("Last Transcription", EditorStyles.boldLabel);
            GUILayout.TextArea(_lastTranscription);

            // Mic data
            GUILayout.Label("Mic Status", EditorStyles.boldLabel);
            GUILayout.Label($"Mic range: {_micMin.ToString("F5")} - {_micMax.ToString("F5")}");
            GUILayout.Label($"Mic current: {_micCurrent.ToString("F5")}");
        }

        // Returns events
        private WitRequestOptions GetRequestOptions() => new WitRequestOptions();

        // Return events
        private VoiceServiceRequestEvents GetRequestEvents()
        {
            VoiceServiceRequestEvents events = new VoiceServiceRequestEvents();
            events.OnInit.AddListener(OnRequestInit);
            events.OnPartialTranscription.AddListener(OnTranscriptionChanged);
            events.OnFullTranscription.AddListener(OnTranscriptionChanged);
            events.OnComplete.AddListener(OnRequestComplete);
            return events;
        }

        // Setup during an activation
        private void OnRequestInit(VoiceServiceRequest request)
        {
            // Add events
            if (_eventProvider != null)
            {
                _eventProvider.VoiceEvents.OnMicLevelChanged.AddListener(OnMicLevelChanged);
            }

            // Init mic data
            _micMin = Mathf.Infinity;
            _micMax = Mathf.NegativeInfinity;

            // Start repaint on update
            EditorApplication.update += UpdateForRepaint;
        }

        // Mic level updates
        private void OnMicLevelChanged(float volume)
        {
            _micCurrent = volume;
            _micMin = Mathf.Min(volume, _micMin);
            _micMax = Mathf.Max(volume, _micMax);
        }

        // Transcription updates
        private void OnTranscriptionChanged(string transcription)
        {
            _lastTranscription = transcription;
        }

        // Repaint
        private void UpdateForRepaint()
        {
            Repaint();
        }

        // Request completed
        private void OnRequestComplete(VoiceServiceRequest request)
        {
            // Remove events
            if (_eventProvider != null)
            {
                _eventProvider.VoiceEvents.OnMicLevelChanged.RemoveListener(OnMicLevelChanged);
            }

            // Stop repaint on update
            EditorApplication.update -= UpdateForRepaint;

            // Remove request
            _request = null;
        }
    }
}
