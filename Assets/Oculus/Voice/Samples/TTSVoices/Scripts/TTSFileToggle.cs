/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Requests;
using Meta.WitAi.TTS;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Integrations;
using UnityEngine;
using UnityEngine.UI;

namespace Meta.Voice.Samples.TTSVoices
{
    public class TTSFileToggle : MonoBehaviour
    {
        // UI references
        [SerializeField] private TTSWit _service;
        [SerializeField] private Button _button;
        [SerializeField] private Text _label;
        [SerializeField] private string _labelFormat = "File Type: {0}";

        // Current audio file type
        private TTSWitAudioType _fileType = (TTSWitAudioType) (-1);
        private List<TTSWitAudioType> _fileTypes = new List<TTSWitAudioType>(Enum.GetValues(typeof(TTSWitAudioType)) as TTSWitAudioType[]);

        // Add listeners
        private void OnEnable()
        {
            if (_service == null)
            {
                _service = GameObject.FindObjectOfType<TTSWit>();
            }
            _button.onClick.AddListener(Toggle);
        }
        // Check for changes
        private void Update()
        {
            if (_fileType != _service.RequestSettings.audioType)
            {
                Refresh();
            }
        }
        // Remove listeners
        private void OnDisable()
        {
            _button.onClick.RemoveListener(Toggle);
        }

        // Toggle cache
        private void Toggle()
        {
            // Increment to next
            int index = _fileTypes.IndexOf(_fileType);
            index++;
            if (index >= _fileTypes.Count)
            {
                index = 0;
            }

            // Apply file type
            _service.RequestSettings.audioType = _fileTypes[index];

            // Clear runtime cache
            _service.UnloadAll();

            // Refresh
            Refresh();
        }

        // Refresh location & button text
        private void Refresh()
        {
            _fileType = _service.RequestSettings.audioType;
            _label.text = string.Format(_labelFormat, _fileType.ToString());
        }
    }
}
