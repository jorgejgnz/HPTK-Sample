/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using TMPro;

namespace Meta.Voice.Samples.Dictation
{
    public class SimpleLabelResizer : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _label;
        private string _text;

        private void Reset()
        {
            _label = gameObject.GetComponent<TextMeshProUGUI>();
        }

        private void Awake()
        {
            if (_label == null)
            {
                _label = gameObject.GetComponent<TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            if (!string.Equals(_text, _label.text))
            {
                RefreshSize();
            }
        }

        public void RefreshSize()
        {
            _text = _label.text;
            float preferredHeight = _label.preferredHeight;
            _label.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferredHeight);
        }
    }
}
