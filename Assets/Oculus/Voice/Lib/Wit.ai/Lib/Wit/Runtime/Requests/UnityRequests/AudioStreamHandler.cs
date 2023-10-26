/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Meta.Voice.Audio;
using UnityEngine.Scripting;

namespace Meta.WitAi.Requests
{
    /// <summary>
    /// The various supported audio decode options
    /// </summary>
    public enum AudioStreamDecodeType
    {
        PCM16,
        MP3,
        WAV
    }

    /// <summary>
    /// A download handler for UnityWebRequest that decodes audio data, passes
    /// the data into an iAudioClipStream & provides download state information.
    /// </summary>
    [Preserve]
    public class AudioStreamHandler : DownloadHandlerScript, IVRequestStreamable
    {
        /// <summary>
        /// Clip used to cache audio data
        /// </summary>
        public IAudioClipStream ClipStream { get; private set; }

        /// <summary>
        /// The audio stream decode option
        /// </summary>
        public AudioStreamDecodeType DecodeType { get; private set; }

        /// <summary>
        /// Audio stream data is ready to be played
        /// </summary>
        public bool IsStreamReady { get; private set; }

        /// <summary>
        /// Audio stream data has completed reception
        /// </summary>
        public bool IsStreamComplete { get; private set; }


        // Leftover byte
        private bool _hasLeftover = false;
        private byte[] _leftovers = new byte[2];
        // Current samples received
        private int _decodingChunks = 0;
        private bool _requestComplete = false;
        // Error handling
        private int _errorDecoded;
        private byte[] _errorBytes;

        // Generate
        public AudioStreamHandler(IAudioClipStream newClipStream, AudioType newDecodeType)
        {
            // Apply parameters
            ClipStream = newClipStream;
            DecodeType = GetDecodeType(newDecodeType);

            // Setup data
            _hasLeftover = false;
            _decodingChunks = 0;
            _requestComplete = false;
            IsStreamReady = false;
            IsStreamComplete = false;
            _errorBytes = null;
            _errorDecoded = 0;

            // Begin stream
            VLog.D($"Clip Stream - Began\nClip Stream: {ClipStream.GetType()}\nFile Type: {DecodeType}");
        }

        // If size is provided, generate clip using size
        [Preserve]
        protected override void ReceiveContentLengthHeader(ulong contentLength)
        {
            // Ignore if already complete
            if (contentLength == 0 || IsStreamComplete)
            {
                return;
            }

            // Assume text if less than min chunk size
            int minChunkSize = Mathf.Max(100, Mathf.CeilToInt(0.1f * ClipStream.Channels * ClipStream.SampleRate));
            if (contentLength < (ulong)minChunkSize)
            {
                _errorBytes = new byte[minChunkSize];
                return;
            }

            // Apply size
            int newSamples = GetClipSamplesFromContentLength(contentLength, DecodeType);
            VLog.D($"Clip Stream - Received Size\nTotal Samples: {newSamples}");
            ClipStream.SetTotalSamples(newSamples);
        }

        // Receive data
        [Preserve]
        protected override bool ReceiveData(byte[] receiveData, int dataLength)
        {
            // Exit if desired
            if (!base.ReceiveData(receiveData, dataLength) || IsStreamComplete)
            {
                return false;
            }

            // Append to error
            if (_errorBytes != null)
            {
                for (int i = 0; i < Mathf.Min(dataLength, _errorBytes.Length - _errorDecoded); i++)
                {
                    _errorBytes[_errorDecoded + i] = receiveData[i];
                }
                _errorDecoded += dataLength;
                return true;
            }

            // Decode data async
            _decodingChunks++;
            ThreadUtility.PerformInBackground(() => DecodeData(receiveData, dataLength), OnDecodeComplete);

            // Return data
            return true;
        }
        // Decode data
        private float[] DecodeData(byte[] receiveData, int dataLength)
        {
            // Next decoded samples
            float[] newSamples = null;

            // Decode PCM chunk
            if (DecodeType == AudioStreamDecodeType.PCM16)
            {
                newSamples = DecodeChunkPCM16(receiveData, dataLength, ref _hasLeftover, ref _leftovers);
            }
            // TODO: Decode MP3 chunk
            else if (DecodeType == AudioStreamDecodeType.MP3)
            {

            }
            // TODO: Decode WAV chunk
            else if (DecodeType == AudioStreamDecodeType.WAV)
            {

            }

            // Failed
            return newSamples;
        }
        // Decode complete
        private void OnDecodeComplete(float[] newSamples, string error)
        {
            // Complete
            _decodingChunks--;

            // Fail with error
            if (!string.IsNullOrEmpty(error))
            {
                VLog.W($"Decode Chunk Failed\n{error}");
                TryToFinalize();
                return;
            }
            // Fail without samples
            if (newSamples == null)
            {
                VLog.W($"Decode Chunk Failed\nNo samples returned");
                TryToFinalize();
                return;
            }

            // Add to clip
            if (newSamples.Length > 0)
            {
                ClipStream.AddSamples(newSamples);
                VLog.D($"Clip Stream - Decoded {newSamples.Length} Samples");
            }

            // Stream is now ready
            if (!IsStreamReady && ClipStream.IsReady)
            {
                IsStreamReady = true;
                VLog.D($"Clip Stream - Stream Ready");
            }

            // Try to finalize
            TryToFinalize();
        }

        // Used for error handling
        [Preserve]
        protected override string GetText()
        {
            return _errorBytes != null ? Encoding.UTF8.GetString(_errorBytes) : string.Empty;
        }

        // Return progress if total samples has been determined
        [Preserve]
        protected override float GetProgress()
        {
            if (_errorBytes != null && _errorBytes.Length > 0)
            {
                return (float) _errorDecoded / _errorBytes.Length;
            }
            if (ClipStream.TotalSamples > 0)
            {
                return (float) ClipStream.AddedSamples / ClipStream.TotalSamples;
            }
            return 0f;
        }

        // Clean up clip with final sample count
        [Preserve]
        protected override void CompleteContent()
        {
            // Ignore if called multiple times
            if (_requestComplete)
            {
                return;
            }

            // Complete
            _requestComplete = true;
            TryToFinalize();
        }

        // Handle completion
        private void TryToFinalize()
        {
            // Already finalized or not yet complete
            if (IsStreamComplete || !_requestComplete || _decodingChunks > 0 || ClipStream == null)
            {
                return;
            }

            // Wait a single frame prior to final completion to ensure OnReady is called first
            if (!IsStreamReady)
            {
                IsStreamReady = true;
                VLog.D($"Clip Stream - Stream Ready");
                CoroutineUtility.StartCoroutine(FinalWait());
                return;
            }

            // Stream complete
            IsStreamComplete = true;
            ClipStream.SetTotalSamples(ClipStream.AddedSamples);
            VLog.D($"Clip Stream - Complete\nLength: {ClipStream.Length:0.00} secs");

            // Dispose
            Dispose();
        }

        // A final wait callback that ensures onready is called first for non-streaming instances
        private IEnumerator FinalWait()
        {
            yield return null;
            TryToFinalize();
        }

        // Destroy old clip
        public void CleanUp()
        {
            // Already complete
            if (IsStreamComplete)
            {
                _leftovers = null;
                _errorBytes = null;
                ClipStream = null;
                return;
            }

            // Destroy clip
            if (ClipStream != null)
            {
                ClipStream.Unload();
                ClipStream = null;
            }

            // Dispose handler
            Dispose();

            // Complete
            IsStreamComplete = true;
            VLog.D($"Clip Stream - Cleaned Up");
        }

        #region STATIC
        /// <summary>
        /// Determine decode type based on audio type
        /// </summary>
        public static AudioStreamDecodeType GetDecodeType(AudioType audioType)
        {
            switch (audioType)
            {
                case AudioType.WAV:
                    return AudioStreamDecodeType.WAV;
                case AudioType.MPEG:
                    return AudioStreamDecodeType.MP3;
            }
            return AudioStreamDecodeType.PCM16;
        }
        /// <summary>
        /// Currently can only decode pcm
        /// </summary>
        public static bool CanDecodeType(AudioType audioType)
        {
            switch (GetDecodeType(audioType))
            {
                case AudioStreamDecodeType.PCM16:
                    return true;
            }
            return false;
        }
        // Decode raw pcm data
        public static AudioClip GetClipFromRawData(byte[] rawData, AudioStreamDecodeType decodeType, string clipName, int channels, int sampleRate)
        {
            // Decode data
            float[] samples = DecodeAudio(rawData, decodeType);
            if (samples == null)
            {
                return null;
            }
            // Generate clip
            return GetClipFromSamples(samples, clipName, channels, sampleRate);
        }
        // Decode raw pcm data
        public static void GetClipFromRawDataAsync(byte[] rawData, AudioStreamDecodeType decodeType, string clipName, int channels, int sampleRate, Action<AudioClip, string> onComplete)
        {
            // Perform in background
            ThreadUtility.PerformInBackground(() => DecodeAudio(rawData, decodeType), (samples, error) =>
            {
                if (!string.IsNullOrEmpty(error))
                {
                    error = $"Audio decode async failed\n{error}";
                    VLog.E(error);
                    onComplete?.Invoke(null, error);
                }
                else if (rawData == null)
                {
                    error = "Audio decode async results missing";
                    VLog.E(error);
                    onComplete?.Invoke(null, error);
                }
                else
                {
                    AudioClip result = GetClipFromSamples(samples, clipName, channels, sampleRate);
                    onComplete?.Invoke(result, error);
                }
            });
        }
        // Decode raw pcm data
        public static float[] DecodeAudio(byte[] rawData, AudioStreamDecodeType decodeType)
        {
            // Samples to be decoded
            float[] samples = null;

            // Decode raw data
            if (decodeType == AudioStreamDecodeType.PCM16)
            {
                samples = DecodePCM16(rawData);
            }
            // Not supported
            else
            {
                VLog.E($"Not Supported Decode File Type\nType: {decodeType}");
            }

            // Return samples
            return samples;
        }
        // Get audio clip from samples
        private static AudioClip GetClipFromSamples(float[] samples, string clipName, int channels, int sampleRate)
        {
            AudioClip result = AudioClip.Create(clipName, samples.Length, channels, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }
        // Determines clip sample count via content length dependent on file type
        public static int GetClipSamplesFromContentLength(ulong contentLength, AudioStreamDecodeType decodeType)
        {
            switch (decodeType)
            {
                    case AudioStreamDecodeType.PCM16:
                        return Mathf.FloorToInt(contentLength / 2f);
            }
            return 0;
        }
        #endregion

        #region PCM DECODE
        // Decode an entire array
        public static float[] DecodePCM16(byte[] rawData)
        {
            float[] samples = new float[Mathf.FloorToInt(rawData.Length / 2f)];
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] = DecodeSamplePCM16(rawData, i * 2);
            }
            return samples;
        }
        // Decode a single chunk
        private static float[] DecodeChunkPCM16(byte[] chunkData, int chunkLength, ref bool hasLeftover, ref byte[] leftovers)
        {
            // Determine if previous chunk had a leftover or if newest chunk contains one
            bool prevLeftover = hasLeftover;
            bool nextLeftover = (chunkLength - (prevLeftover ? 1 : 0)) % 2 != 0;
            hasLeftover = nextLeftover;

            // Generate sample array
            int startOffset = prevLeftover ? 1 : 0;
            int endOffset = nextLeftover ? 1 : 0;
            int newSampleCount = (chunkLength + startOffset - endOffset) / 2;
            float[] newSamples = new float[newSampleCount];

            // Append first byte to previous array
            if (prevLeftover)
            {
                // Append first byte to leftover array
                leftovers[1] = chunkData[0];
                // Decode first sample
                newSamples[0] = DecodeSamplePCM16(leftovers, 0);
            }

            // Store last byte
            if (nextLeftover)
            {
                leftovers[0] = chunkData[chunkLength - 1];
            }

            // Decode remaining samples
            for (int i = 0; i < newSamples.Length - startOffset; i++)
            {
                newSamples[startOffset + i] = DecodeSamplePCM16(chunkData, startOffset + i * 2);
            }

            // Return samples
            return newSamples;
        }
        // Decode a single sample
        private static float DecodeSamplePCM16(byte[] rawData, int index)
        {
            return (float)BitConverter.ToInt16(rawData, index) / (float)Int16.MaxValue;
        }
        #endregion

        #region MPEG DECODE

        #endregion

        #region WAV DECODE

        #endregion
    }
}
