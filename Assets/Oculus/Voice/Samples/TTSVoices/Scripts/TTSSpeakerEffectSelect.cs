/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using UnityEngine;
using Meta.WitAi;

namespace Meta.Voice.Samples.TTSVoices
{
    /// <summary>
    /// A demo script that uses two dropdown menus to adjust the
    /// SSML tags of a TTSSpeaker.
    /// </summary>
    public class TTSSpeakerEffectSelect : TTSSpeakerObserver
    {
        [Header("Effect Settings")]
        [SerializeField] [Tooltip("Dropdown used for character selection")]
        private SimpleDropdownList _characterDropdown;
        [SerializeField] [Tooltip("Dropdown used for environment selection")]
        private SimpleDropdownList _environmentDropdown;

        // Supported IDs
        private const string NONE_ID = "NONE";
        private static readonly string[] CHARACTER_IDS = new [] {NONE_ID, "CHIPMUNK", "MONSTER", "ROBOT", "DAEMON"};
        private static readonly string[] ENVIRONMENT_IDS = new [] {NONE_ID, "REVERB", "ROOM", "PHONE", "PA", "CATHEDRAL"};

        protected override void OnEnable()
        {
            base.OnEnable();
            RefreshDropdowns();
            _characterDropdown.OnIndexSelected.AddListener(OnCharacterSelected);
            _environmentDropdown.OnIndexSelected.AddListener(OnEnvironmentSelected);
        }
        protected override void OnDisable()
        {
            base.OnDisable();
            _characterDropdown.OnIndexSelected.RemoveListener(OnCharacterSelected);
            _environmentDropdown.OnIndexSelected.RemoveListener(OnEnvironmentSelected);
        }

        // Refresh dropdown using voice settings
        private void RefreshDropdowns()
        {
            if (!Speaker)
            {
                VLog.W("No speaker found");
                return;
            }

            // Refresh characters
            RefreshDropdown("character", _characterDropdown, CHARACTER_IDS);

            // Refresh environments
            RefreshDropdown("environments", _environmentDropdown, ENVIRONMENT_IDS);
        }
        // Refresh a specific dropdown
        private void RefreshDropdown(string id, SimpleDropdownList dropdown, string[] options)
        {
            if (!dropdown)
            {
                VLog.W($"No {id} dropdown found");
                return;
            }
            if (options == null || options.Length == 0)
            {
                VLog.W($"No {id} options found");
                return;
            }

            // Load dropdown & select first index
            dropdown.LoadDropdown(options);
            dropdown.SelectOption(0);
        }

        // Apply voice on option select
        private void OnCharacterSelected(int newIndex)
        {
            RefreshSsml();
        }

        // Apply voice on option select
        private void OnEnvironmentSelected(int newIndex)
        {
            RefreshSsml();
        }

        // Refresh speaker ssml
        private void RefreshSsml()
        {
            if (!Speaker)
            {
                VLog.W("No speaker found");
                return;
            }

            // Get SSMLs
            StringBuilder prepend = new StringBuilder();
            StringBuilder append = new StringBuilder();

            // Add ssml tags
            prepend.Append("<speak>");
            append.Append("</speak>");

            // Get character & environment ids
            string characterId = _characterDropdown.SelectedOption;
            if (string.Equals(characterId, NONE_ID))
            {
                characterId = null;
            }
            string environmentId = _environmentDropdown.SelectedOption;
            if (string.Equals(environmentId, NONE_ID))
            {
                environmentId = null;
            }

            // Add sfx tag
            bool hasCharacter = !string.IsNullOrEmpty(characterId);
            bool hasEnvironment = !string.IsNullOrEmpty(environmentId);
            if (hasCharacter || hasEnvironment)
            {
                // Add prefix & postfix
                prepend.Append("<sfx");
                append.Insert(0, "</sfx>");

                // Add character
                if (hasCharacter)
                {
                    prepend.Append($" character=\"{characterId.ToLower()}\"");
                }
                // Add environment
                if (hasEnvironment)
                {
                    prepend.Append($" environment=\"{environmentId.ToLower()}\"");
                }

                // Finalize
                prepend.Append(">");
            }

            // Set SSML
            Speaker.PrependedText = prepend.ToString();
            Speaker.AppendedText = append.ToString();
        }
    }
}
