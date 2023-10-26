/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;
using Meta.WitAi.Json;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi.Requests
{
    public class WitInfoVRequest : WitVRequest, IWitInfoVRequest
    {
        /// <summary>
        /// Constructor for wit based info VRequests
        /// </summary>
        /// <param name="configuration">The configuration interface to be used</param>
        /// <param name="useServerToken">Editor only option to use server token instead of client token</param>
        /// <param name="onDownloadProgress">The callback for progress related to downloading</param>
        /// <param name="onFirstResponse">The callback for the first response of data from a request</param>
        public WitInfoVRequest(IWitRequestConfiguration configuration, bool useServerToken = true,
            RequestProgressDelegate onDownloadProgress = null,
            RequestFirstResponseDelegate onFirstResponse = null)
            : base(configuration, null, useServerToken, onDownloadProgress, onFirstResponse) {}

        // Get all apps & return the current app info
        public bool RequestAppId(RequestCompleteDelegate<string> onComplete)
        {
            Dictionary<string, string> uriParameters = new Dictionary<string, string>();
            uriParameters[WitEditorConstants.ENDPOINT_APPS_LIMIT] = 10000.ToString();
            uriParameters[WitEditorConstants.ENDPOINT_APPS_OFFSET] = 0.ToString();
            return RequestWitGet<WitResponseNode>(WitEditorConstants.ENDPOINT_APPS, uriParameters, (results, error) =>
            {
                if (string.IsNullOrEmpty(error) && results != null)
                {
                    WitResponseArray nodes = results.AsArray;
                    if (nodes != null)
                    {
                        foreach (WitResponseNode node in nodes)
                        {
                            WitResponseClass child = node.AsObject;
                            if (child.HasChild(WitEditorConstants.ENDPOINT_APP_FOR_TOKEN) && child[WitEditorConstants.ENDPOINT_APP_FOR_TOKEN].AsBool && child.HasChild(WitEditorConstants.ENDPOINT_APP_ID))
                            {
                                onComplete?.Invoke(child[WitEditorConstants.ENDPOINT_APP_ID], null);
                                return;
                            }
                        }
                    }
                    error = "No app id found for token";
                }
                onComplete?.Invoke(null, error);
            });
        }

        // Gets all app data
        public bool RequestApps(int limit, int offset,
            RequestCompleteDelegate<WitAppInfo[]> onComplete)
        {
            Dictionary<string, string> uriParameters = new Dictionary<string, string>();
            uriParameters[WitEditorConstants.ENDPOINT_APPS_LIMIT] = Mathf.Max(limit, 1).ToString();
            uriParameters[WitEditorConstants.ENDPOINT_APPS_OFFSET] = Mathf.Max(offset, 0).ToString();
            return RequestWitGet<WitAppInfo[]>(WitEditorConstants.ENDPOINT_APPS, uriParameters, onComplete);
        }

        // Get app info request
        public bool RequestAppInfo(string applicationId,
            RequestCompleteDelegate<WitAppInfo> onComplete)
        {
            return RequestWitGet<WitAppInfo>($"{WitEditorConstants.ENDPOINT_APPS}/{applicationId}", null,
                onComplete);
        }

        // Get the export url to download
        public bool RequestAppExportInfo(string applicationId,
            RequestCompleteDelegate<WitExportInfo> onComplete)
        {
            return RequestWitGet<WitExportInfo>(WitEditorConstants.ENDPOINT_EXPORT, null,
                onComplete);
        }

        public delegate void RequestCompleteDelegateByAppId<TResult>(string appId, TResult result, string error);
        // Download the export zip from provided url
        public bool RequestAppExportZip(string downloadUri, string appId,
            RequestCompleteDelegateByAppId<ZipArchive> onComplete)
        {
            var uri = new Uri(downloadUri);
            var request = new VRequest();
            request.RequestFile(uri, (result,error) =>
            {
                try
                {
                    var zip = new ZipArchive(new MemoryStream(result));
                    onComplete(appId, zip, null);
                }
                catch (Exception e)
                {
                    onComplete(appId, null, e.ToString());
                }
            });
            return true;
        }
        // Retrieve the version tags for the app
        public bool RequestAppVersionTags(string applicationId,
            RequestCompleteDelegate<WitVersionTagInfo[][]> onComplete)
        {
            return RequestWitGet<WitVersionTagInfo[][]>($"{WitEditorConstants.ENDPOINT_APPS}/{applicationId}/{WitEditorConstants.ENDPOINT_TAGS}", null, onComplete);
        }

        // Obtain client app token
        public bool RequestClientAppToken(string applicationId,
            RequestCompleteDelegate<string> onComplete)
        {
            var jsonNode = new WitResponseClass()
            {
                { "refresh", "false" }
            };
            return RequestWitPost<WitResponseNode>($"{WitEditorConstants.ENDPOINT_APPS}/{applicationId}/{WitEditorConstants.ENDPOINT_CLIENTTOKENS}",
                null, jsonNode.ToString(),
                (results, error) =>
                {
                    if (string.IsNullOrEmpty(error))
                    {
                        WitResponseClass child = results.AsObject;
                        if (child.HasChild(WitEditorConstants.ENDPOINT_CLIENTTOKENS_VAL))
                        {
                            onComplete?.Invoke(child[WitEditorConstants.ENDPOINT_CLIENTTOKENS_VAL].Value, error);
                            return;
                        }

                        error = $"No client app token found for app\nApp: {applicationId}";
                    }
                    onComplete?.Invoke(null, error);
                });
        }

        // Obtain wit app intents
        public bool RequestIntentList(RequestCompleteDelegate<WitIntentInfo[]> onComplete)
        {
            return RequestWitGet<WitIntentInfo[]>(WitEditorConstants.ENDPOINT_INTENTS, null, onComplete);
        }

        // Get specific intent info
        public bool RequestIntentInfo(string intentId, RequestCompleteDelegate<WitIntentInfo> onComplete)
        {
            return RequestWitGet<WitIntentInfo>($"{WitEditorConstants.ENDPOINT_INTENTS}/{intentId}",
                null, onComplete);
        }

        // Obtain wit app entities
        public bool RequestEntityList(RequestCompleteDelegate<WitEntityInfo[]> onComplete)
        {
            return RequestWitGet<WitEntityInfo[]>(WitEditorConstants.ENDPOINT_ENTITIES,
                null, onComplete);
        }

        // Get specific entity info
        public bool RequestEntityInfo(string entityId,
            RequestCompleteDelegate<WitEntityInfo> onComplete)
        {
            return RequestWitGet<WitEntityInfo>($"{WitEditorConstants.ENDPOINT_ENTITIES}/{entityId}",
                null, onComplete);
        }

        // Obtain wit app traits
        public bool RequestTraitList(RequestCompleteDelegate<WitTraitInfo[]> onComplete)
        {
            return RequestWitGet<WitTraitInfo[]>(WitEditorConstants.ENDPOINT_TRAITS,
                null, onComplete);
        }

        // Get specific trait info
        public bool RequestTraitInfo(string traitId,
            RequestCompleteDelegate<WitTraitInfo> onComplete)
        {
            return RequestWitGet<WitTraitInfo>($"{WitEditorConstants.ENDPOINT_TRAITS}/{traitId}",
                null, onComplete);
        }

        // Obtain wit app voices in a dictionary format
        public bool RequestVoiceList(RequestCompleteDelegate<Dictionary<string, WitVoiceInfo[]>> onComplete)
        {
            return RequestWitGet<Dictionary<string, WitVoiceInfo[]>>(WitEditorConstants.ENDPOINT_TTS_VOICES, null, onComplete);
        }
    }
}
