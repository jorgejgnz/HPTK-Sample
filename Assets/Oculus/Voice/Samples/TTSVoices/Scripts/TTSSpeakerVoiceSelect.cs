/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Linq;
using UnityEngine;
using Meta.WitAi;

namespace Meta.Voice.Samples.TTSVoices
{
    /// <summary>
    /// A demo script that uses a dropdown menu to adjust the
    /// voice setting id of a TTSSpeaker.
    /// </summary>
    public class TTSSpeakerVoiceSelect : TTSSpeakerObserver
    {
        [SerializeField] [Tooltip("Dropdown used for voice selection")]
        private SimpleDropdownList _dropdown;

        protected override void Awake()
        {
            base.Awake();
            _dropdown.DropdownToggleUnselectedText = "CUSTOM";
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshDropdown();
            _dropdown.OnOptionSelected.AddListener(OnOptionSelected);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _dropdown.OnOptionSelected.RemoveListener(OnOptionSelected);
        }

        // Fix voice selection if changed elsewhere
        private void Update()
        {
            if (!string.Equals(Speaker?.VoiceSettings?.SettingsId, _dropdown.SelectedOption))
            {
                _dropdown.SelectOption(Speaker?.VoiceSettings?.SettingsId);
            }
        }

        // Refresh dropdown using voice settings
        private void RefreshDropdown()
        {
            if (!Speaker)
            {
                VLog.W("No speaker found");
                return;
            }
            if (!Speaker.TTSService)
            {
                VLog.W("No speaker service found");
                return;
            }
            if (!_dropdown)
            {
                VLog.W("No dropdown found");
                return;
            }

            // Get all voice names & load dropdown
            string[] voiceNames = Speaker.TTSService.GetAllPresetVoiceSettings()
                .Select((voiceSetting) => voiceSetting.SettingsId).ToArray();
            _dropdown.LoadDropdown(voiceNames);

            // Get selected voice &
            _dropdown.SelectOption(Speaker.presetVoiceID);
        }

        // Apply voice on option select
        private void OnOptionSelected(string newOption)
        {
            if (!Speaker)
            {
                VLog.W("No speaker found");
                return;
            }
            Speaker.presetVoiceID = newOption;
        }
    }
}
