/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using Meta.WitAi.Data.Info;
using UnityEngine;

namespace Meta.WitAi.Data.Configuration
{
    /**
     * A portion of the Wit app's configuration data.
     */
    public abstract class WitConfigurationAssetData : ScriptableObject
    {
        /**
         * Retrieves from the server the relevant data.
         */
        public abstract void Refresh(IWitRequestConfiguration configuration);
    }
}
