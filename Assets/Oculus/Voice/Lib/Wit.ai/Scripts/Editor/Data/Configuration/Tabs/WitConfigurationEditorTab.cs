﻿/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Text;
using Meta.WitAi.Data.Info;
using UnityEditor;
namespace Meta.WitAi.Data.Configuration.Tabs
{
    public abstract class WitConfigurationEditorTab
    {
        // the WitConfigurationData type relevant to this tab
        public abstract Type DataType { get; }

        public abstract string TabID { get; }
        public abstract int TabOrder { get; }
        public abstract string TabLabel { get; }
        public abstract string MissingLabel { get; }
        public abstract bool ShouldTabShow(WitAppInfo appInfo);
        public virtual bool ShouldTabShow(WitConfiguration configuration) { return false; }

        public virtual string GetPropertyName(string tabID)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("_appInfo");
            sb.Append($".{TabID}");
            return sb.ToString();
        }
        public virtual string GetTabText(bool titleLabel)
        {
            return titleLabel ? TabLabel : MissingLabel;
        }
    }
}
