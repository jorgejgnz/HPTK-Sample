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

using System;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.Voice.TelemetryUtilities
{
    internal class TelemetryConsentManager
    {
        /// <summary>
        /// The editor prefs key holding the latest consent status.
        /// </summary>
        private const string TELEMETRY_CONSENT_KEY = "VoiceSdk.Telemetry.Consent";

        /// <summary>
        /// The editor prefs key holding the date consent was last provided or withdrawn by user.
        /// </summary>
        private const string TELEMETRY_CONSENT_DATE_KEY = "VoiceSdk.Telemetry.ConsentDate";

        public const string DISCLOSURE_TEXT =
            "Shared edit-time data regarding your interactions with Unity’s SDK features will be used to enhance the developer experience. The data shared may include callback method signatures (but not method names), SDK features used, and errors resulting from our SDK.";

        private static bool? _consentProvided = null;

        /// <summary>
        /// True if consent has been provided, false if denied, and null if not set to either yet.
        /// Will prompt the user to provide consent if not set yet.
        /// </summary>
        public static bool ConsentProvided
        {
            get
            {
                if (_consentProvided != null)
                {
                    return _consentProvided.Value;
                }

                var keyExists = EditorPrefs.HasKey(TELEMETRY_CONSENT_KEY);
                if (!keyExists)
                {
                    _consentProvided = EditorUtility.DisplayDialog("Share edit-time telemetry with Meta?", DISCLOSURE_TEXT, "Share", "Withhold");
                    StoreConsentPref(_consentProvided.Value);
                }
                else
                {
                    _consentProvided = EditorPrefs.GetBool(TELEMETRY_CONSENT_KEY);
                }

                return _consentProvided.Value;

            }

            set
            {
                if (_consentProvided == value)
                {
                    return;
                }

                _consentProvided = value;
                StoreConsentPref(value);
            }
        }

        private static void StoreConsentPref(bool consented)
        {
                EditorPrefs.SetBool(TELEMETRY_CONSENT_KEY, consented);
                EditorPrefs.SetString(TELEMETRY_CONSENT_DATE_KEY, DateTime.UtcNow.ToString(CultureInfo.InvariantCulture));

                Telemetry.ConsentProvided = consented;
        }
    }
}
