/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Meta.Voice.VSDKHub;
using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.WitAi;
using Oculus.Voice.Utility;
using Oculus.Voice;
using UnityEngine;

namespace Meta.Voice
{
    [MetaHubPage("About", VoiceHubConstants.CONTEXT_VOICE,  priority: 1000)]
    public class AboutWindow : IMetaHubPage
    {
        private Vector2 _offset;

        public void OnGUI()
        {
            Vector2 size;
            WitEditorUI.LayoutWindow(VoiceSDKStyles.Texts.AboutTitleLabel, null, null, null, OnWindowGUI, ref _offset, out size);
        }

        private void OnWindowGUI()
        {
            WitEditorUI.LayoutKeyLabel(VoiceSDKStyles.Texts.AboutVoiceSdkVersionLabel, VoiceSDKConstants.SdkVersion);
            WitEditorUI.LayoutKeyLabel(VoiceSDKStyles.Texts.AboutWitSdkVersionLabel, WitConstants.SDK_VERSION);
            WitEditorUI.LayoutKeyLabel(VoiceSDKStyles.Texts.AboutWitApiVersionLabel, WitConstants.API_VERSION);

            GUILayout.Space(16);

            if (WitEditorUI.LayoutTextButton(VoiceSDKStyles.Texts.AboutTutorialButtonLabel))
            {
                Application.OpenURL(VoiceSDKStyles.Texts.AboutTutorialButtonUrl);
            }
        }
    }
}
