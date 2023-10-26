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
using System.Collections.Generic;
using UnityEngine;
using Meta.XR.BuildingBlocks.Editor;

namespace Oculus.Interaction.Editor.BuildingBlocks
{
    public class OVRInteractionBlockData : BlockData
    {
        public string _cameraRigBlockId;

        protected override List<GameObject> InstallRoutine()
        {
            var cameraRigBlockData = Meta.XR.BuildingBlocks.Editor.Utils.GetBlockData(_cameraRigBlockId);
            var cameraRigBlock = cameraRigBlockData.GetBlock();
            if (cameraRigBlock == null)
            {
                throw new InvalidOperationException(
                    $"Cannot install block '{this.name}' : Cannot find block with type {cameraRigBlock.name} in the scene.");
            }

            var interaction = Instantiate(Prefab, cameraRigBlock.transform, true);
            interaction.SetActive(true);
            interaction.name = $"[BB] {BlockName}";
            BlocksUtils.UpdateForAutoWiring(interaction);

            return  new List<GameObject>() { interaction };
        }
    }
}
