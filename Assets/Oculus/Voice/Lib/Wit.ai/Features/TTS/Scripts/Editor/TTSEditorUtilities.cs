/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.TTS.Integrations;
using Meta.WitAi.TTS.Utilities;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Lib;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.TTS
{
    public static class TTSEditorUtilities
    {
        // Default TTS Setup
        public static Transform CreateDefaultSetup()
        {
            // Generate parent
            Transform parent = GenerateGameObject("TTS").transform;

            // Add TTS Service
            TTSService service = CreateService(parent);

            // Add TTS Speaker
            CreateSpeaker(parent, service);

            // Select parent
            Selection.activeObject = parent.gameObject;
            return parent;
        }

        // Default TTS Service
        public static TTSService CreateService(Transform parent = null, bool ignoreErrors = false)
        {
            // Get parent
            if (parent == null)
            {
                Transform selected = Selection.activeTransform;
                if (selected != null && selected.gameObject.scene.rootCount > 0)
                {
                    parent = Selection.activeTransform;
                }
            }
            // Ignore if found
            TTSService instance = GameObject.FindObjectOfType<TTSService>();
            if (instance != null)
            {
                // Log
                if (!ignoreErrors)
                {
                    VLog.W($"TTS Service - A TTSService is already in scene\nGameObject: {instance.gameObject.name}");
                }

                // Move into parent
                if (parent != null)
                {
                    instance.transform.SetParent(parent, true);
                }
            }

            // Generate TTSWit
            else
            {
                instance = CreateWitService(parent);
            }

            // Select & return instance
            Selection.activeObject = instance.gameObject;
            return instance;
        }

        // Default TTS Service
        private static TTSWit CreateWitService(Transform parent = null)
        {
            // Generate new TTSWit & add caches
            TTSWit ttsWit = GenerateGameObject("TTSWitService", parent).AddComponent<TTSWit>();
            ttsWit.gameObject.AddComponent<TTSRuntimeCache>();
            ttsWit.gameObject.AddComponent<TTSDiskCache>();
            VLog.D($"TTS Service - Instantiated Service {ttsWit.gameObject.name}");

            // Refresh configuration
            WitConfiguration configuration = SetupConfiguration(ttsWit);
            if (configuration != null)
            {
                RefreshAvailableVoices(ttsWit);
            }

            // Log
            return ttsWit;
        }

        // Wit configuration
        private static WitConfiguration SetupConfiguration(TTSService instance)
        {
            // Ignore non-tts wit
            if (instance.GetType() != typeof(TTSWit))
            {
                return null;
            }
            // Already setup
            TTSWit ttsWit = instance as TTSWit;
            if (ttsWit.RequestSettings.configuration != null)
            {
                return ttsWit.RequestSettings.configuration;
            }

            // Refresh configuration list
            if (WitConfigurationUtility.WitConfigs == null)
            {
                WitConfigurationUtility.ReloadConfigurationData();
            }

            // Assign first wit configuration found
            if (WitConfigurationUtility.WitConfigs != null && WitConfigurationUtility.WitConfigs.Length > 0)
            {
                ttsWit.RequestSettings.configuration = WitConfigurationUtility.WitConfigs[0];
                VLog.D($"TTS Service - Assigned Wit Configuration {ttsWit.RequestSettings.configuration.name}");
            }

            // Warning
            if (ttsWit.RequestSettings.configuration == null)
            {
                VLog.W($"TTS Service - Please create and assign a WitConfiguration to TTSWit");
            }

            // Return configuration
            return ttsWit.RequestSettings.configuration;
        }

        // Refresh available voices
        internal static void RefreshAvailableVoices(TTSWit ttsWit, Action<WitAppInfo> onUpdateComplete = null)
        {
            // Fail without configuration
            if (ttsWit == null)
            {
                VLog.W($"TTS Service - Cannot refresh voices without TTS Wit Service");
                return;
            }
            IWitRequestConfiguration configuration = ttsWit.RequestSettings.configuration;
            if (configuration == null)
            {
                VLog.W($"TTS Service - Cannot refresh voices without TTS Wit Configuration");
                return;
            }

            // Update app info
            WitAppInfoUtility.Update(ttsWit.RequestSettings.configuration, (newInfo, r) =>
            {
                configuration.SetApplicationInfo(newInfo);
                UpdatePresets(ttsWit, newInfo);
                onUpdateComplete?.Invoke(newInfo);
            });
        }

        // Add all presets
        private static void UpdatePresets(TTSWit ttsWit, WitAppInfo appInfo)
        {
            // Cannot update presets without voices
            if (appInfo.voices == null || appInfo.voices.Length == 0)
            {
                VLog.W("TTS Refresh failed to find voices");
                return;
            }

            // Add all voices to preset voice list
            if (ttsWit.PresetWitVoiceSettings == null || ttsWit.PresetWitVoiceSettings.Length == 0)
            {
                AddPresetsForInfo(ttsWit, appInfo.voices);
            }

            // Refresh speakers
            RefreshEmptySpeakers(ttsWit);
        }

        // Adds a list of voice infos
        internal static void AddPresetsForInfo(TTSWit ttsWit, WitVoiceInfo[] voiceInfos)
        {
            // Ignore without infos
            if (voiceInfos == null || voiceInfos.Length == 0)
            {
                return;
            }

            // Add all voices to preset voice list
            List<TTSWitVoiceSettings> voices = new List<TTSWitVoiceSettings>();
            if (ttsWit.PresetWitVoiceSettings != null)
            {
                voices.AddRange(ttsWit.PresetWitVoiceSettings);
            }
            foreach (var voiceData in voiceInfos)
            {
                voices.Add(GetDefaultVoiceSetting(voiceData));
            }
            ttsWit.SetVoiceSettings(voices.ToArray());
        }

        // Adds a preset for a specific voice
        internal static void AddPresetForInfo(TTSWit ttsWit, WitVoiceInfo voiceData)
        {
            List<TTSWitVoiceSettings> voices = new List<TTSWitVoiceSettings>();
            if (ttsWit.PresetWitVoiceSettings != null)
            {
                voices.AddRange(ttsWit.PresetWitVoiceSettings);
            }
            voices.Add(GetDefaultVoiceSetting(voiceData));
            ttsWit.SetVoiceSettings(voices.ToArray());
        }

        // Get default voice settings
        private static TTSWitVoiceSettings GetDefaultVoiceSetting(WitVoiceInfo voiceData)
        {
            TTSWitVoiceSettings result = new TTSWitVoiceSettings()
            {
                SettingsId = voiceData.name.ToUpper(),
                voice = voiceData.name
            };
            // Use first style provided
            if (voiceData.styles != null && voiceData.styles.Length > 0)
            {
                result.style = voiceData.styles[0];
            }
            return result;
        }
        // Set all blank IDs to default voice id
        private static void RefreshEmptySpeakers(TTSService service)
        {
            string defaultVoiceID = service.VoiceProvider.VoiceDefaultSettings?.SettingsId;
            foreach (var speaker in GameObject.FindObjectsOfType<TTSSpeaker>())
            {
                if (string.IsNullOrEmpty(speaker.presetVoiceID))
                {
                    speaker.presetVoiceID = defaultVoiceID;
                }
            }
        }

        // Default TTS Speaker
        public static TTSSpeaker CreateSpeaker(Transform parent = null, TTSService service = null)
        {
            // Get parent
            if (parent == null)
            {
                Transform selected = Selection.activeTransform;
                if (selected != null && selected.gameObject.scene.rootCount > 0)
                {
                    parent = Selection.activeTransform;
                }
            }
            // Generate service if possible
            if (service == null)
            {
                service = CreateService(parent);
            }

            // TTS Speaker
            string goName = typeof(TTSSpeaker).Name;
            TTSSpeaker speaker = GenerateGameObject(goName, parent).AddComponent<TTSSpeaker>();
            speaker.presetVoiceID = string.Empty;

            // Audio Source
            AudioSource audio = GenerateGameObject($"{goName}Audio", speaker.transform).AddComponent<AudioSource>();
            audio.playOnAwake = false;
            audio.loop = false;
            audio.spatialBlend = 0f; // Default to 2D

            // Return speaker
            VLog.D($"TTS Service - Instantiated Speaker {speaker.gameObject.name}");
            Selection.activeObject = speaker.gameObject;
            return speaker;
        }

        // Generate with specified name
        private static GameObject GenerateGameObject(string name, Transform parent = null)
        {
            Transform result = new GameObject(name).transform;
            result.SetParent(parent);
            result.localPosition = Vector3.zero;
            result.localRotation = Quaternion.identity;
            result.localScale = Vector3.one;
            return result.gameObject;
        }
    }
}
