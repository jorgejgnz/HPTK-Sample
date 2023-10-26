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
using Meta.WitAi.TTS.Integrations;

namespace Meta.WitAi.TTS
{
    [CustomEditor(typeof(TTSWit), true)]
    public class TTSWitInspector : TTSServiceInspector
    {
        private int selectedBaseVoice;

        protected override void OnEditTimeGUI()
        {
            base.OnEditTimeGUI();

            var ttsWit = (TTSWit)target;
            var config = ttsWit.RequestSettings.configuration;
            if (!config) return;

            // Get app info for voices
            var appInfo = config.GetApplicationInfo();
            if (null != appInfo.voices && appInfo.voices.Length > 0)
            {
                // Get all voice names from wit
                string[] voiceNames = appInfo.voices.Select(v => v.name).ToArray();

                // Add a selected preset
                GUILayout.BeginHorizontal();
                selectedBaseVoice = EditorGUILayout.Popup(selectedBaseVoice, voiceNames);
                GUI.enabled = selectedBaseVoice >= 0 && selectedBaseVoice < appInfo.voices.Length;
                if (WitEditorUI.LayoutTextButton("Add Preset"))
                {
                    TTSEditorUtilities.AddPresetForInfo(ttsWit, appInfo.voices[selectedBaseVoice]);
                }
                GUILayout.EndHorizontal();

                // Add all unused presets
                GUI.enabled = true;
                if (GUILayout.Button("Add Unused Voices as Presets"))
                {
                    // Get used voices
                    List<string> usedVoiceNames = ttsWit.PresetWitVoiceSettings.Select(v => v.voice).ToList();

                    // Get unused voices
                    var unusedVoices = appInfo.voices.Where(v => !usedVoiceNames.Contains(v.name)).ToArray();

                    // Add all unused presets
                    TTSEditorUtilities.AddPresetsForInfo(ttsWit, unusedVoices);
                }
            }
            // Log warning
            else
            {
                GUILayout.Label("There are currently no base presets available. Click refresh to check for updates.", EditorStyles.helpBox);
            }

            // Refresh button
            if (GUILayout.Button("Refresh Presets"))
            {
                TTSEditorUtilities.RefreshAvailableVoices(ttsWit, info =>
                {
                    Repaint();
                });
            }
        }
    }
}
