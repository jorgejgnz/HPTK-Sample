using HandPhysicsToolkit.Helpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HandPhysicsToolkit.Input
{
    public class OVRSkeletonTracker : InputDataProvider
    {
        [Header("OVR specific")]
        public OVRHand handData;
        public OVRSkeleton boneData;

        public bool toLocalSpace = false;

        [Header("Scale estimation")]
        public bool autoScale = true;
        [Range(-1.5f,1.5f)]
        public float scaleOffset = 0.0f;

        int parent;

        public override void InitData()
        {
            base.InitData();
        }

        public override void UpdateData()
        {
            base.UpdateData();

            if (!handData || !boneData)
            {
                log = "No hand/bone data!";
                FindHandData(side);
                return;
            }

            log = "Updating from hand tracking...";

            if (handData.IsTracked)
            {
                if (toLocalSpace)
                {
                    // Get abstract transforms in local space (by default from OVRSkeleton)
                    for (int b = 0; b < bones.Length; b++)
                    {
                        parent = GetParent(b);

                        if (parent >= 0)
                        {
                            bones[b].space = Space.Self;
                            bones[b].position = boneData.Bones[b].Transform.localPosition;
                            bones[b].rotation = boneData.Bones[b].Transform.localRotation;
                        }
                        else // Wrist and forearm
                        {
                            bones[b].space = Space.World;
                            bones[b].position = boneData.Bones[b].Transform.position;
                            bones[b].rotation = boneData.Bones[b].Transform.rotation;
                        }
                    }
                }
                else
                {
                    // Get abstract transforms in world space (by default from OVRSkeleton)
                    for (int b = 0; b < bones.Length; b++)
                    {
                        bones[b].space = Space.World;
                        bones[b].position = boneData.Bones[b].Transform.position;
                        bones[b].rotation = boneData.Bones[b].Transform.rotation;
                    }
                }

                UpdateFingerPosesFromBones();

                // Confidence estimation
                confidence = (OVRConfidenceToLerp(handData.HandConfidence) + GetMeanFingerConfidence()) / 2.0f;

                // Hand scale estimation
                if (autoScale)
                    scale = handData.HandScale + scaleOffset;
            }
            else
            {
                confidence = 0.0f;
            }

            log = "Updated from hand tracking!";
        }

        float GetMeanFingerConfidence()
        {
            float sumFingerConfidence = 0.0f;

            sumFingerConfidence += OVRConfidenceToLerp(handData.GetFingerConfidence(OVRHand.HandFinger.Thumb));
            sumFingerConfidence += OVRConfidenceToLerp(handData.GetFingerConfidence(OVRHand.HandFinger.Index));
            sumFingerConfidence += OVRConfidenceToLerp(handData.GetFingerConfidence(OVRHand.HandFinger.Middle));
            sumFingerConfidence += OVRConfidenceToLerp(handData.GetFingerConfidence(OVRHand.HandFinger.Ring));
            sumFingerConfidence += OVRConfidenceToLerp(handData.GetFingerConfidence(OVRHand.HandFinger.Pinky));

            return sumFingerConfidence / 5.0f;
        }

        public static float OVRConfidenceToLerp(OVRHand.TrackingConfidence confidence)
        {
            switch(confidence)
            {
                case OVRHand.TrackingConfidence.High:
                    return 1.0f;
                case OVRHand.TrackingConfidence.Low:
                    return 0.0f;
                default:
                    return 0.0f;
            }
        }

        int GetParent(int boneIndex)
        {
            /*
            * 0 - wrist
            * 1 - forearm
            * 
            * 2 - thumb0
            * 3 - thumb1
            * 4 - thumb2
            * 5 - thumb3
            * 
            * 6 - index1
            * 7 - index2
            * 8 - index3
            * 
            * 9 - middle1
            * 10 - middle2
            * 11 - middle3
            * 
            * 12 - ring1
            * 13 - ring2
            * 14 - ring3
            * 
            * 15 - pinky0
            * 16 - pinky1
            * 17 - pinky2
            * 18 - pinky3
            */

            if (boneIndex == 2 ||
                boneIndex == 6 ||
                boneIndex == 9 ||
                boneIndex == 12 ||
                boneIndex == 15)
                return 0;

            if ((boneIndex > 2 && boneIndex <= 5) ||
                (boneIndex > 6 && boneIndex <= 8) ||
                (boneIndex > 9 && boneIndex <= 11) ||
                (boneIndex > 12 && boneIndex <= 14) ||
                (boneIndex > 15 && boneIndex <= 18))
                return boneIndex - 1;

            // For wrist and forearm
            return -1;
        }

        void FindHandData(Side side)
        {
            // Camera
            OVRCameraRig cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();

            if (!cameraRig) return;

            if (!HPTK.core.trackingSpace) HPTK.core.trackingSpace = cameraRig.trackingSpace;
            if (!HPTK.core.trackedCamera) HPTK.core.trackedCamera = cameraRig.trackingSpace.Find("CenterEyeAnchor").transform;

            // Hands
            OVRHand[] ovrHands = cameraRig.GetComponentsInChildren<OVRHand>();

            foreach (var ovrHand in ovrHands)
            {
                OVRSkeleton.IOVRSkeletonDataProvider skeltonDataProvider = ovrHand as OVRSkeleton.IOVRSkeletonDataProvider;
                OVRSkeleton.SkeletonType skeletonType = skeltonDataProvider.GetSkeletonType();

                OVRSkeleton ovrSkelton = ovrHand.GetComponent<OVRSkeleton>();
                if (ovrSkelton == null) continue;

                if ((skeletonType == OVRSkeleton.SkeletonType.HandLeft && side == Side.Left) ||
                    (skeletonType == OVRSkeleton.SkeletonType.HandRight && side == Side.Right))
                {
                    handData = ovrHand;
                    boneData = ovrSkelton;
                }
                else continue;
            }
        }
    }
}
