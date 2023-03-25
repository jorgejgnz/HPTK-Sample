/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace Meta.WitAi
{
    public class WitService : MonoBehaviour, IWitRuntimeConfigProvider, IVoiceEventProvider
    {
        private WitRequestOptions _currentRequestOptions;
        private float _lastMinVolumeLevelTime;
        private WitRequest _recordingRequest;

        private bool _isSoundWakeActive;
        private RingBuffer<byte>.Marker _lastSampleMarker;
        private bool _minKeepAliveWasHit;
        private bool _isActive;
        private long _minSampleByteCount = 1024 * 10;

        private IVoiceEventProvider _voiceEventProvider;
        private IWitRuntimeConfigProvider _runtimeConfigProvider;
        private ITranscriptionProvider _activeTranscriptionProvider;
        private Coroutine _timeLimitCoroutine;
        private IWitRequestProvider _witRequestProvider;

        // Transcription based endpointing
        private bool _receivedTranscription;
        private float _lastWordTime;

        // Parallel Requests
        private HashSet<WitRequest> _transmitRequests = new HashSet<WitRequest>();
        private HashSet<WitRequest> _queuedRequests = new HashSet<WitRequest>();
        private Coroutine _queueHandler;

        #region Interfaces
        private IWitByteDataReadyHandler[] _dataReadyHandlers;
        private IWitByteDataSentHandler[] _dataSentHandlers;
        private IDynamicEntitiesProvider[] _dynamicEntityProviders;
        private float _time;

        #endregion

#if DEBUG_SAMPLE
        private FileStream sampleFile;
#endif

        /// <summary>
        /// Returns true if wit is currently active and listening with the mic
        /// </summary>
        public bool Active => _isActive || IsRequestActive;

        public bool IsRequestActive => null != _recordingRequest && _recordingRequest.IsActive;

        public IVoiceEventProvider VoiceEventProvider
        {
            get => _voiceEventProvider;
            set => _voiceEventProvider = value;
        }

        public IWitRuntimeConfigProvider ConfigurationProvider
        {
            get => _runtimeConfigProvider;
            set => _runtimeConfigProvider = value;
        }

        public WitRuntimeConfiguration RuntimeConfiguration =>
            _runtimeConfigProvider.RuntimeConfiguration;

        public VoiceEvents VoiceEvents => _voiceEventProvider.VoiceEvents;

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public ITranscriptionProvider TranscriptionProvider
        {
            get => _activeTranscriptionProvider;
            set
            {
                if (null != _activeTranscriptionProvider)
                {
                    _activeTranscriptionProvider.OnFullTranscription.RemoveListener(
                        OnFullTranscription);
                    _activeTranscriptionProvider.OnPartialTranscription.RemoveListener(
                        OnPartialTranscription);
                    _activeTranscriptionProvider.OnMicLevelChanged.RemoveListener(
                        OnTranscriptionMicLevelChanged);
                    _activeTranscriptionProvider.OnStartListening.RemoveListener(
                        OnMicStartListening);
                    _activeTranscriptionProvider.OnStoppedListening.RemoveListener(
                        OnMicStoppedListening);
                }

                _activeTranscriptionProvider = value;

                if (null != _activeTranscriptionProvider)
                {
                    _activeTranscriptionProvider.OnFullTranscription.AddListener(
                        OnFullTranscription);
                    _activeTranscriptionProvider.OnPartialTranscription.AddListener(
                        OnPartialTranscription);
                    _activeTranscriptionProvider.OnMicLevelChanged.AddListener(
                        OnTranscriptionMicLevelChanged);
                    _activeTranscriptionProvider.OnStartListening.AddListener(
                        OnMicStartListening);
                    _activeTranscriptionProvider.OnStoppedListening.AddListener(
                        OnMicStoppedListening);
                }
            }
        }

        public IWitRequestProvider WitRequestProvider
        {
            get => _witRequestProvider;
            set => _witRequestProvider = value;
        }

        public bool MicActive => AudioBuffer.Instance.IsRecording(this);

        protected bool ShouldSendMicData => RuntimeConfiguration.sendAudioToWit ||
                                                  null == _activeTranscriptionProvider;

        #region LIFECYCLE
        // Find transcription provider & Mic
        protected void Awake()
        {
            _dataReadyHandlers = GetComponents<IWitByteDataReadyHandler>();
            _dataSentHandlers = GetComponents<IWitByteDataSentHandler>();
        }
        // Add mic delegates
        protected void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            _runtimeConfigProvider = GetComponent<IWitRuntimeConfigProvider>();
            _voiceEventProvider = GetComponent<IVoiceEventProvider>();

            if (null == _activeTranscriptionProvider && null != RuntimeConfiguration &&
                RuntimeConfiguration.customTranscriptionProvider)
            {
                TranscriptionProvider = RuntimeConfiguration.customTranscriptionProvider;
            }

            SetMicDelegates(true);

            _dynamicEntityProviders = GetComponents<IDynamicEntitiesProvider>();
        }
        // Remove mic delegates
        protected void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            AudioBufferEvents e = AudioBuffer.Instance?.Events;
            SetMicDelegates(false);
        }
        // On scene refresh
        protected virtual void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SetMicDelegates(true);
        }
        // Toggle audio events
        private AudioBuffer _buffer;
        private bool _bufferDelegates = false;
        protected void SetMicDelegates(bool add)
        {
            // Obtain buffer
            if (_buffer == null)
            {
                _buffer = AudioBuffer.Instance;
                _bufferDelegates = false;
            }
            // Get events if possible
            AudioBufferEvents e = _buffer?.Events;
            if (e == null)
            {
                return;
            }
            // Already set
            if (_bufferDelegates == add)
            {
                return;
            }
            // Set delegates
            _bufferDelegates = add;

            // Add delegates
            if (add)
            {
                e.OnMicLevelChanged.AddListener(OnMicLevelChanged);
                e.OnByteDataReady.AddListener(OnByteDataReady);
                e.OnSampleReady += OnMicSampleReady;
            }
            // Remove delegates
            else
            {
                e.OnMicLevelChanged.RemoveListener(OnMicLevelChanged);
                e.OnByteDataReady.RemoveListener(OnByteDataReady);
                e.OnSampleReady -= OnMicSampleReady;
            }
        }
        #endregion

        #region ACTIVATION
        /// <summary>
        /// Activate the microphone and send data to Wit for NLU processing.
        /// </summary>
        public void Activate() => Activate(new WitRequestOptions());
        public void Activate(WitRequestOptions requestOptions)
        {
            if (!IsConfigurationValid())
            {
                VLog.E($"Your AppVoiceExperience \"{gameObject.name}\" does not have a wit config assigned. Understanding Viewer activations will not trigger in game events..");
                return;
            }
            if (_isActive) return;
            StopRecording();
            _lastSampleMarker = AudioBuffer.Instance.CreateMarker(ConfigurationProvider.RuntimeConfiguration.preferredActivationOffset);

            // Handle option setup
            VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);

            if (!AudioBuffer.Instance.IsRecording(this) && ShouldSendMicData)
            {
                _minKeepAliveWasHit = false;
                _isSoundWakeActive = true;

                StartRecording();
            }

            _activeTranscriptionProvider?.Activate();
            _isActive = true;

            _lastMinVolumeLevelTime = float.PositiveInfinity;
            _currentRequestOptions = requestOptions;
        }
        public void ActivateImmediately() => ActivateImmediately(new WitRequestOptions());
        public void ActivateImmediately(WitRequestOptions requestOptions)
        {
            if (!IsConfigurationValid())
            {
                VLog.E($"Your AppVoiceExperience \"{gameObject.name}\" does not have a wit config assigned. Understanding Viewer activations will not trigger in game events..");
                return;
            }
            // Make sure we aren't checking activation time until
            // the mic starts recording. If we're already recording for a live
            // recording, we just triggered an activation so we will reset the
            // last minvolumetime to ensure a minimum time from activation time
            _lastMinVolumeLevelTime = float.PositiveInfinity;
            _lastWordTime = float.PositiveInfinity;
            _receivedTranscription = false;

            // Handle option setup
            VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);

            if (ShouldSendMicData)
            {
                _recordingRequest = WitRequestProvider != null ? WitRequestProvider.CreateWitRequest(RuntimeConfiguration.witConfiguration, requestOptions, _dynamicEntityProviders)
                    : RuntimeConfiguration.witConfiguration.CreateSpeechRequest(requestOptions, _dynamicEntityProviders);
                _recordingRequest.audioEncoding = AudioBuffer.Instance.AudioEncoding;
                _recordingRequest.onPartialTranscription = OnPartialTranscription;
                _recordingRequest.onFullTranscription = OnFullTranscription;
                _recordingRequest.onInputStreamReady = r => OnWitReadyForData();
                _recordingRequest.onPartialResponse += HandlePartialResult;
                _recordingRequest.onResponse += HandleResult;
                VoiceEvents.OnRequestCreated?.Invoke(_recordingRequest);
                _recordingRequest.Request();
                _timeLimitCoroutine = StartCoroutine(DeactivateDueToTimeLimit());
            }

            if (!_isActive)
            {
                _activeTranscriptionProvider?.Activate();
                _isActive = true;
            }

#if DEBUG_SAMPLE
            if (null == sampleFile)
            {
                var file = Application.dataPath + "/test.pcm";
                sampleFile = File.Open(file, FileMode.Create);
                VLog.D("Writing recording to file: " + file);
            }
#endif
            _lastSampleMarker = AudioBuffer.Instance.CreateMarker(ConfigurationProvider
                .RuntimeConfiguration.preferredActivationOffset);
        }
        /// <summary>
        /// Send text data to Wit.ai for NLU processing
        /// </summary>
        /// <param name="text">Text to be processed</param>
        /// <param name="requestOptions">Additional options</param>
        public void Activate(string text) => Activate(text, new WitRequestOptions());
        public void Activate(string text, WitRequestOptions requestOptions)
        {
            if (!IsConfigurationValid())
            {
                VLog.E($"Your AppVoiceExperience \"{gameObject.name}\" does not have a wit config assigned. Understanding Viewer activations will not trigger in game events..");
                return;
            }

            // Handle option setup
            VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);

            // Send transcription
            SendTranscription(text, requestOptions);
        }
        /// <summary>
        /// Check configuration, client access token & app id
        /// </summary>
        public virtual bool IsConfigurationValid()
        {
            return RuntimeConfiguration.witConfiguration != null &&
                   !string.IsNullOrEmpty(RuntimeConfiguration.witConfiguration.GetClientAccessToken());
        }
        #endregion

        #region RECORDING
        // Stop any recording
        private void StopRecording()
        {
            if (!AudioBuffer.Instance.IsRecording(this)) return;

            AudioBuffer.Instance.StopRecording(this);

#if DEBUG_SAMPLE
            if (null != sampleFile)
            {
                VLog.D($"Wrote test samples to {Application.dataPath}/test.pcm");
                sampleFile?.Close();
                sampleFile = null;
            }
#endif
        }
        // When wit is ready, start recording
        private void OnWitReadyForData()
        {
            _lastMinVolumeLevelTime = _time;
            if (!AudioBuffer.Instance.IsRecording(this))
            {
                StartRecording();
            }
        }
        // Handle begin recording
        private void StartRecording()
        {
            // Check for input
            if (!AudioBuffer.Instance.IsInputAvailable)
            {
                AudioBuffer.Instance.CheckForInput();
            }
            // Wait for input and then try again
            if (!AudioBuffer.Instance.IsInputAvailable)
            {
                VoiceEvents.OnError.Invoke("Input Error", "No input source was available. Cannot activate for voice input.");
                return;
            }
            // Already recording
            if (AudioBuffer.Instance.IsRecording(this))
            {
                return;
            }

            // Start recording
            AudioBuffer.Instance.StartRecording(this);
        }
        // Callback for mic start
        private void OnMicStartListening()
        {
            VoiceEvents?.OnStartListening?.Invoke();
        }
        // Callback for mic end
        private void OnMicStoppedListening()
        {
            VoiceEvents?.OnStoppedListening?.Invoke();
        }

        private void OnByteDataReady(byte[] buffer, int offset, int length)
        {
            VoiceEvents?.OnByteDataReady.Invoke(buffer, offset, length);

            for (int i = 0; null != _dataReadyHandlers && i < _dataReadyHandlers.Length; i++)
            {
                _dataReadyHandlers[i].OnWitDataReady(buffer, offset, length);
            }
        }

        // Callback for mic sample ready
        private void OnMicSampleReady(RingBuffer<byte>.Marker marker, float levelMax)
        {
            if (null == _lastSampleMarker) return;

            if (_minSampleByteCount > _lastSampleMarker.RingBuffer.Capacity)
            {
                _minSampleByteCount = _lastSampleMarker.RingBuffer.Capacity;
            }

            if (IsRequestActive && _recordingRequest.IsRequestStreamActive && _lastSampleMarker.AvailableByteCount >= _minSampleByteCount)
            {
                // Flush the marker since the last read and send it to Wit
                _lastSampleMarker.ReadIntoWriters(
                    (buffer, offset, length) =>
                    {
                        _recordingRequest.Write(buffer, offset, length);
                        #if DEBUG_SAMPLE
                        sampleFile?.Write(buffer, offset, length);
                        #endif
                    },
                    (buffer, offset, length) => VoiceEvents?.OnByteDataSent?.Invoke(buffer, offset, length),
                    (buffer, offset, length) =>
                    {
                        for (int i = 0; i < _dataSentHandlers.Length; i++)
                        {
                            _dataSentHandlers[i]?.OnWitDataSent(buffer, offset, length);
                        }
                    });

                if (_receivedTranscription)
                {
                    float elapsed = _time - _lastWordTime;
                    if (elapsed >
                        RuntimeConfiguration.minTranscriptionKeepAliveTimeInSeconds)
                    {
                        VLog.D($"Deactivated due to inactivity. No new words detected in {elapsed:0.00} seconds.");
                        DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToInactivity);
                    }
                }
                else
                {
                    float elapsed = _time - _lastMinVolumeLevelTime;
                    if (elapsed >
                        RuntimeConfiguration.minKeepAliveTimeInSeconds)
                    {
                        VLog.D($"Deactivated due to inactivity. No sound detected in {elapsed:0.00} seconds.");
                        DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToInactivity);
                    }
                }
            }
            else if (_isSoundWakeActive && levelMax > RuntimeConfiguration.soundWakeThreshold)
            {
                VoiceEvents?.OnMinimumWakeThresholdHit?.Invoke();
                _isSoundWakeActive = false;
                ActivateImmediately(_currentRequestOptions);
                _lastSampleMarker.Offset(RuntimeConfiguration.sampleLengthInMs * -2);
            }
        }

        private void Update()
        {
            _time = Time.time;
        }

        // Mic level change
        private void OnMicLevelChanged(float level)
        {
            if (null != TranscriptionProvider && TranscriptionProvider.OverrideMicLevel) return;

            if (level > RuntimeConfiguration.minKeepAliveVolume)
            {
                _lastMinVolumeLevelTime = _time;
                _minKeepAliveWasHit = true;
            }
            VoiceEvents?.OnMicLevelChanged?.Invoke(level);
        }
        // Mic level changed in transcription
        private void OnTranscriptionMicLevelChanged(float level)
        {
            if (null != TranscriptionProvider && TranscriptionProvider.OverrideMicLevel)
            {
                OnMicLevelChanged(level);
            }
        }
        #endregion

        #region DEACTIVATION
        /// <summary>
        /// Stop listening and submit the collected microphone data to wit for processing.
        /// </summary>
        public void Deactivate()
        {
            DeactivateRequest(AudioBuffer.Instance.IsRecording(this) ? VoiceEvents?.OnStoppedListeningDueToDeactivation : null, false);
        }
        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        public void DeactivateAndAbortRequest()
        {
            VoiceEvents?.OnAborting.Invoke();
            DeactivateRequest(AudioBuffer.Instance.IsRecording(this) ? VoiceEvents?.OnStoppedListeningDueToDeactivation : null, true);
        }
        // Stop listening if time expires
        private IEnumerator DeactivateDueToTimeLimit()
        {
            yield return new WaitForSeconds(RuntimeConfiguration.maxRecordingTime);
            if (IsRequestActive)
            {
                VLog.D($"Deactivated input due to timeout.\nMax Record Time: {RuntimeConfiguration.maxRecordingTime}");
                DeactivateRequest(VoiceEvents?.OnStoppedListeningDueToTimeout, false);
            }
        }
        private void DeactivateRequest(UnityEvent onComplete = null, bool abort = false)
        {
            // Stop timeout coroutine
            if (null != _timeLimitCoroutine)
            {
                StopCoroutine(_timeLimitCoroutine);
                _timeLimitCoroutine = null;
            }

            // Stop recording
            StopRecording();

            // Deactivate transcription provider
            _activeTranscriptionProvider?.Deactivate();

            // Deactivate recording request
            bool isRecordingRequestActive = IsRequestActive;
            DeactivateWitRequest(_recordingRequest, abort);

            // Abort transmitting requests
            if (abort)
            {
                AbortQueue();
                foreach (var request in _transmitRequests)
                {
                    DeactivateWitRequest(request, true);
                }
                _transmitRequests.Clear();
            }
            // Transmit recording request
            else if (isRecordingRequestActive && _minKeepAliveWasHit)
            {
                _transmitRequests.Add(_recordingRequest);
                _recordingRequest = null;
                VoiceEvents?.OnMicDataSent?.Invoke();
            }

            // Disable below event
            _minKeepAliveWasHit = false;

            // No longer active
            _isActive = false;

            // Perform on complete event
            onComplete?.Invoke();
        }
        // Deactivate wit request
        private void DeactivateWitRequest(WitRequest request, bool abort)
        {
            if (request != null && request.IsActive)
            {
                if (abort)
                {
                    request.AbortRequest();
                }
                else
                {
                    request.CloseRequestStream();
                }
            }
        }
        #endregion

        #region TRANSCRIPTION
        private void OnPartialTranscription(string transcription)
        {
            // Clear record data
            _receivedTranscription = true;
            _lastWordTime = _time;
            // Delegate
            VoiceEvents?.OnPartialTranscription.Invoke(transcription);
        }
        private void OnFullTranscription(string transcription)
        {
            // Delegate
            VoiceEvents?.OnFullTranscription?.Invoke(transcription);
            // Send transcription
            if (RuntimeConfiguration.customTranscriptionProvider)
            {
                SendTranscription(transcription, new WitRequestOptions());
            }
        }
        private void SendTranscription(string transcription, WitRequestOptions requestOptions)
        {
            // Create request & add response delegate
            WitRequest request = RuntimeConfiguration.witConfiguration.CreateMessageRequest(transcription, requestOptions, _dynamicEntityProviders);
            request.onResponse += HandleResult;
            request.onPartialResponse += HandlePartialResult;

            // Call on create delegate
            VoiceEvents?.OnRequestCreated?.Invoke(request);

            // Add to queue
            AddToQueue(request);
        }
        #endregion

        #region QUEUE
        // Add request to wait queue
        private void AddToQueue(WitRequest request)
        {
            // In editor or disabled, do not queue
            if (!Application.isPlaying || RuntimeConfiguration.maxConcurrentRequests <= 0)
            {
                _transmitRequests.Add(request);
                request.Request();
                return;
            }

            // Add to queue
            _queuedRequests.Add(request);

            // If not running, begin
            if (_queueHandler == null)
            {
                _queueHandler = StartCoroutine(PerformDequeue());
            }
        }
        // Abort request
        private void AbortQueue()
        {
            if (_queueHandler != null)
            {
                StopCoroutine(_queueHandler);
                _queueHandler = null;
            }
            foreach (var request in _queuedRequests)
            {
                DeactivateWitRequest(request, true);
            }
            _queuedRequests.Clear();
        }
        // Coroutine used to send transcriptions when possible
        private IEnumerator PerformDequeue()
        {
            // Perform until no requests remain
            while (_queuedRequests.Count > 0)
            {
                // Wait a frame to space out requests
                yield return new WaitForEndOfFrame();

                // If space, dequeue & request
                if (_transmitRequests.Count < RuntimeConfiguration.maxConcurrentRequests)
                {
                    // Dequeue
                    WitRequest request = _queuedRequests.First();
                    _queuedRequests.Remove(request);

                    // Transmit
                    _transmitRequests.Add(request);
                    request.Request();
                }
            }

            // Kill coroutine
            _queueHandler = null;
        }
        #endregion

        #region RESPONSE
        /// <summary>
        /// Main thread call to handle partial response callbacks
        /// </summary>
        private void HandlePartialResult(WitRequest request)
        {
            if (request != null && request.ResponseData != null)
            {
                VoiceEvents?.OnPartialResponse?.Invoke(request.ResponseData);
            }
        }
        /// <summary>
        /// Main thread call to handle result callbacks
        /// </summary>
        private void HandleResult(WitRequest request)
        {
            // If result is obtained before transcription
            if (request == _recordingRequest)
            {
                DeactivateRequest(null, false);
            }

            // Handle success
            if (request.StatusCode == (int) HttpStatusCode.OK)
            {
                if (null != request.ResponseData)
                {
                    VoiceEvents?.OnResponse?.Invoke(request.ResponseData);
                }
                else
                {
                    VoiceEvents?.OnError?.Invoke("No Data", "No data was returned from the server.");
                }
            }
            // Handle failure
            else
            {
                if (request.StatusCode != WitRequest.ERROR_CODE_ABORTED)
                {
                    VoiceEvents?.OnError?.Invoke("HTTP Error " + request.StatusCode,
                        request.StatusDescription);
                }
                else
                {
                    VoiceEvents?.OnAborted?.Invoke();
                }
            }
            // Remove from transmit list, missing if aborted
            if ( _transmitRequests.Contains(request))
            {
                _transmitRequests.Remove(request);
            }

            // Complete delegate
            VoiceEvents?.OnRequestCompleted?.Invoke();
        }
        #endregion
    }

    public interface IWitRuntimeConfigProvider
    {
        WitRuntimeConfiguration RuntimeConfiguration { get; }
    }

    public interface IVoiceEventProvider
    {
        VoiceEvents VoiceEvents { get; }
    }
}
