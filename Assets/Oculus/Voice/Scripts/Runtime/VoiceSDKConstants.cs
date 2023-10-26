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

using System.Text;
using Meta.WitAi;
using Meta.WitAi.Requests;
using UnityEngine;

namespace Oculus.Voice
{
    /// <summary>
    /// Script for obtaining Voice SDK specific
    /// constants, including sdk version number.
    /// </summary>
    public static class VoiceSDKConstants
    {
        // On first access, initialize
        static VoiceSDKConstants()
        {
            Init();
        }
        // On load, initialize
        [RuntimeInitializeOnLoadMethod]
        static void Init()
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;
            WitVRequest.OnProvideCustomUserAgent += OnCustomUserAgent;
        }
        private static bool _isInitialized = false;

        // Current Voice SDK Version
        public static string SdkVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_sdkVersion))
                {
                    _sdkVersion = WitConstants.SDK_VERSION;
                }
                return _sdkVersion;
            }
        }
        // Overridden by build process
        private static string _sdkVersion = "57.0.0.193.153";

        // User agent prefix
        private const string _userAgentPrefix = "voice-sdk-";
        // Append voice sdk if possible
        private static void OnCustomUserAgent(StringBuilder sb)
        {
            if (!sb.ToString().StartsWith(_userAgentPrefix))
            {
                sb.Insert(0, $"{_userAgentPrefix}{SdkVersion},");
            }
        }
    }
}
