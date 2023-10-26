/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Meta.WitAi.Data
{
    [Serializable]
    public class SimulatedResponse
    {
        public int code;
        public List<SimulatedResponseMessage> messages = new List<SimulatedResponseMessage>();
        public string responseDescription;
    }

    public class SimulatedResponseMessage
    {
        public float delay;
        [TextArea]
        public string responseBody;
    }
}
