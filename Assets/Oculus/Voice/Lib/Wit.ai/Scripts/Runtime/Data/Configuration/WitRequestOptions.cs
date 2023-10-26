/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Json;
using Meta.WitAi.Requests;
using Meta.WitAi.Interfaces;

namespace Meta.WitAi.Configuration
{
    public class WitRequestOptions : VoiceServiceRequestOptions
    {
        /// <summary>
        /// An interface that provides a list of entities that should be used for nlu resolution.
        /// </summary>
        public IDynamicEntitiesProvider dynamicEntities;

        /// <summary>
        /// The maximum number of intent matches to return
        /// </summary>
        public int nBestIntents = -1;

        /// <summary>
        /// The tag for snapshot
        /// </summary>
        public string tag;

        /// <summary>
        /// Formerly used for request id
        /// </summary>
        [Obsolete("Use 'RequestId' property instead")] [JsonIgnore]
        public string requestID => RequestId;

        /// <summary>
        /// Callback for completion
        /// </summary>
        public Action<WitRequest> onResponse;

        // Get json string. Used to get the payload for PI.
        // PI will reparse these parameters and construct it's own request.
        public string ToJsonString()
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["nBestIntents"] = nBestIntents.ToString();
            parameters["tag"] = tag;
            parameters["requestID"] = RequestId;
            foreach (var key in QueryParams.Keys)
            {
                parameters[key] = QueryParams[key];
            }
            return JsonConvert.SerializeObject(parameters);
        }
    }
}
