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
using System.Linq;
using Meta.WitAi;
using Meta.WitAi.Events;
using Meta.WitAi.Requests;
using UnityEngine;

namespace Oculus.Voice.Bindings.Android
{
    public class VoiceSDKListenerBinding : AndroidJavaProxy
    {
        private IVoiceService _voiceService;
        private readonly IVCBindingEvents _bindingEvents;

        public VoiceEvents VoiceEvents => _voiceService.VoiceEvents;
        public TelemetryEvents TelemetryEvents => _voiceService.TelemetryEvents;

        public enum StoppedListeningReason : int {
            NoReasonProvided = 0,
            Inactivity = 1,
            Timeout = 2,
            Deactivation = 3,
        }

        public VoiceSDKListenerBinding(IVoiceService voiceService, IVCBindingEvents bindingEvents) : base(
            "com.oculus.assistant.api.voicesdk.immersivevoicecommands.IVCEventsListener")
        {
            _voiceService = voiceService;
            _bindingEvents = bindingEvents;
        }

        // Get request for a specified request id
        private VoiceServiceRequest GetRequest(string requestId)
        {
            HashSet<VoiceServiceRequest> requests = _voiceService.Requests;
            if (requests == null || requests.Count == 0)
            {
                return null;
            }
            foreach (var request in requests)
            {
                string checkRequestId = request?.Options?.RequestId;
                if (string.Equals(requestId, checkRequestId))
                {
                    return request;
                }
            }
            return requests.First();
        }

        /// <summary>
        /// Callback for listening start
        /// </summary>
        public void onStartListening(string requestId)
        {
            // Event callbacks
            VoiceEvents.OnStartListening?.Invoke();
        }
        public void onStartListening() => onStartListening(null);

        /// <summary>
        /// Callback for listening completion
        /// </summary>
        public void onStoppedListening(int reason, string requestId)
        {
            // Request callbacks
            var request = GetRequest(requestId);

            // Event callbacks
            VoiceEvents.OnStoppedListening?.Invoke();
            switch((StoppedListeningReason)reason){
                case StoppedListeningReason.NoReasonProvided:
                    break;
                case StoppedListeningReason.Inactivity:
                    VoiceEvents.OnStoppedListeningDueToInactivity?.Invoke();
                    request.Cancel();
                    break;
                case StoppedListeningReason.Timeout:
                    VoiceEvents.OnStoppedListeningDueToTimeout?.Invoke();
                    request.Cancel();
                    break;
                case StoppedListeningReason.Deactivation:
                    VoiceEvents.OnStoppedListeningDueToDeactivation?.Invoke();
                    request.Cancel();
                    break;
            }
        }
        public void onStoppedListening(int reason) => onStoppedListening(reason, null);

        /// <summary>
        /// Request submission callback
        /// </summary>
        /// <param name="requestId">The associated unique request identifier</param>
        public void onRequestCreated(string requestId)
        {
            // Request callbacks
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandleTransmissionBegan();
            }

            // Event callbacks
#pragma warning disable CS0618
            VoiceEvents.OnRequestCreated?.Invoke(null);
            VoiceEvents.OnSend?.Invoke(null);
        }
        private void onRequestCreated() => onRequestCreated(null);

        /// <summary>
        /// Partial transcription set
        /// </summary>
        /// <param name="transcription"></param>
        /// <param name="requestId"></param>
        public void onPartialTranscription(string transcription, string requestId)
        {
            // Request callbacks
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandlePartialTranscription(transcription);
            }

            // Partial transcription callback
            VoiceEvents.OnPartialTranscription?.Invoke(transcription);
        }
        public void onPartialTranscription(string transcription) => onPartialTranscription(transcription, null);

        /// <summary>
        /// Final transcription received
        /// </summary>
        /// <param name="transcription"></param>
        /// <param name="requestId"></param>
        public void onFullTranscription(string transcription, string requestId)
        {
            // Request callbacks
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandleFullTranscription(transcription);
            }

            // Transcription callback
            VoiceEvents.OnFullTranscription?.Invoke(transcription);
        }
        public void onFullTranscription(string transcription) => onFullTranscription(transcription, null);

        /// <summary>
        /// Callback when early response data has been received
        /// </summary>
        /// <param name="responseJson">The unparsed json data</param>
        /// <param name="requestId">The associated unique request identifier</param>
        public void onPartialResponse(string responseJson, string requestId)
        {
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandlePartialResponse(responseJson);
            }
        }
        public void onPartialResponse(string responseJson) => onPartialResponse(responseJson, null);

        /// <summary>
        /// Called when user request cancellation has occured
        /// </summary>
        /// <param name="requestId">The associated unique request identifier</param>
        public void onAborted(string requestId)
        {
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandleCanceled();
            }
        }
        public void onAborted() => onAborted(null);

        /// <summary>
        /// Called when an error message has been received
        /// </summary>
        /// <param name="error">The error itself</param>
        /// <param name="message">The error message</param>
        /// <param name="errorBody">The full body of the message</param>
        /// <param name="requestId">The associated unique request identifier</param>
        public void onError(string error, string message, string errorBody, string requestId)
        {
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandleError(error, message, errorBody);
            }
        }
        public void onError(string error, string message, string errorBody) => onError(error, message, errorBody, null);

        /// <summary>
        /// Callback when response data has been received
        /// </summary>
        /// <param name="responseJson">The unparsed json data</param>
        /// <param name="requestId">The associated unique request identifier</param>
        public void onResponse(string responseJson, string requestId)
        {
            var request = GetRequest(requestId);
            if (request is VoiceSDKImplRequest implRequest)
            {
                implRequest.HandleResponse(responseJson);
            }
        }
        public void onResponse(string responseJson) => onResponse(responseJson, null);


        public void onMicLevelChanged(float level, string requestId)
        {
            VoiceEvents.OnMicLevelChanged?.Invoke(level);
        }
        public void onMicLevelChanged(float level) => onMicLevelChanged(level, null);

        public void onMicDataSent(string requestId)
        {
            VoiceEvents.OnMicDataSent?.Invoke();
        }
        public void onMicDataSent() => onMicDataSent(null);

        public void onMinimumWakeThresholdHit(string requestId)
        {
            VoiceEvents.OnMinimumWakeThresholdHit?.Invoke();
        }
        public void onMinimumWakeThresholdHit() => onMinimumWakeThresholdHit(null);

        public void onRequestCompleted(string requestId)
        {

        }
        public void onRequestCompleted() => onRequestCompleted(null);

        public void onServiceNotAvailable(string error, string message)
        {
            VLog.W($"Platform service is not available: {error} - {message}");
            _bindingEvents.OnServiceNotAvailable(error, message);
        }

        public void onAudioDurationTrackerFinished(long timestamp, double duration)
        {
            long ticksElapsed = NativeTimestampToDateTime(timestamp).Ticks / TimeSpan.TicksPerMillisecond;
            TelemetryEvents.OnAudioTrackerFinished?.Invoke(ticksElapsed, duration);
        }

        private DateTime NativeTimestampToDateTime(long javaTimestamp)
        {
            // Java timestamp is milliseconds past epoch
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return dateTime.AddMilliseconds(javaTimestamp);
        }
    }
}
