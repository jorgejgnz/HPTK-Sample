/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.TTS.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.WitAi.TTS.Events
{
    /// <summary>
    /// Events related to web requests
    /// </summary>
    [Serializable]
    public class TTSWebRequestEvents
    {
        [Tooltip("Called when a web request begins transmission")]
        public TTSClipEvent OnRequestBegin = new TTSClipEvent();

        [Tooltip("Called when a web request is cancelled")]
        public TTSClipEvent OnRequestCancel = new TTSClipEvent();

        [Tooltip("Called when a web request fails")]
        public TTSClipErrorEvent OnRequestError = new TTSClipErrorEvent();

        [Tooltip("Called when a web request receives first data")]
        public TTSClipEvent OnRequestFirstResponse = new TTSClipEvent();

        [Tooltip("Called when a web request is ready for playback")]
        public TTSClipEvent OnRequestReady = new TTSClipEvent();

        [Tooltip("Called when a web request is completed via success, cancellation or failure")]
        public TTSClipEvent OnRequestComplete = new TTSClipEvent();
    }
}
