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
    public class HandTrackingBlockData : BlockData
    {
        protected override List<GameObject> InstallRoutine()
        {
            var cameraRig = OVRProjectSetupUtils.FindComponentInScene<OVRCameraRig>();
            if (cameraRig == null)
            {
                throw new InvalidOperationException(
                    "The Hand Tracking BB cannot be installed without a camera rig present in the scene.");
            }

            var leftHand = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            leftHand.SetActive(true);
            leftHand.name = $"[BB] {BlockName} left";
            leftHand.transform.parent = cameraRig.leftHandAnchor;

            leftHand.GetComponent<OVRHand>().HandType = OVRHand.Hand.HandLeft;
            leftHand.GetComponent<OVRSkeleton>().SetSkeletonType(OVRSkeleton.SkeletonType.HandLeft);
            leftHand.GetComponent<OVRMesh>().SetMeshType(OVRMesh.MeshType.HandLeft);

            var rightHand = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            rightHand.SetActive(true);
            rightHand.name = $"[BB] {BlockName} right";
            rightHand.transform.parent = cameraRig.rightHandAnchor;

            rightHand.GetComponent<OVRHand>().HandType = OVRHand.Hand.HandRight;
            rightHand.GetComponent<OVRSkeleton>().SetSkeletonType(OVRSkeleton.SkeletonType.HandRight);
            rightHand.GetComponent<OVRMesh>().SetMeshType(OVRMesh.MeshType.HandRight);

            return new List<GameObject> {leftHand, rightHand};
        }
    }
}
