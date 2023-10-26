/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using UnityEngine;
using Meta.WitAi;

namespace Meta.Voice.Audio
{
    /// <summary>
    /// Unity specific audio player that will play any IAudioClipStream that includes an IAudioClipProvider
    /// </summary>
    [Serializable]
    public class UnityAudioPlayer : AudioPlayer, IAudioSourceProvider
    {
        /// <summary>
        /// Audio source to be used for text-to-speech playback
        /// </summary>
        [Header("Playback Settings")]
        [Tooltip("Audio source to be used for text-to-speech playback")]
        [SerializeField] private AudioSource _audioSource;
        public AudioSource AudioSource => _audioSource;

        /// <summary>
        /// Duplicates audio source reference on awake instead of using it directly.
        /// </summary>
        [Tooltip("Duplicates audio source reference on awake instead of using it directly.")]
        [SerializeField] private bool _cloneAudioSource = false;
        public bool CloneAudioSource => _cloneAudioSource;

        /// <summary>
        /// Performs all player initialization
        /// </summary>
        public override void Init()
        {
            // Find base audio source if possible
            if (AudioSource == null)
            {
                _audioSource = gameObject.GetComponentInChildren<AudioSource>();
            }

            // Duplicate audio source
            if (CloneAudioSource)
            {
                // Create new audio source
                AudioSource instance = new GameObject($"{gameObject.name}_AudioOneShot").AddComponent<AudioSource>();
                instance.PreloadCopyData();

                // Move into this transform & default to 3D audio
                if (AudioSource == null)
                {
                    instance.transform.SetParent(transform, false);
                    instance.spread = 1f;
                }

                // Move into audio source & copy source values
                else
                {
                    instance.transform.SetParent(AudioSource.transform, false);
                    instance.Copy(AudioSource);
                }

                // Reset instance's transform
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;

                // Apply
                _audioSource = instance;
            }

            // Setup audio source settings
            AudioSource.playOnAwake = false;
        }

        /// <summary>
        /// A string returned to describe any reasons playback
        /// is currently unavailable
        /// </summary>
        public override string GetPlaybackErrors()
        {
            if (AudioSource == null)
            {
                return "Audio source is missing";
            }
            return string.Empty;
        }

        /// <summary>
        /// Sets audio clip & begins playback at a specified offset
        /// </summary>
        /// <param name="offsetSamples">The starting offset of the clip</param>
        protected override void Play(int offsetSamples = 0)
        {
            // Play audio
            if (ClipStream is IAudioClipProvider uacs)
            {
                AudioSource.loop = false;
                AudioSource.clip = uacs.Clip;
                AudioSource.timeSamples = offsetSamples;
                AudioSource.Play();
            }
            // Null stream
            else if (ClipStream == null)
            {
                VLog.E($"{GetType()} cannot play null clip stream");
            }
            // Log error
            else
            {
                VLog.E($"{GetType()} cannot play {ClipStream.GetType()} clips");
            }
        }

        /// <summary>
        /// Whether the player is currently playing back audio
        /// </summary>
        public override bool IsPlaying => AudioSource != null && AudioSource.isPlaying;

        /// <summary>
        /// The currently elapsed sample count
        /// </summary>
        public override int ElapsedSamples => AudioSource != null ? AudioSource.timeSamples : 0;

        /// <summary>
        /// Performs a pause if the current clip is playing
        /// </summary>
        public override void Pause()
        {
            if (IsPlaying)
            {
                AudioSource.Pause();
            }
        }

        /// <summary>
        /// Performs a resume if the current clip is paused
        /// </summary>
        public override void Resume()
        {
            if (!IsPlaying)
            {
                AudioSource.UnPause();
            }
        }

        /// <summary>
        /// Stops playback & removes the current clip reference
        /// </summary>
        public override void Stop()
        {
            if (IsPlaying)
            {
                AudioSource.Stop();
            }
            AudioSource.clip = null;
            base.Stop();
        }
    }
}
