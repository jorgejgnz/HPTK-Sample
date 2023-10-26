/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using Meta.WitAi.TTS.Data;
using Meta.WitAi.TTS.Utilities;

namespace Meta.Voice.Samples.TTSVoices
{
    /// <summary>
    /// A demo script that provides access to a TTSSpeaker
    /// and overrides all TTSSpeaker callback events.
    /// </summary>
    public class TTSSpeakerObserver : MonoBehaviour
    {
        [Header("Speaker Settings")]
        [SerializeField] [Tooltip("TTSSpeaker being observed, if left empty it will grab the speaker from the GameObject")]
        private TTSSpeaker _speaker;
        public TTSSpeaker Speaker => _speaker;

        /// <summary>
        /// Obtains speaker if not set
        /// </summary>
        protected virtual void Awake()
        {
            if (_speaker == null)
            {
                _speaker = gameObject.GetComponentInChildren<TTSSpeaker>();
            }
        }

        /// <summary>
        /// On enable, add all callbacks
        /// </summary>
        protected virtual void OnEnable()
        {
            if (_speaker == null)
            {
                return;
            }
            _speaker.Events.OnPlaybackQueueBegin.AddListener(OnPlaybackQueueBegin);
            _speaker.Events.OnPlaybackQueueComplete.AddListener(OnPlaybackQueueComplete);
            _speaker.Events.OnLoadBegin.AddListener(OnLoadBegin);
            _speaker.Events.OnLoadAbort.AddListener(OnLoadAbort);
            _speaker.Events.OnLoadFailed.AddListener(OnLoadFailed);
            _speaker.Events.OnLoadSuccess.AddListener(OnLoadSuccess);
            _speaker.Events.OnPlaybackReady.AddListener(OnPlaybackReady);
            _speaker.Events.OnPlaybackStart.AddListener(OnPlaybackStart);
            _speaker.Events.OnPlaybackCancelled.AddListener(OnPlaybackCancelled);
            _speaker.Events.OnPlaybackComplete.AddListener(OnPlaybackComplete);
        }

        /// <summary>
        /// On disable, remove all callbacks
        /// </summary>
        protected virtual void OnDisable()
        {
            if (_speaker == null)
            {
                return;
            }
            _speaker.Events.OnPlaybackQueueBegin.RemoveListener(OnPlaybackQueueBegin);
            _speaker.Events.OnPlaybackQueueComplete.RemoveListener(OnPlaybackQueueComplete);
            _speaker.Events.OnLoadBegin.RemoveListener(OnLoadBegin);
            _speaker.Events.OnLoadAbort.RemoveListener(OnLoadAbort);
            _speaker.Events.OnLoadFailed.RemoveListener(OnLoadFailed);
            _speaker.Events.OnLoadSuccess.RemoveListener(OnLoadSuccess);
            _speaker.Events.OnPlaybackReady.RemoveListener(OnPlaybackReady);
            _speaker.Events.OnPlaybackStart.RemoveListener(OnPlaybackStart);
            _speaker.Events.OnPlaybackCancelled.RemoveListener(OnPlaybackCancelled);
            _speaker.Events.OnPlaybackComplete.RemoveListener(OnPlaybackComplete);
        }

        /// <summary>
        /// Callback for queue begin
        /// </summary>
        protected virtual void OnPlaybackQueueBegin()
        {

        }
        /// <summary>
        /// Callback for queue completion
        /// </summary>
        protected virtual void OnPlaybackQueueComplete()
        {

        }

        /// <summary>
        /// Callback for load begin
        /// </summary>
        protected virtual void OnLoadBegin(TTSSpeaker speaker, TTSClipData clipData)
        {

        }
        /// <summary>
        /// Callback for load cancelation
        /// </summary>
        protected virtual void OnLoadAbort(TTSSpeaker speaker, TTSClipData clipData)
        {

        }
        /// <summary>
        /// Callback for load error
        /// </summary>
        protected virtual void OnLoadFailed(TTSSpeaker speaker, TTSClipData clipData, string error)
        {

        }
        /// <summary>
        /// Callback for load success
        /// </summary>
        protected virtual void OnLoadSuccess(TTSSpeaker speaker, TTSClipData clipData)
        {

        }

        /// <summary>
        /// Callback for playback is ready for a clip
        /// </summary>
        protected virtual void OnPlaybackReady(TTSSpeaker speaker, TTSClipData clipData)
        {

        }
        /// <summary>
        /// Callback for playback for a clip has begun
        /// </summary>
        protected virtual void OnPlaybackStart(TTSSpeaker speaker, TTSClipData clipData)
        {

        }
        /// <summary>
        /// Callback for playback for a clip has been canceled
        /// </summary>
        protected virtual void OnPlaybackCancelled(TTSSpeaker speaker, TTSClipData clipData, string reason)
        {

        }
        /// <summary>
        /// Callback for playback completion
        /// </summary>
        protected virtual void OnPlaybackComplete(TTSSpeaker speaker, TTSClipData clipData)
        {

        }
    }
}
