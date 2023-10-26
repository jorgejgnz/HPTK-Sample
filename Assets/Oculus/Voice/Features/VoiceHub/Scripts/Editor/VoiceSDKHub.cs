/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Threading;
using Meta.Voice.Hub;
using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Utilities;
using Meta.Voice.TelemetryUtilities;
using UnityEditor;

namespace Meta.Voice.VSDKHub
{
    [MetaHubContext(VoiceHubConstants.CONTEXT_VOICE)]
    public class VoiceSDKHubContext : MetaHubContext
    {
    }
    
    public class VoiceSDKHub : MetaHub
    {
        public static readonly List<string> Contexts = new List<string>
        {
            VoiceHubConstants.CONTEXT_VOICE
        };

        private List<string> _vsdkContexts;
        public override List<string> ContextFilter
        {
            get
            {
                if (null == _vsdkContexts || _vsdkContexts.Count == 0)
                {
                    _vsdkContexts = Contexts.ToList();
                    AddChildContexts(_vsdkContexts);
                }

                return _vsdkContexts;
            }
        }

        public static string GetPageId(string pageName)
        {
            return VoiceHubConstants.CONTEXT_VOICE + "::" + pageName;
        }
        
        [MenuItem("Oculus/Voice SDK/Voice Hub", false, 1)]
        private static void ShowWindow()
        {
            MetaHub.ShowWindow<VoiceSDKHub>(Contexts.ToArray());
        }

        public static void ShowPage(string page)
        {
            Telemetry.LogInstantEvent(Telemetry.TelemetryEventId.OpenUi, new Dictionary<Telemetry.AnnotationKey, string>()
                {
                    {Telemetry.AnnotationKey.PageId, page}
                });
            var window = MetaHub.ShowWindow<VoiceSDKHub>(Contexts.ToArray());
            window.SelectedPage = page;
        }

        protected override void OnEnable()
        {
            _vsdkContexts = null;
            base.OnEnable();
        }
    }
}
