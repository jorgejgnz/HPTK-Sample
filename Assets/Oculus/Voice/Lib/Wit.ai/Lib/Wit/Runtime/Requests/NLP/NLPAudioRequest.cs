/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi;
using Meta.WitAi.Json;
using UnityEngine.Events;

namespace Meta.Voice
{
    /// <summary>
    /// Abstract class for all NLP audio requests
    /// </summary>
    /// <typeparam name="TUnityEvent">The type of event callback performed by TEvents for all event callbacks</typeparam>
    /// <typeparam name="TOptions">The type containing all specific options to be passed to the end service.</typeparam>
    /// <typeparam name="TEvents">The type containing all events of TSession to be called throughout the lifecycle of the request.</typeparam>
    /// <typeparam name="TResults">The type containing all data that can be returned from the end service.</typeparam>
    public abstract class NLPAudioRequest<TUnityEvent, TOptions, TEvents, TResults>
        : TranscriptionRequest<TUnityEvent, TOptions, TEvents, TResults>,
            INLPAudioRequest<TUnityEvent, TOptions, TEvents, TResults>
        where TUnityEvent : UnityEventBase
        where TOptions : INLPAudioRequestOptions
        where TEvents : NLPAudioRequestEvents<TUnityEvent>
        where TResults : INLPAudioRequestResults
    {
        /// <summary>
        /// Constructor class for NLP audio requests
        /// </summary>
        /// <param name="newOptions">The request parameters to be used</param>
        /// <param name="newEvents">The request events to be called throughout it's lifecycle</param>
        protected NLPAudioRequest(TOptions newOptions, TEvents newEvents) : base(newOptions, newEvents) {}

        /// <summary>
        /// Getter for response data
        /// </summary>
        public WitResponseNode ResponseData => Results?.ResponseData;

        // Ensure final is not called multiple times
        private bool _isFinalized = false;

        /// <summary>
        /// Initializes the request, and ensures that the request commences in a non-finalized state.
        /// </summary>
        protected override void OnInit()
        {
            base.OnInit();
            _isFinalized = false;
        }

        /// <summary>
        /// Method to be called when an NLP request had completed
        /// </summary>
        /// <param name="responseData">Parsed json data returned from request</param>
        /// <param name="error">Error returned from a request</param>
        protected virtual void HandlePartialNlpResponse(WitResponseNode responseData)
        {
            // Ignore if not in correct state
            if (!IsActive)
            {
                return;
            }
            
            if (null != responseData) responseData[WitConstants.HEADER_REQUEST_ID] = Options.RequestId;
            
            // Apply response data
            ApplyResultResponseData(responseData);

            // Partial response called
            OnPartialResponse();
        }

        /// <summary>
        /// Applies response data to the current results
        /// </summary>
        /// <param name="newData">The returned response data</param>
        protected abstract void ApplyResultResponseData(WitResponseNode newData);

        /// <summary>
        /// Called when response data has been updated
        /// </summary>
        protected virtual void OnPartialResponse()
        {
            Events?.OnPartialResponse?.Invoke(ResponseData);
        }

        /// <summary>
        /// Method to be called when an NLP request had completed
        /// </summary>
        /// <param name="responseData">Parsed json data returned from request</param>
        /// <param name="error">Error returned from a request</param>
        protected virtual void HandleFinalNlpResponse(WitResponseNode responseData, string error)
        {
            // Ignore if not in correct state
            if (!IsActive || _isFinalized)
            {
                return;
            }
            _isFinalized = true;
            
            if (null != responseData) responseData[WitConstants.HEADER_REQUEST_ID] = Options.RequestId;

            // Send partial data if not previously sent
            if (responseData != null && responseData != ResponseData)
            {
                HandlePartialNlpResponse(responseData);
            }

            // Error returned
            if (!string.IsNullOrEmpty(error))
            {
                HandleFailure(error);
            }
            // No response
            else if (responseData == null)
            {
                HandleFailure("No response returned");
            }
            // Success
            else
            {
                // Callback for final response
                OnFullResponse();

                // Handle success
                HandleSuccess(Results);
            }
        }

        /// <summary>
        /// Called when response data has been updated for the final time
        /// </summary>
        protected virtual void OnFullResponse()
        {
            Events?.OnFullResponse?.Invoke(ResponseData);
        }

        /// <summary>
        /// Cancels the current request but handles success immediately if possible
        /// </summary>
        public virtual void CompleteEarly()
        {
            // Ignore if not in correct state
            if (!IsActive || _isFinalized)
            {
                return;
            }

            // Cancel instead
            if (ResponseData == null)
            {
                Cancel("Cannot complete early without response data");
            }
            // Handle success
            else
            {
                HandleFinalNlpResponse(ResponseData, string.Empty);
            }
        }
    }
}
