/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;

namespace Meta.Voice.Audio
{
    /// <summary>
    /// An interface for an audio system that can be used to return custom audio
    /// clip streams and audio players on specific gameObjects
    /// </summary>
    public interface IAudioSystem
    {
        /// <summary>
        /// Returns a new audio clip stream for audio stream handling
        /// </summary>
        /// <param name="channels">Number of channels within audio</param>
        /// <param name="sampleRate">Desired rate of playback</param>
        IAudioClipStream GetAudioClipStream(int channels, int sampleRate);

        /// <summary>
        /// Returns a new audio player for managing audio clip stream playback states
        /// </summary>
        /// <param name="root">The gameobject to add the player to if applicable</param>
        IAudioPlayer GetAudioPlayer(GameObject root);
    }
}
