/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.TTS.Data;

namespace Meta.WitAi.TTS
{
    [CustomEditor(typeof(TTSSpeaker), true)]
    public class TTSSpeakerInspector : Editor
    {
        // Speaker
        private TTSSpeaker _speaker;
        private SerializedProperty _presetVoiceProperty;
        private SerializedProperty _customVoiceProperty;

        // Voices
        private int _voiceIndex = -1;
        private string[] _voicePresetIds = null;

        // Voice text
        private const string UI_PRESET_VOICE_KEY = "Voice Preset";
        private const string UI_CUSTOM_VOICE_KEY = "Custom Voice";
        private const string UI_CUSTOM_KEY = "CUSTOM";

        //
        void OnEnable()
        {
            _speaker = target as TTSSpeaker;
            _presetVoiceProperty = serializedObject.FindProperty("presetVoiceID");
            _customVoiceProperty = serializedObject.FindProperty("customWitVoiceSettings");
        }

        // GUI
        public override void OnInspectorGUI()
        {
            // Display default ui
            base.OnInspectorGUI();

            // Check voices
            TTSService tts = _speaker.TTSService;
            TTSVoiceSettings[] settings = tts?.GetAllPresetVoiceSettings();
            if (_voicePresetIds == null
                || (settings != null && _voicePresetIds.Length != settings.Length + 1)
                || (_voiceIndex >= 0 && _voiceIndex < _voicePresetIds.Length - 1 && !string.Equals(_speaker.presetVoiceID, _voicePresetIds[_voiceIndex])))
            {
                RefreshVoices(settings);
            }

            // No preset voices found, assume custom
            if (_voicePresetIds == null || _voicePresetIds.Length == 0)
            {
                GUI.enabled = false;
                EditorGUILayout.TextField(UI_PRESET_VOICE_KEY, UI_CUSTOM_KEY);
                GUI.enabled = true;
            }
            // Voice dropdown
            else
            {
                bool updated = false;
                WitEditorUI.LayoutPopup(UI_PRESET_VOICE_KEY, _voicePresetIds, ref _voiceIndex, ref updated);
                if (updated)
                {
                    if (_voiceIndex >= 0 && _voiceIndex < _voicePresetIds.Length - 1)
                    {
                        _presetVoiceProperty.stringValue = _voicePresetIds[_voiceIndex];
                    }
                    else
                    {
                        _presetVoiceProperty.stringValue = null;
                    }
                }
            }

            // Add custom layout
            if (_voicePresetIds == null || _voiceIndex < 0 || _voiceIndex >= _voicePresetIds.Length - 1)
            {
                EditorGUILayout.PropertyField(_customVoiceProperty, new GUIContent(UI_CUSTOM_VOICE_KEY));
            }

            // Apply all modified properties
            serializedObject.ApplyModifiedProperties();

            // Layout TTS clip queue
            LayoutClipQueue();
        }

        // Refresh voices
        private void RefreshVoices(TTSVoiceSettings[] settings)
        {
            // Reset voice data
            _voiceIndex = -1;
            _voicePresetIds = null;
            if (settings == null)
            {
                return;
            }

            // Get all ids
            List<string> presetIds = settings.Select(s => s.SettingsId).ToList();
            // Get voice index
            _voiceIndex = presetIds.IndexOf(_speaker.presetVoiceID);
            if (_voiceIndex == -1)
            {
                _voiceIndex = presetIds.Count;
            }
            // Add custom key
            presetIds.Add(UI_CUSTOM_KEY);
            // Apply preset ids
            _voicePresetIds = presetIds.ToArray();
        }

        // Layout clip queue
        private const string UI_CLIP_HEADER_TEXT = "Clip Queue";
        private const string UI_CLIP_SPEAKER_TEXT = "Speaker Clip:";
        private const string UI_CLIP_QUEUE_TEXT = "Loading Clips:";
        private bool _speakerFoldout = false;
        private bool _queueFoldout = false;
        private void LayoutClipQueue()
        {
            // Ignore unless playing
            if (!Application.isPlaying)
            {
                return;
            }

            // Add header
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(UI_CLIP_HEADER_TEXT, EditorStyles.boldLabel);

            // Speaker Foldout
            _speakerFoldout = EditorGUILayout.Foldout(_speakerFoldout, UI_CLIP_SPEAKER_TEXT);
            if (_speakerFoldout)
            {
                EditorGUI.indentLevel++;
                if (!_speaker.IsSpeaking)
                {
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    TTSServiceInspector.DrawClipGUI(_speaker.SpeakingClip);
                }
                EditorGUI.indentLevel--;
            }

            // Queue Foldout
            List<TTSClipData> queuedClips = _speaker.QueuedClips;
            _queueFoldout = EditorGUILayout.Foldout(_queueFoldout, $"{UI_CLIP_QUEUE_TEXT} {(queuedClips == null ? 0 : queuedClips.Count)}");
            if (_queueFoldout)
            {
                EditorGUI.indentLevel++;
                if (queuedClips == null || queuedClips.Count == 0)
                {
                    EditorGUILayout.LabelField("None");
                }
                else
                {
                    for (int i = 0; i < queuedClips.Count; i++)
                    {
                        TTSClipData clipData = queuedClips[i];
                        bool oldFoldout = WitEditorUI.GetFoldoutValue(clipData);
                        bool newFoldout = EditorGUILayout.Foldout(oldFoldout, $"Clip[{i}]");
                        if (oldFoldout != newFoldout)
                        {
                            WitEditorUI.SetFoldoutValue(clipData, newFoldout);
                        }
                        if (newFoldout)
                        {
                            EditorGUI.indentLevel++;
                            TTSServiceInspector.DrawClipGUI(clipData);
                            EditorGUI.indentLevel--;
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
