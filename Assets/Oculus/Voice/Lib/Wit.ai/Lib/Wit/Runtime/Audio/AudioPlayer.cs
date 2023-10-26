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
    /// Custom MonoBehaviour audio player that handles some custom method handling
    /// </summary>
    public abstract class AudioPlayer : MonoBehaviour, IAudioPlayer
    {
        /// <summary>
        /// The currently playing clip stream
        /// </summary>
        public IAudioClipStream ClipStream { get; private set; }

        /// <summary>
        /// Whether the player is currently playing back audio
        /// </summary>
        public virtual bool IsPlaying => ClipStream != null;

        /// <summary>
        /// The currently elapsed sample count
        /// </summary>
        public abstract int ElapsedSamples { get; }

        /// <summary>
        /// Performs all player initialization
        /// </summary>
        public abstract void Init();

        /// <summary>
        /// A string returned to describe any reasons playback
        /// is currently unavailable
        /// </summary>
        public abstract string GetPlaybackErrors();

        /// <summary>
        /// Stops previous playback if applicable, sets clip stream
        /// and begins local playback
        /// </summary>
        /// <param name="clipStream">The clip stream to be played</param>
        /// <param name="offsetSamples">The starting offset of the clip</param>
        public void Play(IAudioClipStream clipStream, int offsetSamples)
        {
            // Stop previous playback
            Stop();

            // Apply new clip stream
            ClipStream = clipStream;

            // Begin playback
            Play(offsetSamples);
        }

        /// <summary>
        /// Performs playback starting with a specific sample
        /// </summary>
        protected abstract void Play(int offsetSamples);

        /// <summary>
        /// Performs a pause if the current clip is playing
        /// </summary>
        public abstract void Pause();

        /// <summary>
        /// Performs a resume if the current clip is paused
        /// </summary>
        public abstract void Resume();

        /// <summary>
        /// Stops playback & removes the current clip reference
        /// </summary>
        public virtual void Stop()
        {
            ClipStream = null;
        }
    }
}
