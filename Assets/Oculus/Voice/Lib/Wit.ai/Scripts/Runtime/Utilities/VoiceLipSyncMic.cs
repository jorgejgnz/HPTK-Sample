/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Data;
using UnityEngine;

namespace Meta.WitAi.Lib
{
    /// <summary>
    /// Applies the current Voice Mic mic output to an audio
    /// source clip for use with external lip sync scripts such
    /// as OVRAvatarLipSyncContext.
    /// </summary>
    public class VoiceLipSyncMic : MonoBehaviour
    {
        [Tooltip("Audio desired sample size for lipsync. The mic frequency will be adjusted to match this.")]
        public int AudioSampleRate = 48000;

        [Tooltip("Manual specification of Audio Source. Default will use any attached to the same object.")]
        public AudioSource AudioSource;

        // Obtain audio source & generate clip
        private void Awake()
        {
            // Setup audio source
            if (!AudioSource)
            {
                AudioSource = GetComponent<AudioSource>();
                if (!AudioSource)
                {
                    AudioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            AudioSource.loop = true;
            AudioSource.playOnAwake = false;
            if (AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }

            // Get mic from audio buffer & set sample rate
            if (AudioBuffer.Instance?.MicInput is Mic mic)
            {
                mic.AudioClipSampleRate = AudioSampleRate;
            }
            else
            {
                Debug.LogError("VoiceMicLipSync only works with Mic script.");
            }
        }

        // Enable audio buffer recording
        private void OnEnable()
        {
            // Get buffer
            AudioBuffer buffer = AudioBuffer.Instance;
            if (buffer == null)
            {
                return;
            }
            // Get mic from audio buffer & get audio clip
            if (AudioBuffer.Instance?.MicInput is Mic mic)
            {
                AudioSource.clip = mic.AudioClip;
            }
            buffer.Events.OnSampleReady += OnMicSampleReady;
            buffer.StartRecording(this);
        }

        // Begin playback if possible
        private void OnMicSampleReady(RingBuffer<byte>.Marker marker, float levelMax)
        {
            if (!AudioSource.isPlaying && AudioSource.clip != null)
            {
                AudioSource.Play();
            }
        }

        // Stop audio buffer recording
        private void OnDisable()
        {
            // Stop playback
            if (AudioSource.isPlaying)
            {
                AudioSource.Stop();
            }
            AudioSource.clip = null;

            // Breakdown buffer
            AudioBuffer buffer = AudioBuffer.Instance;
            if (buffer == null)
            {
                return;
            }
            buffer.StopRecording(this);
            buffer.Events.OnSampleReady -= OnMicSampleReady;
        }
    }
}
