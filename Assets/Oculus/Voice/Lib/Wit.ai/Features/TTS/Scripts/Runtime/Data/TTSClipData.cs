/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.Voice.Audio;
using UnityEngine;

namespace Meta.WitAi.TTS.Data
{
    // Various request load states
    public enum TTSClipLoadState
    {
        Unloaded,
        Preparing,
        Loaded,
        Error
    }

    [Serializable]
    public class TTSClipData
    {
        // Text to be spoken
        public string textToSpeak;
        // Unique identifier
        public string clipID;
        // Audio type
        public AudioType audioType;
        // Voice settings for request
        public TTSVoiceSettings voiceSettings;
        // Cache settings for request
        public TTSDiskCacheSettings diskCacheSettings;

        /// <summary>
        /// Unique request id used for tracking & logging
        /// </summary>
        public string queryRequestId => _queryRequestId;
        private string _queryRequestId = Guid.NewGuid().ToString();
        // Whether service should stream audio or just provide all at once
        public bool queryStream;
        // Request data
        public Dictionary<string, string> queryParameters;

        // Clip stream
        public IAudioClipStream clipStream
        {
            get => _clipStream;
            set
            {
                // Unload previous clip stream
                IAudioClipStream v = value;
                if (_clipStream != null && _clipStream != v)
                {
                    clipStream.OnStreamReady = null;
                    clipStream.OnStreamUpdated = null;
                    clipStream.OnStreamComplete = null;
                    _clipStream.Unload();
                }
                // Apply new clip stream
                _clipStream = v;
            }
        }
        private IAudioClipStream _clipStream;
        public AudioClip clip
        {
            get
            {
                if (clipStream is IAudioClipProvider uacs)
                {
                    return uacs.Clip;
                }
                return null;
            }
        }
        // Clip load state
        [NonSerialized] public TTSClipLoadState loadState;
        // Clip load progress
        [NonSerialized] public float loadProgress;

        // On clip state change
        public Action<TTSClipData, TTSClipLoadState> onStateChange;

        /// <summary>
        /// A callback when clip stream is ready
        /// Returns an error if there was an issue
        /// </summary>
        public Action<string> onPlaybackReady;
        /// <summary>
        /// A callback when clip has downloaded successfully
        /// Returns an error if there was an issue
        /// </summary>
        public Action<string> onDownloadComplete;

        /// <summary>
        /// Compare clips if possible
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is TTSClipData other)
            {
                return Equals(other);
            }
            return false;
        }
        /// <summary>
        /// Compare clip ids
        /// </summary>
        public bool Equals(TTSClipData other)
        {
            return HasClipId(other?.clipID);
        }
        /// <summary>
        /// Compare clip ids
        /// </summary>
        public bool HasClipId(string clipId)
        {
            return string.Equals(clipID, clipId, StringComparison.CurrentCultureIgnoreCase);
        }
        /// <summary>
        /// Get hash code
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            var hash = 17;
            hash = hash * 31 + clipID.GetHashCode();
            return hash;
        }
    }
}
