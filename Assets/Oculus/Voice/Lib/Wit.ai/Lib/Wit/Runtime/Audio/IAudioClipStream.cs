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
    /// A delegate for events that provide an IAudioClipStream as a parameter
    /// </summary>
    public delegate void AudioClipStreamDelegate(IAudioClipStream clipStream);

    /// <summary>
    /// An interface to be used for audio clip streams.  Samples added to this stream
    /// are always decoded to 0f - 1f.
    /// </summary>
    public interface IAudioClipStream
    {
        /// <summary>
        /// Whether or not the stream is ready for playback
        /// </summary>
        bool IsReady { get; }

        /// <summary>
        /// Whether or not the stream has been completed by receiving the total sample
        /// count and applying all samples.
        /// </summary>
        bool IsComplete { get; }

        /// <summary>
        /// A getter for the current number of channels in this audio data stream
        /// </summary>
        int Channels { get; }

        /// <summary>
        /// A getter for the current sample rate of how many samples per second should be played
        /// </summary>
        int SampleRate { get; }

        /// <summary>
        /// The total number of samples currently added to this stream
        /// </summary>
        int AddedSamples { get; }

        /// <summary>
        /// The total number of samples expected for this stream
        /// </summary>
        int TotalSamples { get; }

        /// <summary>
        /// The length of the stream in seconds.  Typically Mathf.Max(TotalSamples, AddedSamples) / (Channels * SampleRate)
        /// </summary>
        float Length { get; }

        /// <summary>
        /// A getter for the minimum length in seconds required before the stream is considered ready for playback.
        /// </summary>
        float StreamReadyLength { get; }

        /// <summary>
        /// The callback delegate for stream ready for playback.  This can be set externally but should only be called within
        /// the clip stream itself.
        /// </summary>
        AudioClipStreamDelegate OnStreamReady { set; }

        /// <summary>
        /// The callback delegate for stream update if any additional data such as the AudioClip is
        /// expected to update mid stream.  This can be set externally but should only be called within
        /// the clip stream itself.
        /// </summary>
        AudioClipStreamDelegate OnStreamUpdated { set; }

        /// <summary>
        /// The callback delegate for stream completion once SetContentLength is called & all samples
        /// have been added via the AddSamples(float[] samples) method.  This can be set externally but
        /// should only be called within the clip stream itself.
        /// </summary>
        AudioClipStreamDelegate OnStreamComplete { set; }

        /// <summary>
        /// Adds an array of samples to the current stream
        /// </summary>
        /// <param name="samples">A list of decoded floats from 0f to 1f</param>
        void AddSamples(float[] samples);

        /// <summary>
        /// Called on occasions where the total samples are known.  Either prior to a disk load or
        /// following a stream completion.
        /// </summary>
        /// <param name="totalSamples">The total samples is the final number of samples to be received</param>
        void SetTotalSamples(int totalSamples);

        /// <summary>
        /// Performs a manual refresh to determine if stream is ready or completed
        /// </summary>
        void UpdateState();

        /// <summary>
        /// Called when clip stream should be completely removed from RAM
        /// </summary>
        void Unload();
    }
}
