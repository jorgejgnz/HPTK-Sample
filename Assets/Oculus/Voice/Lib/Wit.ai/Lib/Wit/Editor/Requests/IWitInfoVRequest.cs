/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using Meta.WitAi.Data.Info;
using UnityEngine;
using UnityEngine.Networking;

namespace Meta.WitAi.Requests
{
    internal interface IWitInfoVRequest : IWitVRequest
    {
        bool RequestAppId(VRequest.RequestCompleteDelegate<string> onComplete);

        bool RequestApps(int limit, int offset, VRequest.RequestCompleteDelegate<WitAppInfo[]> onComplete);

        bool RequestAppInfo(string applicationId, VRequest.RequestCompleteDelegate<WitAppInfo> onComplete);

        bool RequestClientAppToken(string applicationId, VRequest.RequestCompleteDelegate<string> onComplete);

        bool RequestIntentList(VRequest.RequestCompleteDelegate<WitIntentInfo[]> onComplete);

        bool RequestIntentInfo(string intentId, VRequest.RequestCompleteDelegate<WitIntentInfo> onComplete);

        bool RequestEntityList(VRequest.RequestCompleteDelegate<WitEntityInfo[]> onComplete);

        bool RequestEntityInfo(string entityId, VRequest.RequestCompleteDelegate<WitEntityInfo> onComplete);

        bool RequestTraitList(VRequest.RequestCompleteDelegate<WitTraitInfo[]> onComplete);

        bool RequestTraitInfo(string traitId, VRequest.RequestCompleteDelegate<WitTraitInfo> onComplete);

        bool RequestVoiceList(VRequest.RequestCompleteDelegate<Dictionary<string, WitVoiceInfo[]>> onComplete);
    }
}
