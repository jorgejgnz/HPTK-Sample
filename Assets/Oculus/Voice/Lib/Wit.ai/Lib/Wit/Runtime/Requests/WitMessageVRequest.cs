/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using System.Collections.Generic;
using Meta.WitAi.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    internal class WitMessageVRequest : WitVRequest
    {
        // Constructor
        public WitMessageVRequest(IWitRequestConfiguration configuration) : base(configuration, false) {}

        /// <summary>
        /// Voice message request
        /// </summary>
        /// <param name="text">Text to be sent to message endpoint</param>
        /// <param name="onComplete">The delegate upon completion</param>
        /// <param name="onProgress">The text download progress</param>
        /// <returns>False if the request cannot be performed</returns>
        public bool MessageRequest(string text,
            RequestCompleteDelegate<WitResponseNode> onComplete,
            RequestProgressDelegate onProgress = null)
        {
            // Error without text
            if (string.IsNullOrEmpty(text))
            {
                onComplete?.Invoke(null, "Cannot perform message request without text");
                return false;
            }

            // Add text to uri parameters
            Dictionary<string, string> uriParams = new Dictionary<string, string>();
            uriParams[WitConstants.ENDPOINT_MESSAGE_PARAM] = text;

            // Perform json request
            return RequestWit(WitConstants.ENDPOINT_MESSAGE, uriParams, onComplete, onProgress);
        }
    }
}
