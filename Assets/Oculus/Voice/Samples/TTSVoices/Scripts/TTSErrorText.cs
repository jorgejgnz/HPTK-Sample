/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.UI;
using Meta.WitAi.TTS;

namespace Meta.Voice.Samples.TTSVoices
{
    public class TTSErrorText : MonoBehaviour
    {
        // Label
        [SerializeField] private Text _errorLabel;

        // Current error response
        private string _error;

        // Reset on enable
        private void OnEnable()
        {
            RefreshText();
        }

        // Add listeners
        private void Update()
        {
            string invalidError = TTSService.Instance == null ? "No TTS Service Found" : TTSService.Instance.GetInvalidError();
            if (!string.Equals(invalidError, _error))
            {
                _error = invalidError;
                RefreshText();
            }
        }

        // Refresh text
        private void RefreshText()
        {
            if (string.IsNullOrEmpty(_error))
            {
                _errorLabel.text = string.Empty;
            }
            else
            {
                _errorLabel.text = $"Error: {_error}";
            }
        }
    }
}
