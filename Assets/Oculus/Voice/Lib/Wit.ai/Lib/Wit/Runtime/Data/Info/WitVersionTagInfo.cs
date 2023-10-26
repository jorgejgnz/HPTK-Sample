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
using UnityEngine.Serialization;

namespace Meta.WitAi.Data.Info
{
    [Serializable]
    public struct WitVersionTagInfo
    {
        public WitVersionTagInfo(string name, string createdAt, string updatedAt, string description)
        {
            this.name = name;
            this.created_at = createdAt;
            this.updated_at = updatedAt;
            this.desc = description;
        }

        /// <summary>
        /// The assigned name of this version tag
        /// </summary>
        [SerializeField] public string name;

        /// <summary>
        /// Date and time of the tag creation (ISO8601).
        /// </summary>
        [SerializeField] public string created_at;

        /// <summary>
        /// Date and time of the last update (move, rename or update description) (ISO8601).
        /// </summary>
        [SerializeField] public string updated_at;

        /// <summary>
        /// Short sentence describing the version.
        /// </summary>
        [SerializeField] public string desc;
    }
}
