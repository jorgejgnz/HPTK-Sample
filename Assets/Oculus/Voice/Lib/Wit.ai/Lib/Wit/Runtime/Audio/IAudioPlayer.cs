/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Meta.Voice.Audio
{
    /// <summary>
    /// An interface for performing playback and interactions for an audio clip
    /// </summary>
    public interface IAudioPlayer
    {
        /// <summary>
        /// The currently playing clip stream
        /// </summary>
        IAudioClipStream ClipStream { get; }

        /// <summary>
        /// Whether the player is currently playing back audio
        /// </summary>
        bool IsPlaying { get; }

        /// <summary>
        /// The currently elapsed sample count
        /// </summary>
        int ElapsedSamples { get; }

        /// <summary>
        /// Called once to perform all player initialization
        /// </summary>
        void Init();

        /// <summary>
        /// A string returned to describe any reasons playback
        /// is currently unavailable
        /// </summary>
        string GetPlaybackErrors();

        /// <summary>
        /// Stops previous playback if applicable, sets clip stream
        /// and begins local playback
        /// </summary>
        /// <param name="clipStream">The clip stream to be played</param>
        /// <param name="offsetSamples">The starting offset of the clip</param>
        void Play(IAudioClipStream clipStream, int offsetSamples);

        /// <summary>
        /// Performs a pause if the current clip is playing
        /// </summary>
        void Pause();

        /// <summary>
        /// Performs a resume if the current clip is paused
        /// </summary>
        void Resume();

        /// <summary>
        /// Stops playback & removes the current clip reference
        /// </summary>
        void Stop();
    }
}
