/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Meta.Voice;
using Meta.WitAi;
using Meta.WitAi.Configuration;
using Meta.WitAi.Events;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Requests;
using Oculus.Voice.Core.Bindings.Android;
using Oculus.Voice.Interfaces;
using Debug = UnityEngine.Debug;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKImpl : BaseAndroidConnectionImpl<VoiceSDKBinding>,
        IPlatformVoiceService, IVCBindingEvents
    {
        private bool _isServiceAvailable = true;
        public Action OnServiceNotAvailableEvent;
        private IVoiceService _baseVoiceService;

        private bool _isActive;

        public VoiceSDKImpl(IVoiceService baseVoiceService) : base(
            "com.oculus.assistant.api.unity.immersivevoicecommands.UnityIVCServiceFragment")
        {
            _baseVoiceService = baseVoiceService;
        }

        public bool UsePlatformIntegrations
        {
            get => true;
            set => throw new NotImplementedException();
        }

        public bool PlatformSupportsWit => service.PlatformSupportsWit && _isServiceAvailable;

        public bool Active => service.Active && _isActive;
        public bool IsRequestActive => service.IsRequestActive;
        public bool MicActive => service.MicActive;
        public void SetRuntimeConfiguration(WitRuntimeConfiguration configuration)
        {
            service.SetRuntimeConfiguration(configuration);
        }

        private VoiceSDKListenerBinding eventBinding;

        public HashSet<VoiceServiceRequest> Requests { get; } = new HashSet<VoiceServiceRequest>();

        public ITranscriptionProvider TranscriptionProvider { get; set; }
        public bool CanActivateAudio()
        {
            return true;
        }

        public bool CanSend()
        {
            return true;
        }

        public override void Connect(string version)
        {
            base.Connect(version);
            eventBinding = new VoiceSDKListenerBinding(this, this);
            eventBinding.VoiceEvents.OnStoppedListening.AddListener(OnStoppedListening);
            service.SetListener(eventBinding);
            service.Connect();
            Debug.Log(
                $"Platform integration initialization complete. Platform integrations are {(PlatformSupportsWit ? "active" : "inactive")}");
        }

        public override void Disconnect()
        {
            base.Disconnect();
            if (null != eventBinding)
            {
                eventBinding.VoiceEvents.OnStoppedListening.RemoveListener(OnStoppedListening);
            }
        }

        private void OnStoppedListening()
        {
            _isActive = false;
        }

        public VoiceServiceRequest Activate(string text, WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            if (requestOptions == null)
            {
                requestOptions = new WitRequestOptions();
            }
            requestOptions.Text = text;
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            VoiceServiceRequest request = GetRequest(requestOptions, requestEvents, NLPRequestInputType.Text);
            request.Send();
            return request;
        }

        public VoiceServiceRequest Activate(WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            if (_isActive) return null;
            _isActive = true;
            if (requestOptions == null)
            {
                requestOptions = new WitRequestOptions();
            }
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            VoiceServiceRequest request = GetRequest(requestOptions, requestEvents, NLPRequestInputType.Audio);
            request.ActivateAudio();
            return request;
        }

        public VoiceServiceRequest ActivateImmediately(WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents)
        {
            if (_isActive) return null;
            _isActive = true;
            if (requestOptions == null)
            {
                requestOptions = new WitRequestOptions();
            }
            eventBinding.VoiceEvents.OnRequestOptionSetup?.Invoke(requestOptions);
            VoiceServiceRequest request = GetRequest(requestOptions, requestEvents, NLPRequestInputType.Audio, true);
            request.ActivateAudio();
            return request;
        }

        public void Deactivate()
        {
            _isActive = false;
            foreach (var request in Requests)
            {
                if (request.InputType == NLPRequestInputType.Audio)
                {
                    request.DeactivateAudio();
                }
            }
        }

        public void DeactivateAndAbortRequest()
        {
            _isActive = false;
            foreach (var request in Requests)
            {
                if (request.InputType == NLPRequestInputType.Audio)
                {
                    request.Cancel();
                }
            }
        }

        public void DeactivateAndAbortRequest(VoiceServiceRequest request)
        {
            if (!Requests.Contains(request))
            {
                return;
            }
            request.Cancel();
        }

        public void OnServiceNotAvailable(string error, string message)
        {
            _isActive = false;
            _isServiceAvailable = false;
            OnServiceNotAvailableEvent?.Invoke();
        }

        public VoiceEvents VoiceEvents
        {
            get => _baseVoiceService.VoiceEvents;
            set => _baseVoiceService.VoiceEvents = value;
        }

        public TelemetryEvents TelemetryEvents
        {
            get => _baseVoiceService.TelemetryEvents;
            set => _baseVoiceService.TelemetryEvents = value;
        }

        // Obtains a VoiceSDKImplRequest with specified parameters
        private VoiceServiceRequest GetRequest(WitRequestOptions requestOptions,
            VoiceServiceRequestEvents requestEvents,
            NLPRequestInputType inputType,
            bool audioImmediate = false)
        {
            VoiceSDKImplRequest request = new VoiceSDKImplRequest(service, inputType, audioImmediate, requestOptions, requestEvents);
            Requests.Add(request);
            request.Events.OnCancel.AddListener(OnRequestCanceled);
            request.Events.OnFailed.AddListener(OnRequestFailed);
            request.Events.OnSuccess.AddListener(OnRequestSuccess);
            request.Events.OnComplete.AddListener(OnRequestComplete);
            VoiceEvents?.OnRequestInitialized?.Invoke(request);
            return request;
        }

        // Cancelation callback
        private void OnRequestCanceled(VoiceServiceRequest request)
        {
            // Ignore if missing
            if (!Requests.Contains(request))
            {
                return;
            }

            // Canceled
            VLog.D($"Request Canceled\nReason: {request.Results.Message}");
            VoiceEvents?.OnCanceled?.Invoke(request.Results.Message);
            if (!string.Equals(request.Results.Message, WitConstants.CANCEL_MESSAGE_PRE_SEND))
            {
                VoiceEvents?.OnAborted?.Invoke();
            }
        }

        // Failure callback
        private void OnRequestFailed(VoiceServiceRequest request)
        {
            // Ignore if missing
            if (!Requests.Contains(request))
            {
                return;
            }

            // Failed
            VLog.D($"Request Failed\nError: {request.Results.Message}");
            VoiceEvents?.OnError?.Invoke("HTTP Error " + request.Results.StatusCode, request.Results.Message);
            VoiceEvents?.OnRequestCompleted?.Invoke();
        }

        // Success callback
        private void OnRequestSuccess(VoiceServiceRequest request)
        {
            // Ignore if missing
            if (!Requests.Contains(request))
            {
                return;
            }

            // Success
            VLog.D("Request Success");
            VoiceEvents?.OnResponse?.Invoke(request.Results.ResponseData);
            VoiceEvents?.OnRequestCompleted?.Invoke();
        }

        // Request completed
        private void OnRequestComplete(VoiceServiceRequest request)
        {
            // Ignore if missing
            if (!Requests.Contains(request))
            {
                return;
            }

            // Remove listeners
            request.Events.OnCancel.RemoveListener(OnRequestCanceled);
            request.Events.OnFailed.RemoveListener(OnRequestFailed);
            request.Events.OnSuccess.RemoveListener(OnRequestSuccess);
            request.Events.OnComplete.RemoveListener(OnRequestComplete);

            // Remove request
            Requests.Remove(request);
            VoiceEvents?.OnComplete?.Invoke(request);
        }
    }
}
