/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using UnityEngine;
using Meta.WitAi;

namespace Meta.Voice.Audio
{
    /// <summary>
    /// A class for generating and appending to audio clips while streaming
    /// </summary>
    public class UnityAudioClipStream : AudioClipStream, IAudioClipProvider
    {
        /// <summary>
        /// The audio clip to be used for Unity AudioSource playback
        /// </summary>
        public AudioClip Clip { get; private set; }

        // Whether or not the clip should be edited
        private bool _streamable;
        // The streaming chunk size
        private int _chunkSize;
        // Default chunk length in seconds when not provided
        public const float DEFAULT_CHUNK_LENGTH = 5f;

        /// <summary>
        /// Constructor with default chunk length
        /// </summary>
        /// <param name="newChannels">The audio channels/tracks for the incoming audio data</param>
        /// <param name="newSampleRate">The sample rate for incoming audio data</param>
        /// <param name="newStreamReadyLength">The minimum length in seconds required before the OnStreamReady method is called</param>
        public UnityAudioClipStream(int newChannels, int newSampleRate, float newStreamReadyLength) : base(newChannels, newSampleRate, newStreamReadyLength)
        {
            _streamable = true;
            _chunkSize = Mathf.CeilToInt(DEFAULT_CHUNK_LENGTH * (newChannels * newSampleRate));
        }

        /// <summary>
        /// Constructor with specific chunk size
        /// </summary>
        /// <param name="newChannels">The audio channels/tracks for the incoming audio data</param>
        /// <param name="newSampleRate">The sample rate for incoming audio data</param>
        /// <param name="newStreamReadyLength">The minimum length in seconds required before the OnStreamReady method is called</param>
        /// <param name="newChunkSamples">Samples to increase audio clip by</param>
        public UnityAudioClipStream(int newChannels, int newSampleRate, float newStreamReadyLength, float newChunkLength) : base(newChannels, newSampleRate, newStreamReadyLength)
        {
            _streamable = true;
            _chunkSize = Mathf.CeilToInt(Mathf.Max(newChunkLength, newStreamReadyLength) * newChannels * newSampleRate);
        }

        /// <summary>
        /// Constructor with an existing audio clip
        /// </summary>
        /// <param name="newClip">Audio clip to be used for playback</param>
        public UnityAudioClipStream(AudioClip newClip) : base(newClip == null ? 0 : newClip.channels, newClip == null ? 0 : newClip.frequency, 0f)
        {
            _streamable = false;
            AddedSamples = newClip == null ? 0 : newClip.samples;
            TotalSamples = newClip == null ? 0 : newClip.samples;
            Clip = newClip;
        }

        /// <summary>
        /// Adds an array of samples to the current stream
        /// </summary>
        /// <param name="samples">A list of decoded floats from 0f to 1f</param>
        public override void AddSamples(float[] newSamples)
        {
            // Cannot add samples to non-streamable clip
            if (!_streamable)
            {
                VLog.E(GetType().ToString(), "Cannot add samples to a non-streamable AudioClip");
                return;
            }

            // Generate initial clip
            if (Clip == null)
            {
                int newMaxSamples = Mathf.Max(_chunkSize,
                    AddedSamples + newSamples.Length);
                UpdateClip(newMaxSamples);
            }
            // Generate larger clip if needed
            else if (AddedSamples + newSamples.Length > TotalSamples)
            {
                int newMaxSamples = Mathf.Max(TotalSamples + _chunkSize,
                    AddedSamples + newSamples.Length);
                UpdateClip(newMaxSamples);
            }

            // Append to audio clip
            if (newSamples.Length > 0)
            {
                Clip.SetData(newSamples, AddedSamples);
            }

            // Increment AddedSamples & check for completion
            base.AddSamples(newSamples);
        }

        /// <summary>
        /// Calls on occassions where the total samples are known.  Either prior to a disk load or
        /// following a stream completion.
        /// </summary>
        /// <param name="totalSamples">The total samples is the final number of samples to be received</param>
        public override void SetTotalSamples(int totalSamples)
        {
            // Cannot add samples to non-streamable clip
            if (!_streamable)
            {
                VLog.E(GetType().ToString(), "Cannot set total samples of a non-streamable AudioClip");
                return;
            }

            // Set clip with specific length
            UpdateClip(totalSamples);

            // Increment TotalSamples & check for completion
            base.SetTotalSamples(totalSamples);
        }

        /// <summary>
        /// Called when clip stream should be completely removed from ram
        /// </summary>
        public override void Unload()
        {
            base.Unload();
            if (Clip != null)
            {
                Clip.DestroySafely();
                Clip = null;
            }
        }

        // Generate audio clip for a specific sample count
        private void UpdateClip(int samples)
        {
            // Cannot update a non-streamable clip
            if (!_streamable)
            {
                return;
            }
            // Already generated
            if (Clip != null && TotalSamples == samples)
            {
                return;
            }

            // Get old clip if applicable
            AudioClip oldClip = Clip;
            int oldClipSamples = TotalSamples;

            // Generate new clip
            TotalSamples = samples;
            Clip = GetCachedClip(TotalSamples, Channels, SampleRate);

            // If previous clip existed, get previous data
            if (oldClip != null)
            {
                // Apply existing data
                int copySamples = Mathf.Min(oldClipSamples, samples);
                float[] oldSamples = new float[copySamples];
                oldClip.GetData(oldSamples, 0);
                Clip.SetData(oldSamples, 0);

                // Invoke clip updated callback
                VLog.D($"Clip Stream - Clip Updated\nNew Samples: {samples}\nOld Samples: {oldClipSamples}");

                // Requeue previous clip
                ReuseCachedClip(oldClip);
            }
            else
            {
                VLog.D($"Clip Stream - Clip Generated\nSamples: {samples}");
            }

            // Handle update
            HandleStreamUpdated();
        }

        #region CACHING
        // Total clips generated including unloaded
        private static int ClipsGenerated = 0;
        // List of preloaded audio clips
        private static List<AudioClip> Clips = new List<AudioClip>();

        /// <summary>
        /// Method used to preload clips to improve performance at runtime
        /// </summary>
        /// <param name="total">Total clips to preload.  This should be the number of clips that could be running at once</param>
        public static void PreloadCachedClips(int total, int lengthSamples, int channels, int frequency)
        {
            for (int i = 0; i < total; i++)
            {
                GenerateCacheClip(lengthSamples, channels, frequency);
            }
        }
        // Preload a single clip
        private static void GenerateCacheClip(int lengthSamples, int channels, int frequency)
        {
            ClipsGenerated++;
            AudioClip clip = AudioClip.Create($"AudioClip_{ClipsGenerated:000}", lengthSamples, channels, frequency, false);
            Clips.Add(clip);
        }
        // Preload a single clip
        private static AudioClip GetCachedClip(int lengthSamples, int channels, int frequency)
        {
            // Find a matching clip
            int clipIndex = Clips.FindIndex((clip) => DoesClipMatch(clip, lengthSamples, channels, frequency));

            // Generate a clip with the specified size
            if (clipIndex == -1)
            {
                clipIndex = Clips.Count;
                GenerateCacheClip(lengthSamples, channels, frequency);
            }

            // Get clip, remove from preload list & return
            AudioClip result = Clips[clipIndex];
            Clips.RemoveAt(clipIndex);
            return result;
        }
        // Check if clip matches
        private static bool DoesClipMatch(AudioClip clip, int lengthSamples, int channels, int frequency)
        {
            return clip.samples == lengthSamples && clip.channels == channels && clip.frequency == frequency;
        }
        // Reuse clip
        private static void ReuseCachedClip(AudioClip clip)
        {
            Clips.Add(clip);
        }
        /// <summary>
        /// Destroy all cached clips
        /// </summary>
        public static void DestroyCachedClips()
        {
            foreach (var clip in Clips)
            {
                clip.DestroySafely();
            }
            Clips.Clear();
        }
        #endregion
    }
}
