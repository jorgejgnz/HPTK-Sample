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

using Meta.WitAi;
using Meta.WitAi.Data;
using UnityEngine;

namespace Meta.Voice.Samples.LiveUnderstanding
{
    public class LiveUnderstandingColorChanger : MonoBehaviour
    {
        [Tooltip("Changes the color of all renderers inside this container")]
        [SerializeField] private Transform _container;

        // Constants
        private const string COLOR_SET_INTENT_ID = "change_color";
        private const string COLOR_ENTITY_ID = "color:color";

        // On validate callback
        public void OnValidatePartialResponse(VoiceSession sessionData)
        {
            string intentName = sessionData.response.GetIntentName();
            if (string.Equals(intentName, COLOR_SET_INTENT_ID))
            {
                string[] colorNames = sessionData.response.GetAllEntityValues(COLOR_ENTITY_ID);
                if (colorNames != null && colorNames.Length > 0)
                {
                    OnValidateColorSet(sessionData, colorNames[0]);
                }
            }
        }

        // Validate & set color
        public void OnValidateColorSet(VoiceSession sessionData, string color)
        {
            Color c;
            if (TryGetColor(color, out c))
            {
                SetColor(c);
                sessionData.validResponse = true;
            }
        }

        // Try to get a color
        private bool TryGetColor(string colorName, out Color color)
        {
            // Check default
            if (ColorUtility.TryParseHtmlString(colorName, out color))
            {
                return true;
            }
            // Failed
            return false;
        }

        // Set color
        public void SetColor(Color newColor)
        {
            Renderer[] renderers = _container.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers)
            {
                r.material.color = newColor;
            }
        }
    }
}
