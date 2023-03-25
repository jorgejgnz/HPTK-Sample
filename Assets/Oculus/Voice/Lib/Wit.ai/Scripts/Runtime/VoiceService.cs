/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.Conduit;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Data.Intents;
using Meta.WitAi.Events;
using Meta.WitAi.Events.UnityEventListeners;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;
using UnityEngine;
using Meta.WitAi;

namespace Meta.WitAi
{
    public abstract class VoiceService : MonoBehaviour, IVoiceService, IInstanceResolver, IAudioEventProvider
    {
        /// <summary>
        /// When set to true, Conduit will be used. Otherwise, the legacy dispatching will be used.
        /// </summary>
        private bool UseConduit => _witConfiguration && _witConfiguration.useConduit;

        /// <summary>
        /// The wit configuration.
        /// </summary>
        private WitConfiguration _witConfiguration;

        /// <summary>
        /// The Conduit parameter provider.
        /// </summary>
        private readonly IParameterProvider _conduitParameterProvider = new ParameterProvider();

        /// <summary>
        /// This field should not be accessed outside the Wit-Unity library. If you need access
        /// to events you should be using the VoiceService.VoiceEvents property instead.
        /// </summary>
        [Tooltip("Events that will fire before, during and after an activation")] [SerializeField]
        protected VoiceEvents events = new VoiceEvents();

        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        public abstract bool Active { get; }

        /// <summary>
        /// The Conduit-based dispatcher that dispatches incoming invocations based on a manifest.
        /// </summary>
        internal IConduitDispatcher ConduitDispatcher { get; set; }

        /// <summary>
        /// Returns true if the service is actively communicating with Wit.ai during an Activation. The mic may or may not still be active while this is true.
        /// </summary>
        public abstract bool IsRequestActive { get; }

        /// <summary>
        /// Gets/Sets a custom transcription provider. This can be used to replace any built in asr
        /// with an on device model or other provided source
        /// </summary>
        public abstract ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Returns true if this voice service is currently reading data from the microphone
        /// </summary>
        public abstract bool MicActive { get; }

        public virtual VoiceEvents VoiceEvents
        {
            get => events;
            set => events = value;
        }

        /// <summary>
        /// A subset of events around collection of audio data
        /// </summary>
        public IAudioInputEvents AudioEvents => VoiceEvents;

        /// <summary>
        /// A subset of events around receiving transcriptions
        /// </summary>
        public ITranscriptionEvent TranscriptionEvents => VoiceEvents;

        /// <summary>
        /// Returns true if the audio input should be read in an activation
        /// </summary>
        protected abstract bool ShouldSendMicData { get; }

        /// <summary>
        /// Constructs a <see cref="VoiceService"/>
        /// </summary>
        protected VoiceService()
        {
            _conduitParameterProvider.SetSpecializedParameter(ParameterProvider.WitResponseNodeReservedName, typeof(WitResponseNode));
            _conduitParameterProvider.SetSpecializedParameter(ParameterProvider.VoiceSessionReservedName, typeof(VoiceSession));
            var conduitDispatcherFactory = new ConduitDispatcherFactory(this);
            ConduitDispatcher = conduitDispatcherFactory.GetDispatcher();
        }

        /// <summary>
        /// Send text data for NLU processing. Results will return the same way a voice based activation would.
        /// </summary>
        /// <param name="text"></param>
        public void Activate(string text) => Activate(text, new WitRequestOptions());

        /// <summary>
        /// Send text data for NLU processing with custom request options.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="requestOptions"></param>
        public abstract void Activate(string text, WitRequestOptions requestOptions);

        /// <summary>
        /// Start listening for sound or speech from the user and start sending data to Wit.ai once sound or speech has been detected.
        /// </summary>
        public void Activate() => Activate(new WitRequestOptions());

        /// <summary>
        /// Activate the microphone and send data for NLU processing. Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        /// <param name="requestOptions"></param>
        public abstract void Activate(WitRequestOptions requestOptions);

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.
        /// </summary>
        public void ActivateImmediately() => ActivateImmediately(new WitRequestOptions());

        /// <summary>
        /// Activate the microphone and send data for NLU processing immediately without waiting for sound/speech from the user to begin.  Includes optional additional request parameters like dynamic entities and maximum results.
        /// </summary>
        public abstract void ActivateImmediately(WitRequestOptions requestOptions);

        /// <summary>
        /// Stop listening and submit any remaining buffered microphone data for processing.
        /// </summary>
        public abstract void Deactivate();

        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        public abstract void DeactivateAndAbortRequest();

        /// <summary>
        /// Returns objects of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>Objects of the specified type.</returns>
        public IEnumerable<object> GetObjectsOfType(Type type)
        {
            return FindObjectsOfType(type);
        }

        protected virtual void Awake()
        {
            var witConfigProvider = this.GetComponent<IWitRuntimeConfigProvider>();
            _witConfiguration = witConfigProvider?.RuntimeConfiguration?.witConfiguration;

            InitializeEventListeners();

            if (!UseConduit)
            {
                MatchIntentRegistry.Initialize();
            }
        }

        private void InitializeEventListeners()
        {
            var audioEventListener = GetComponent<AudioEventListener>();
            if (!audioEventListener)
            {
                gameObject.AddComponent<AudioEventListener>();
            }

            var transcriptionEventListener = GetComponent<TranscriptionEventListener>();
            if (!transcriptionEventListener)
            {
                gameObject.AddComponent<TranscriptionEventListener>();
            }
        }

        protected virtual void OnEnable()
        {
            if (UseConduit)
            {
                ConduitDispatcher.Initialize(_witConfiguration.ManifestLocalPath);
                if (_witConfiguration.relaxedResolution)
                {
                    if (!ConduitDispatcher.Manifest.ResolveEntities())
                    {
                        VLog.E("Failed to resolve Conduit entities");
                    }

                    foreach (var entity in ConduitDispatcher.Manifest.CustomEntityTypes)
                    {
                        _conduitParameterProvider.AddCustomType(entity.Key, entity.Value);
                    }
                }
            }
            VoiceEvents.OnPartialResponse.AddListener(ValidateShortResponse);
            VoiceEvents.OnResponse.AddListener(HandleResponse);
        }

        protected virtual void OnDisable()
        {
            VoiceEvents.OnPartialResponse.RemoveListener(ValidateShortResponse);
            VoiceEvents.OnResponse.RemoveListener(HandleResponse);
        }

        private VoiceSession GetVoiceSession(WitResponseNode response)
        {
            return new VoiceSession
            {
                service = this,
                response = response,
                validResponse = false
            };
        }

        protected virtual void ValidateShortResponse(WitResponseNode response)
        {
            if (VoiceEvents.OnValidatePartialResponse != null)
            {
                // Create short response data
                VoiceSession validationData = GetVoiceSession(response);

                // Call short response
                VoiceEvents.OnValidatePartialResponse.Invoke(validationData);

                // Invoke
                if (UseConduit)
                {
                    // Ignore without an intent
                    WitIntentData intent = response.GetFirstIntentData();
                    if (intent != null)
                    {
                        _conduitParameterProvider.PopulateParametersFromNode(response);
                        _conduitParameterProvider.AddParameter(ParameterProvider.VoiceSessionReservedName,
                            validationData);
                        _conduitParameterProvider.AddParameter(ParameterProvider.WitResponseNodeReservedName, response);
                        ConduitDispatcher.InvokeAction(_conduitParameterProvider, intent.name, _witConfiguration.relaxedResolution, intent.confidence, true);
                    }
                }

                // Deactivate
                if (validationData.validResponse)
                {
                    // Call response
                    VoiceEvents.OnResponse?.Invoke(response);

                    // Deactivate immediately
                    DeactivateAndAbortRequest();
                }
            }
        }

        protected virtual void HandleResponse(WitResponseNode response)
        {
            HandleIntents(response);
        }

        private void HandleIntents(WitResponseNode response)
        {
            var intents = response.GetIntents();
            foreach (var intent in intents)
            {
                HandleIntent(intent, response);
            }
        }

        private void HandleIntent(WitIntentData intent, WitResponseNode response)
        {
            if (UseConduit)
            {
                _conduitParameterProvider.PopulateParametersFromNode(response);
                _conduitParameterProvider.AddParameter(ParameterProvider.WitResponseNodeReservedName, response);
                ConduitDispatcher.InvokeAction(_conduitParameterProvider, intent.name,
                    _witConfiguration.relaxedResolution, intent.confidence, false);
            }
            else
            {
                var methods = MatchIntentRegistry.RegisteredMethods[intent.name];
                foreach (var method in methods)
                {
                    ExecuteRegisteredMatch(method, intent, response);
                }
            }
        }



        private void ExecuteRegisteredMatch(RegisteredMatchIntent registeredMethod,
            WitIntentData intent, WitResponseNode response)
        {
            if (intent.confidence >= registeredMethod.matchIntent.MinConfidence &&
                intent.confidence <= registeredMethod.matchIntent.MaxConfidence)
            {
                foreach (var obj in GetObjectsOfType(registeredMethod.type))
                {
                    var parameters = registeredMethod.method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        registeredMethod.method.Invoke(obj, Array.Empty<object>());
                        continue;
                    }
                    if (parameters[0].ParameterType != typeof(WitResponseNode) || parameters.Length > 2)
                    {
                        VLog.E("Match intent only supports methods with no parameters or with a WitResponseNode parameter. Enable Conduit or adjust the parameters");
                        continue;
                    }
                    if (parameters.Length == 1)
                    {
                        registeredMethod.method.Invoke(obj, new object[] {response});
                    }
                }
            }
        }
    }

    public interface IVoiceService : IVoiceEventProvider
    {
        /// <summary>
        /// Returns true if this voice service is currently active and listening with the mic
        /// </summary>
        bool Active { get; }

        bool IsRequestActive { get; }

        bool MicActive { get; }

        new VoiceEvents VoiceEvents { get; set; }

        ITranscriptionProvider TranscriptionProvider { get; set; }

        /// <summary>
        /// Send text data for NLU processing with custom request options.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="requestOptions">Custom request options</param>
        void Activate(string text, WitRequestOptions requestOptions);

        /// <summary>
        /// Activate the microphone and wait for threshold and then send data
        /// </summary>
        /// <param name="requestOptions">Custom request options</param>
        void Activate(WitRequestOptions requestOptions);

        /// <summary>
        /// Activate the microphone and send data for NLU processing with custom request options.
        /// </summary>
        /// <param name="requestOptions">Custom request options</param>
        void ActivateImmediately(WitRequestOptions requestOptions);

        /// <summary>
        /// Stop listening and submit the collected microphone data for processing.
        /// </summary>
        void Deactivate();

        /// <summary>
        /// Stop listening and abort any requests that may be active without waiting for a response.
        /// </summary>
        void DeactivateAndAbortRequest();
    }
}
