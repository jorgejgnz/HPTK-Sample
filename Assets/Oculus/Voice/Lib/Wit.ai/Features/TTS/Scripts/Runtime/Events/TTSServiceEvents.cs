/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;

namespace Meta.WitAi.TTS.Events
{
    [Serializable]
    public class TTSServiceEvents
    {
        [Tooltip("Called when a audio clip has been added to the runtime cache")]
        public TTSClipEvent OnClipCreated  = new TTSClipEvent();

        [Tooltip("Called when a audio clip has been removed from the runtime cache")]
        public TTSClipEvent OnClipUnloaded  = new TTSClipEvent();

        /// <summary>
        /// All events related to web requests
        /// </summary>
        public TTSWebRequestEvents WebRequest = new TTSWebRequestEvents();

        /// <summary>
        /// All events related to streaming from web or disk
        /// </summary>
        public TTSStreamEvents Stream = new TTSStreamEvents();

        /// <summary>
        /// All events related to downloading from the web
        /// </summary>
        public TTSDownloadEvents Download = new TTSDownloadEvents();
    }
}
