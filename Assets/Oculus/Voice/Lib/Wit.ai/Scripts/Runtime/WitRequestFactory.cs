/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text;
using System.Collections.Generic;
using System.Web;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Data.Entities;
using Meta.WitAi.Interfaces;
using Meta.WitAi.Json;

namespace Meta.WitAi
{
    public static class WitRequestFactory
    {
        private static WitRequest.QueryParam QueryParam(string key, string value)
        {
            return new WitRequest.QueryParam() { key = key, value = value };
        }

        private static void HandleWitRequestOptions(WitRequestOptions requestOptions,
            IDynamicEntitiesProvider[] additionalEntityProviders,
            List<WitRequest.QueryParam> queryParams)
        {
            WitResponseClass entities = new WitResponseClass();
            bool hasEntities = false;

            if (null != additionalEntityProviders)
            {
                foreach (var provider in additionalEntityProviders)
                {
                    foreach (var providerEntity in provider.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, providerEntity);
                    }
                }
            }

            if (DynamicEntityKeywordRegistry.HasDynamicEntityRegistry)
            {
                foreach (var providerEntity in DynamicEntityKeywordRegistry.Instance.GetDynamicEntities())
                {
                    hasEntities = true;
                    MergeEntities(entities, providerEntity);
                }
            }

            if (null != requestOptions)
            {
                if (!string.IsNullOrEmpty(requestOptions.tag))
                {
                    queryParams.Add(QueryParam("tag", requestOptions.tag));
                }

                if (null != requestOptions.dynamicEntities)
                {
                    foreach (var entity in requestOptions.dynamicEntities.GetDynamicEntities())
                    {
                        hasEntities = true;
                        MergeEntities(entities, entity);
                    }
                }
            }

            if (hasEntities)
            {
                queryParams.Add(QueryParam("entities", entities.ToString()));
            }
        }

        private static void MergeEntities(WitResponseClass entities, WitDynamicEntity providerEntity)
        {
            if (!entities.HasChild(providerEntity.entity))
            {
                entities[providerEntity.entity] = new WitResponseArray();
            }
            var mergedArray = entities[providerEntity.entity];
            Dictionary<string, WitResponseClass> map = new Dictionary<string, WitResponseClass>();
            HashSet<string> synonyms = new HashSet<string>();
            var existingKeywords = mergedArray.AsArray;
            for (int i = 0; i < existingKeywords.Count; i++)
            {
                var keyword = existingKeywords[i].AsObject;
                var key = keyword["keyword"].Value;
                if(!map.ContainsKey(key))
                {
                    map[key] = keyword;
                }
            }
            foreach (var keyword in providerEntity.keywords)
            {
                if (map.TryGetValue(keyword.keyword, out var keywordObject))
                {
                    foreach (var synonym in keyword.synonyms)
                    {
                        keywordObject["synonyms"].Add(synonym);
                    }
                }
                else
                {
                    keywordObject = JsonConvert.SerializeToken(keyword).AsObject;
                    map[keyword.keyword] = keywordObject;
                    mergedArray.Add(keywordObject);
                }
            }
        }

        /// <summary>
        /// Creates a message request that will process a query string with NLU
        /// </summary>
        /// <param name="config"></param>
        /// <param name="query">Text string to process with the NLU</param>
        /// <returns></returns>
        public static WitRequest CreateMessageRequest(this WitConfiguration config, string query, WitRequestOptions requestOptions, IDynamicEntitiesProvider[] additionalDynamicEntities = null)
        {
            List<WitRequest.QueryParam> queryParams = new List<WitRequest.QueryParam>
            {
                QueryParam("q", query)
            };

            if (null != requestOptions && -1 != requestOptions.nBestIntents)
            {
                queryParams.Add(QueryParam("n", requestOptions.nBestIntents.ToString()));
            }

            HandleWitRequestOptions(requestOptions, additionalDynamicEntities, queryParams);

            if (null != requestOptions && !string.IsNullOrEmpty(requestOptions.tag))
            {
                queryParams.Add(QueryParam("tag", requestOptions.tag));
            }

            var path = WitEndpointConfig.GetEndpointConfig(config).Message;
            WitRequest request = new WitRequest(config, path, queryParams.ToArray());

            if (null != requestOptions)
            {
                request.onResponse += requestOptions.onResponse;
                request.requestIdOverride = requestOptions.requestID;
            }

            return request;
        }

        /// <summary>
        /// Creates a request for nlu processing that includes a data stream for mic data
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static WitRequest CreateSpeechRequest(this WitConfiguration config, WitRequestOptions requestOptions, IDynamicEntitiesProvider[] additionalEntityProviders = null)
        {
            List<WitRequest.QueryParam> queryParams = new List<WitRequest.QueryParam>();

            if (null != requestOptions && -1 != requestOptions.nBestIntents)
            {
                queryParams.Add(QueryParam("n", requestOptions.nBestIntents.ToString()));
            }

            HandleWitRequestOptions(requestOptions, additionalEntityProviders, queryParams);

            var path = WitEndpointConfig.GetEndpointConfig(config).Speech;
            WitRequest request = new WitRequest(config, path, queryParams.ToArray());

            if (null != requestOptions)
            {
                request.onResponse += requestOptions.onResponse;
                request.requestIdOverride = requestOptions.requestID;
            }

            return request;
        }

        /// <summary>
        /// Creates a request for getting the transcription from the mic data
        /// </summary>
        ///<param name="config"></param>
        /// <param name="requestOptions"></param>
        /// <returns>WitRequest</returns>
        public static WitRequest CreateDictationRequest(this WitConfiguration config, WitRequestOptions requestOptions)
        {
            List<WitRequest.QueryParam> queryParams = new List<WitRequest.QueryParam>();
            var path = WitEndpointConfig.GetEndpointConfig(config).Dictation;
            WitRequest request = new WitRequest(config, path, queryParams.ToArray());
            if (null != requestOptions)
            {
                request.onResponse += requestOptions.onResponse;
                request.requestIdOverride = requestOptions.requestID;
            }

            return request;
        }

        #region IDE Only Requests
        #if UNITY_EDITOR

        /// <summary>
        /// Add a specific intent to the app
        /// </summary>
        /// <param name="config"></param>
        /// <param name="intentName">The name of the intent</param>
        /// <returns></returns>
        public static WitRequest PostIntentRequest(this WitConfiguration config, string intentName)
        {
            var json = new WitResponseClass()
            {
                {"name", intentName} 
            };

            var postData = Encoding.UTF8.GetBytes(json.ToString());
            var request = new WitRequest(config, WitConstants.ENDPOINT_INTENTS, true)
            {
                postContentType = "application/json",
                postData = postData
            };

            return request;
        }

        /// <summary>
        /// Import app data from generated manifest JSON
        /// </summary>
        /// <param name="config"></param>
        /// <param name="appName">The name of the app as it is defined in wit.ai</param>
        /// <param name="manifestData">The serialized manifest to import from</param>
        /// <returns>Built request object</returns>
        public static WitRequest CreateImportDataRequest(this WitConfiguration config, string appName, string manifestData)
        {
            var jsonNode = new WitResponseClass()
            {
                { "text", manifestData },
                { "config_type", "1" },
                { "config_value", "" } 
            };

            var postData = Encoding.UTF8.GetBytes(jsonNode.ToString());
            var request = new WitRequest(
                config,
                WitConstants.ENDPOINT_IMPORT,
                true,
                QueryParam("name", appName),
                QueryParam("private", "true"),
                QueryParam("action_graph", "true"))
            {
                postContentType = "application/json",
                postData = postData,
                forcedHttpMethodType = "PUT"
            };

            return request;
        }

        #endif
        #endregion
    }
}
