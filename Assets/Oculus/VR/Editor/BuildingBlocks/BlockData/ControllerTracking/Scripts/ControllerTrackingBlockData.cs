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

namespace Meta.XR.BuildingBlocks.Editor
{
    public class ControllerTrackingBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine()
        {
            var cameraRig = OVRProjectSetupUtils.FindComponentInScene<OVRCameraRig>();
            if (cameraRig == null)
            {
                throw new InvalidOperationException(
                    "The Controller Tracking Building Block cannot be installed without a camera rig present in the scene.");
            }

            var leftController = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            leftController.SetActive(true);
            leftController.name = $"[BB] {BlockName} left";
            leftController.transform.parent = cameraRig.leftControllerAnchor;
            leftController.GetComponent<OVRControllerHelper>().m_controller = OVRInput.Controller.LTouch;

            var rightController = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            rightController.SetActive(true);
            rightController.name = $"[BB] {BlockName} right";
            rightController.transform.parent = cameraRig.rightControllerAnchor;
            rightController.GetComponent<OVRControllerHelper>().m_controller = OVRInput.Controller.RTouch;

            return new List<GameObject> {leftController, rightController};
        }
    }
}
