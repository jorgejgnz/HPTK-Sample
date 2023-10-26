/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using Meta.WitAi;

namespace Meta.Voice.Audio
{
    /// <summary>
    /// An audio system that provides UnityAudioClipStreams & UnityAudioPlayers for playback using Unity's built-in audio system
    /// </summary>
    public class UnityAudioSystem : MonoBehaviour, IAudioSystem
    {
        /// <summary>
        /// Chunk buffer length in seconds
        /// </summary>
        public float ChunkBufferLength = UnityAudioClipStream.DEFAULT_CHUNK_LENGTH;

        /// <summary>
        /// Audio clip ready length in seconds
        /// </summary>
        public float AudioClipReadyLength = 1f;

        /// <summary>
        /// Chunk buffer length in seconds
        /// </summary>
        public int AudioClipPreloadCount = 3;

        // Preload clips if desired
        private void Awake()
        {
            if (AudioClipPreloadCount <= 0)
            {
                return;
            }

            // Total samples to preload
            int totalSamples = Mathf.CeilToInt(ChunkBufferLength *
                                               WitConstants.ENDPOINT_TTS_CHANNELS *
                                               WitConstants.ENDPOINT_TTS_SAMPLE_RATE);

            // Preload specified amount of clips
            UnityAudioClipStream.PreloadCachedClips(AudioClipPreloadCount, totalSamples, WitConstants.ENDPOINT_TTS_CHANNELS, WitConstants.ENDPOINT_TTS_SAMPLE_RATE);
        }

        // Destroy all cached clips
        private void OnDestroy()
        {
            if (AudioClipPreloadCount <= 0)
            {
                return;
            }
            UnityAudioClipStream.DestroyCachedClips();
        }

        /// <summary>
        /// Returns a new audio clip stream for audio stream handling
        /// </summary>
        /// <param name="channels">Number of channels within audio</param>
        /// <param name="sampleRate">Desired rate of playback</param>
        public IAudioClipStream GetAudioClipStream(int channels, int sampleRate) =>
            new UnityAudioClipStream(channels, sampleRate, AudioClipReadyLength, ChunkBufferLength);

        /// <summary>
        /// Returns a new audio player for managing audio clip stream playback states
        /// </summary>
        /// <param name="root">The gameobject to add the player to if applicable</param>
        public IAudioPlayer GetAudioPlayer(GameObject root) => root.AddComponent<UnityAudioPlayer>();
    }
}
