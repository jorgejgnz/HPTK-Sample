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
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.Voice.Samples.LightTraits
{
    public class LightSwitch : MonoBehaviour
    {
        [Tooltip("Renderers to apply materials to")]
        [SerializeField] private Renderer[] _renderers;

        [Tooltip("Materials to be applied when the light is off")]
        [SerializeField] private Material[] _offMaterials;
        [Tooltip("Materials to be applied when the light is on")]
        [SerializeField] private Material[] _onMaterials;

        // Whether currently on or not
        public bool IsOn { get; private set; }

        // Trait data
        public const string TRAIT_ID = "wit$on_off";
        public const string TRAIT_ON_VALUE = "on";

        // Disable light on start
        private void OnEnable()
        {
            SetLight(false);
        }

        // On response callback
        public void OnResponse(WitResponseNode commandResult)
        {
            // Check for trait value
            var traitValue = commandResult.GetTraitValue(TRAIT_ID);
            if (string.IsNullOrEmpty(traitValue))
            {
                Debug.LogWarning($"No value found for trait: {TRAIT_ID}");
                return;
            }

            // Get value
            bool isOn = string.Equals(traitValue, TRAIT_ON_VALUE);
            if (isOn != IsOn)
            {
                SetLight(isOn);
            }
        }

        // Set light
        public void SetLight(bool toOn)
        {
            IsOn = toOn;
            foreach (var renderer in _renderers)
            {
                renderer.materials = toOn ? _onMaterials : _offMaterials;
            }
        }
    }
}
