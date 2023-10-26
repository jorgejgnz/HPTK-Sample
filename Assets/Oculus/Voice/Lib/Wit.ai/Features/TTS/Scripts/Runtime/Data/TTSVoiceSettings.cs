/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.WitAi.TTS.Data
{
    public abstract class TTSVoiceSettings
    {
        [Tooltip("A unique id used for linking these voice settings to a TTS Speaker")]
        [FormerlySerializedAs("settingsID")]
        public string SettingsId;

        [Tooltip("Text that is added to the front of any TTS request using this voice setting")]
        [TextArea]
        public string PrependedText;

        [TextArea]
        [Tooltip("Text that is added to the end of any TTS request using this voice setting")]
        public string AppendedText;
    }
}
