/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using Meta.Voice;
using Meta.WitAi.Configuration;
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.WitAi.Requests
{
    [Serializable]
    public abstract class VoiceServiceRequest
        : NLPRequest<VoiceServiceRequestEvent, WitRequestOptions, VoiceServiceRequestEvents, VoiceServiceRequestResults>
    {
        /// <summary>
        /// Constructor for Voice Service requests
        /// </summary>
        /// <param name="newInputType">The request input type (text/audio) to be used</param>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected VoiceServiceRequest(NLPRequestInputType newInputType, WitRequestOptions newOptions, VoiceServiceRequestEvents newEvents) : base(newInputType, newOptions, newEvents) {}

        /// <summary>
        /// The status code returned from the last request
        /// </summary>
        public int StatusCode
        {
            get => Results == null ? 0 : Results.StatusCode;
            protected set
            {
                int newCode = value;
                if (newCode.Equals(Results == null ? 0 : Results.StatusCode))
                {
                    return;
                }
                if (Results == null)
                {
                    Results = new VoiceServiceRequestResults();
                }
                Results.StatusCode = newCode;
            }
        }
        
        #region Simulation
        protected override bool OnSimulateResponse()
        {
            if (null == simulatedResponse) return false;
            

            // Begin calling on main thread if needed
            WatchMainThreadCallbacks();
            
            SimulateResponse();
            return true;
        }

        private async void SimulateResponse()
        {
            var stackTrace = new StackTrace();
            StatusCode = simulatedResponse.code;
            var statusDescription = simulatedResponse.responseDescription;
            for (int i = 0; i < simulatedResponse.messages.Count - 1; i++)
            {
                var message = simulatedResponse.messages[i];
                await System.Threading.Tasks.Task.Delay((int)(message.delay * 1000));
                var partialResponse = WitResponseNode.Parse(message.responseBody);
                HandlePartialNlpResponse(partialResponse);
            }

            var lastMessage = simulatedResponse.messages.Last();
            await System.Threading.Tasks.Task.Delay((int)(lastMessage.delay * 1000));
            var lastResponseData = WitResponseNode.Parse(lastMessage.responseBody);
            MainThreadCallback(() =>
            {
                // Send partial data if not previously sent
                if (!lastResponseData.HasResponse())
                {
                    HandlePartialNlpResponse(lastResponseData);
                }

                // Apply error if needed
                if (null != lastResponseData)
                {
                    var error = lastResponseData["error"];
                    if (!string.IsNullOrEmpty(error))
                    {
                        statusDescription += $"\n{error}";
                    }
                }

                // Call completion delegate
                HandleFinalNlpResponse(lastResponseData,
                    StatusCode == (int)HttpStatusCode.OK
                        ? string.Empty
                        : $"{statusDescription}\n\nStackTrace:\n{stackTrace}\n\n");
            });
        }

        #endregion
        
        #region Thread Safety
        // Check performing
        private CoroutineUtility.CoroutinePerformer _performer = null;
        // All actions
        private ConcurrentQueue<Action> _mainThreadCallbacks = new ConcurrentQueue<Action>();

        // While active, perform any sent callbacks
        protected void WatchMainThreadCallbacks()
        {
            // Ignore if already performing
            if (_performer != null)
            {
                return;
            }

            // Check callbacks every frame (editor or runtime)
            _performer = CoroutineUtility.StartCoroutine(PerformMainThreadCallbacks());
        }
        // Every frame check for callbacks & perform any found
        private System.Collections.IEnumerator PerformMainThreadCallbacks()
        {
            // While checking, continue
            while (HasMainThreadCallbacks())
            {
                // Wait for frame
                if (Application.isPlaying && !Application.isBatchMode)
                {
                    yield return new WaitForEndOfFrame();
                }
                // Wait for a tick
                else
                {
                    yield return null;
                }

                // Perform if possible
                while (_mainThreadCallbacks.Count > 0 && _mainThreadCallbacks.TryDequeue(out var result))
                {
                    result();
                }
            }
            _performer = null;
        }
        // If active or performing callbacks
        private bool HasMainThreadCallbacks()
        {
            return IsActive || _mainThreadCallbacks.Count > 0;
        }
        // Called from background thread
        protected void MainThreadCallback(Action action)
        {
            if (action == null)
            {
                return;
            }
            _mainThreadCallbacks.Enqueue(action);
        }
        #endregion

        /// <summary>
        /// Returns an empty result object with the current status code
        /// </summary>
        /// <param name="newMessage">The message to be set on the results</param>
        protected override VoiceServiceRequestResults GetResultsWithMessage(string newMessage)
        {
            VoiceServiceRequestResults results = new VoiceServiceRequestResults(newMessage);
            results.StatusCode = StatusCode;
            results.ResponseData = ResponseData;
            return results;
        }

        /// <summary>
        /// Applies a transcription to the current results
        /// </summary>
        /// <param name="newTranscription">The transcription returned</param>
        /// <param name="newIsFinal">Whether the transcription has completed building</param>
        protected override void ApplyTranscription(string newTranscription, bool newIsFinal)
        {
            if (Results == null)
            {
                Results = new VoiceServiceRequestResults();
            }
            Results.Transcription = newTranscription;
            Results.IsFinalTranscription = newIsFinal;
            if (Results.IsFinalTranscription)
            {
                List<string> transcriptions = new List<string>();
                if (Results.FinalTranscriptions != null)
                {
                    transcriptions.AddRange(Results.FinalTranscriptions);
                }
                transcriptions.Add(Results.Transcription);
                Results.FinalTranscriptions = transcriptions.ToArray();
            }
            OnTranscriptionChanged();
        }

        /// <summary>
        /// Applies response data to the current results
        /// </summary>
        /// <param name="newData">The returned response data</param>
        protected override void ApplyResultResponseData(WitResponseNode newData)
        {
            if (Results == null)
            {
                Results = new VoiceServiceRequestResults();
            }
            Results.ResponseData = newData;
        }

        /// <summary>
        /// Performs an event callback with this request as the parameter
        /// </summary>
        /// <param name="eventCallback">The voice service request event to be called</param>
        protected override void RaiseEvent(VoiceServiceRequestEvent eventCallback)
        {
            eventCallback?.Invoke(this);
        }
    }
}
