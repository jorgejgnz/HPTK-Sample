/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.Windows;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Lib;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Utilities;
using UnityEngine;

namespace Meta.WitAi.TTS.Voices
{
    [CustomPropertyDrawer(typeof( TTSWitVoiceSettings))]
    public class TTSWitVoiceSettingsDrawer : PropertyDrawer
    {
        // Constants for var layout
        private const float VAR_HEIGHT = 20f;
        private const float VAR_MARGIN = 4f;
        private const float FIELD_HEIGHT = 60f;

        // Constants for var lookup
        private const string VAR_SETTINGS = "SettingsId";
        private const string VAR_VOICE = "voice";
        private const string VAR_STYLE = "style";

        // Voice data
        private TTSService _ttsService;
        private bool _onSpeaker;
        private IWitRequestConfiguration _configuration;
        private bool _configUpdating;
        private WitVoiceInfo[] _voices;
        private string[] _voiceNames;

        // Subfields
        private static readonly FieldInfo[] _fields = FieldGUI.GetFields(typeof( TTSWitVoiceSettings));

        // Determine height
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            // Get service
            if (!_ttsService)
            {
                GetService(property, out _ttsService, out _onSpeaker);
            }
            // Property
            if (!property.isExpanded)
            {
                return VAR_HEIGHT;
            }
            // Add each field + 1 for dropdown
            int total = _fields.Length + 1;
            // Remove 3 on speaker
            if (_onSpeaker)
            {
                total -= 3;
            }
            // Add 2 for voice properties
            int voiceIndex = GetVoiceIndex(property);
            if (voiceIndex != -1)
            {
                total += 2;
            }
            // Get height
            float height = total * VAR_HEIGHT + Mathf.Max(0, total - 1) * VAR_MARGIN;
            // Add fields
            if (!_onSpeaker)
            {
                height += 2 * (FIELD_HEIGHT - VAR_HEIGHT);
            }
            return height;
        }

        // Handles gui layout
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Get service if needed
            if (!_ttsService)
            {
                GetService(property, out _ttsService, out _onSpeaker);
            }

            // On gui
            float y = position.y;
            string voiceName = property.FindPropertyRelative(VAR_SETTINGS).stringValue;
            if (string.IsNullOrEmpty(voiceName))
            {
                voiceName = property.displayName;
            }
            property.isExpanded =
                EditorGUI.Foldout(new Rect(position.x, y, position.width, VAR_HEIGHT), property.isExpanded, voiceName);
            if (!property.isExpanded)
            {
                return;
            }
            y += VAR_HEIGHT + VAR_MARGIN;

            // Increment
            EditorGUI.indentLevel++;

            // Refresh voices if needed
            RefreshVoices(_ttsService, property);
            // Get voice index
            int voiceIndex = GetVoiceIndex(property);

            // Iterate subfields
            for (int s = 0; s < _fields.Length; s++)
            {
                // Get subfield
                FieldInfo subfield = _fields[s];

                // Ignore base fields on speaker
                if (_onSpeaker && subfield.DeclaringType == typeof(TTSVoiceSettings))
                {
                    continue;
                }

                // Get subfield property
                SerializedProperty subfieldProperty = property.FindPropertyRelative(subfield.Name);
                Rect subfieldRect = new Rect(position.x, y, position.width, VAR_HEIGHT);
                if (string.Equals(subfield.Name, VAR_VOICE) && voiceIndex != -1)
                {
                    int newVoiceIndex = EditorGUI.Popup(subfieldRect, subfieldProperty.displayName, voiceIndex,
                        _voiceNames);
                    newVoiceIndex = Mathf.Clamp(newVoiceIndex, 0, _voiceNames.Length);
                    if (voiceIndex != newVoiceIndex)
                    {
                        voiceIndex = newVoiceIndex;
                        subfieldProperty.stringValue = _voiceNames[voiceIndex];
                        GUI.FocusControl(null);
                    }
                    y += VAR_HEIGHT + VAR_MARGIN;
                    continue;
                }
                if (string.Equals(subfield.Name, VAR_STYLE) && voiceIndex >= 0 && voiceIndex < _voices.Length)
                {
                    // Get voice data
                    WitVoiceInfo voiceInfo = _voices[voiceIndex];
                    EditorGUI.indentLevel++;

                    // Locale layout
                    EditorGUI.LabelField(subfieldRect, "Locale", voiceInfo.locale);
                    y += VAR_HEIGHT + VAR_MARGIN;

                    // Gender layout
                    subfieldRect = new Rect(position.x, y, position.width, VAR_HEIGHT);
                    EditorGUI.LabelField(subfieldRect, "Gender", voiceInfo.gender);
                    y += VAR_HEIGHT + VAR_MARGIN;

                    // Style layout/select
                    subfieldRect = new Rect(position.x, y, position.width, VAR_HEIGHT);
                    if (voiceInfo.styles != null && voiceInfo.styles.Length > 0)
                    {
                        // Get style index
                        string style = subfieldProperty.stringValue;
                        int styleIndex = new List<string>(voiceInfo.styles).IndexOf(style);

                        // Show style select
                        int newStyleIndex = EditorGUI.Popup(subfieldRect, subfieldProperty.displayName, styleIndex,
                            voiceInfo.styles);
                        newStyleIndex = Mathf.Clamp(newStyleIndex, 0, voiceInfo.styles.Length);
                        if (styleIndex != newStyleIndex)
                        {
                            // Apply style
                            styleIndex = newStyleIndex;
                            subfieldProperty.stringValue = voiceInfo.styles[styleIndex];
                            GUI.FocusControl(null);
                        }

                        // Move down
                        y += VAR_HEIGHT + VAR_MARGIN;
                        EditorGUI.indentLevel--;
                        continue;
                    }

                    // Undent
                    EditorGUI.indentLevel--;
                }

                //
                TextAreaAttribute area = subfield.GetCustomAttribute<TextAreaAttribute>();
                if (area != null)
                {
                    subfieldRect.height = FIELD_HEIGHT;
                    y += FIELD_HEIGHT - VAR_HEIGHT;
                }

                // Default layout
                EditorGUI.PropertyField(subfieldRect, subfieldProperty, new GUIContent(subfieldProperty.displayName));

                // Clamp in between range
                RangeAttribute range = subfield.GetCustomAttribute<RangeAttribute>();
                if (range != null)
                {
                    int newValue = Mathf.Clamp(subfieldProperty.intValue, (int)range.min, (int)range.max);
                    if (subfieldProperty.intValue != newValue)
                    {
                        subfieldProperty.intValue = newValue;
                    }
                }

                // Increment
                y += VAR_HEIGHT + VAR_MARGIN;
            }

            // Undent
            EditorGUI.indentLevel--;
        }
        // Get service & if on a speaker
        private void GetService(SerializedProperty property, out TTSService ttsService, out bool onSpeaker)
        {
            // Get tts wit if possible
            object targetObject = property.serializedObject.targetObject;
            if (targetObject != null)
            {
                // Currently on TTSService
                if (targetObject is TTSService service)
                {
                    ttsService = service;
                    onSpeaker = false;
                    return;
                }
                // Currently on TTSSpeaker
                if (targetObject is TTSSpeaker speaker)
                {
                    ttsService = speaker.TTSService;
                    onSpeaker = true;
                    return;
                }
            }
            // Speaker not found
            ttsService = null;
            onSpeaker = false;
        }
        // Refresh voices
        private void RefreshVoices(TTSService ttsService, SerializedProperty property)
        {
            // Ensure service exists
            if (!(ttsService is TTSWit))
            {
                return;
            }
            // Get configuration
            IWitRequestConfiguration configuration = (ttsService as TTSWit).RequestSettings.configuration;
            // Set configuration
            if (_configuration != configuration)
            {
                _configuration = configuration;
                _voices = null;
                _voiceNames = null;
                _configUpdating = false;
            }
            // Ignore if null
            if (configuration == null)
            {
                return;
            }
            // Ignore if already set up
            if (_voices != null && _voiceNames != null && !_configUpdating)
            {
                return;
            }
            // Get voices
            _voices = configuration.GetApplicationInfo().voices;
            _voiceNames = _voices?.Select(voice => voice.name).ToArray();

            // Voices found!
            if (_voices != null && _voices.Length > 0)
            {
                _configUpdating = false;
            }
            // Configuration needs voices, perform update
            else if (!_configUpdating)
            {
                // Perform update if possible
                if (_configuration is WitConfiguration witConfig && !witConfig.IsUpdatingData())
                {
                    witConfig.RefreshAppInfo();
                }
                // Now updating
                _configUpdating = true;
            }
        }
        // Get voice index
        private int GetVoiceIndex(SerializedProperty property)
        {
            SerializedProperty voiceProperty = property.FindPropertyRelative(VAR_VOICE);
            string voiceID = voiceProperty.stringValue;
            int voiceIndex = -1;
            List<string> voiceNames = new List<string>();
            if (_voiceNames != null)
            {
                voiceNames.AddRange(_voiceNames);
            }
            if (voiceNames.Count > 0)
            {
                if (string.IsNullOrEmpty(voiceID))
                {
                    voiceIndex = 0;
                    voiceID = voiceNames[0];
                    voiceProperty.stringValue = voiceID;
                    GUI.FocusControl(null);
                }
                else
                {
                    voiceIndex = voiceNames.IndexOf(voiceID);
                }
            }
            return voiceIndex;
        }
    }
}
