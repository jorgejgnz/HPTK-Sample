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
using System.Linq;
using Meta.WitAi.Data.Info;
using Meta.WitAi.Json;
using UnityEditor;

namespace Meta.WitAi.Requests
{
    internal class WitSyncVRequest : WitVRequest, IWitSyncVRequest
    {
        // Constructor
        public WitSyncVRequest(IWitRequestConfiguration configuration) : base(configuration, true) {}

        /// <summary>
        /// Submits an intent to be added to the current wit app
        /// </summary>
        /// <param name="intentInfo">The intent data to be submitted</param>
        /// <param name="onComplete">On completion that returns an intent with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        public bool RequestAddIntent(WitIntentInfo intentInfo,
            RequestCompleteDelegate<WitIntentInfo> onComplete)
        {
            string json = JsonConvert.SerializeObject(intentInfo);
            return RequestWit(WitEditorConstants.ENDPOINT_ADD_INTENT, null, json, onComplete);
        }

        /// <summary>
        /// Submits an entity to be added to the current wit app
        /// </summary>
        /// <param name="entityInfo">The entity info to be submitted</param>
        /// <param name="onComplete">On completion that returns an entity with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        public bool RequestAddEntity(WitEntityInfo entityInfo,
            RequestCompleteDelegate<WitEntityInfo> onComplete)
        {
            string json = JsonConvert.SerializeObject(entityInfo);
            return RequestWit(WitEditorConstants.ENDPOINT_ADD_ENTITY, null, json, onComplete);
        }

        /// <summary>
        /// Submits a keyword to be added to an entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity this keyword should be added to</param>
        /// <param name="keywordInfo">The keyword & synonyms submitted</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>
        public bool RequestAddEntityKeyword(string entityId,
            WitEntityKeywordInfo keywordInfo,
            RequestCompleteDelegate<WitEntityInfo> onComplete)
        {
            string json = JsonConvert.SerializeObject(keywordInfo);
            return RequestWit($"{WitEditorConstants.ENDPOINT_ADD_ENTITY}/{entityId}/{WitEditorConstants.ENDPOINT_ADD_ENTITY_KEYWORD}",
                null, json, onComplete);
        }
        
        /// <summary>
        /// Submits a synonym to be added to a keyword on the specified entity on the current wit app
        /// </summary>
        /// <param name="entityId">The entity that holds the keyword</param>
        /// <param name="keyword">The keyword we're adding the synonym to</param>
        /// <param name="synonym">The synonym we're adding</param>
        /// <param name="onComplete">On completion that returns updated entity if successful</param>
        /// <returns>False if fails to make request</returns>

        public bool RequestAddSynonym(string entityId, string keyword, string synonym, RequestCompleteDelegate<WitEntityInfo> onComplete)
        {
            string json = $"{{\"synonym\": \"{synonym}\"}}";
            return RequestWit(
                $"{WitEditorConstants.ENDPOINT_ENTITIES}/{entityId}/{WitEditorConstants.ENDPOINT_ADD_ENTITY_KEYWORD}/{keyword}/{WitEditorConstants.ENDPOINT_ADD_ENTITY_KEYWORD_SYNONYMS}",
                null, json, onComplete);        
        }

        /// <summary>
        /// Submits a trait to be added to the current wit app
        /// </summary>
        /// <param name="traitInfo">The trait data to be submitted</param>
        /// <param name="onComplete">On completion that returns a trait with unique id if successful</param>
        /// <returns>False if fails to make request</returns>
        public bool RequestAddTrait(WitTraitInfo traitInfo,
            RequestCompleteDelegate<WitTraitInfo> onComplete)
        {
            List<JsonConverter> converters = new List<JsonConverter>(JsonConvert.DefaultConverters);
            converters.Add(new WitTraitValueInfoAddConverter());
            string json = JsonConvert.SerializeObject(traitInfo, converters.ToArray());
            return RequestWit(WitEditorConstants.ENDPOINT_ADD_TRAIT, null, json, onComplete);
        }
        // Simple trait value converter since post requires string array
        private class WitTraitValueInfoAddConverter : JsonConverter
        {
            public override bool CanWrite => true;
            public override bool CanConvert(Type objectType)
            {
                return typeof(WitTraitValueInfo) == objectType;
            }
            public override WitResponseNode WriteJson(object existingValue)
            {
                return new WitResponseData(((WitTraitValueInfo)existingValue).value);
            }
        }
        /// <summary>
        /// Submits a trait value to be added to the current wit app
        /// </summary>
        /// <param name="traitId">The trait id to be submitted</param>
        /// <param name="traitValue">The trait value to be submitted</param>
        /// <param name="onComplete">On completion callback that returns updated trait if successful</param>
        /// <returns>False if fails to make request</returns>
        public bool RequestAddTraitValue(string traitId,
            string traitValue,
            RequestCompleteDelegate<WitTraitInfo> onComplete)
        {
            WitTraitValueInfo traitValInfo = new WitTraitValueInfo()
            {
                value = traitValue
            };
            string json = JsonConvert.SerializeObject(traitValInfo);
            return RequestWit($"{WitEditorConstants.ENDPOINT_ADD_TRAIT}/{traitId}/{WitEditorConstants.ENDPOINT_ADD_TRAIT_VALUE}",
                null, json, onComplete);
        }
    }
}
