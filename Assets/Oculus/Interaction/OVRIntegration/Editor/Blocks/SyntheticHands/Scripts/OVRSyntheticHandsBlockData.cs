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

using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks.Editor;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    public class OVRSyntheticHandsBlockData : BlockData
    {
        public string _handsBlockId;
        public string _uOIAssetsHandsBlockId;
        public GameObject _leftHand;
        public GameObject _rightHand;

        protected override List<GameObject> InstallRoutine()
        {
            var syntheticHands = new List<GameObject>();
            foreach (var hand in BlocksUtils.GetHands(_handsBlockId))
            {
                var syntheticHand = InstantiateHand(hand);
                syntheticHands.Add(syntheticHand);
            }

            DisableUOIAssetsHandVisual();

            return syntheticHands;
        }

        private GameObject InstantiateHand(Hand hand)
        {
            var handedness = hand.Handedness;
            var prefab = handedness == Handedness.Left ? _leftHand : _rightHand;
            var syntheticHand = Instantiate(prefab, hand.transform, false);
            syntheticHand.GetComponent<SyntheticHand>().InjectModifyDataFromSource(hand);
            syntheticHand.SetActive(true);
            syntheticHand.name = $"[BB] Synthetic {handedness} Hand";
            BlocksUtils.UpdateForAutoWiring(syntheticHand);
            return syntheticHand;
        }

        private void DisableUOIAssetsHandVisual()
        {
            var handsBlocks = Meta.XR.BuildingBlocks.Editor.Utils.GetBlocks(_uOIAssetsHandsBlockId);
            foreach (var hand in handsBlocks)
            {
                var skeletonRenderer = hand.GetComponent<OVRSkeletonRenderer>();
                var meshRenderer = hand.GetComponent<OVRMeshRenderer>();
                var skinnedMeshRenderer = hand.GetComponent<SkinnedMeshRenderer>();
                if (skeletonRenderer && skeletonRenderer.enabled) skeletonRenderer.enabled = false;
                if (meshRenderer && meshRenderer.enabled) meshRenderer.enabled = false;
                if (skinnedMeshRenderer && skinnedMeshRenderer.enabled) skinnedMeshRenderer.enabled = false;
            }
        }
    }
}
