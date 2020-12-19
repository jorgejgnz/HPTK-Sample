using HPTK.Models.Avatar;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HPTK.Input
{
    public class OVRSkeletonTracker : InputDataProvider
    {
        [Header("OVR specific")]
        public OVRHand handData;
        public OVRSkeleton boneData;

        public bool toLocalSpace = false;

        [Range(-0.25f,0.25f)]
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
                return;

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

                UpdateFingers();

                // Confidence estimation
                confidence = (OVRConfidenceToLerp(handData.HandConfidence) + GetMeanFingerConfidence()) / 2.0f;

                // Hand scaling
                scale = handData.HandScale + scaleOffset;
            }
            else
            {
                confidence = 0.0f;
            }
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
            * 
            * 19 - thumbTip
            * 20 - indexTip
            * 21 - middleTip
            * 22 - ringTip
            * 23 - pinkyTip
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

            if (boneIndex == 19)
                return 5;

            if (boneIndex == 20)
                return 8;

            if (boneIndex == 21)
                return 11;

            if (boneIndex == 22)
                return 14;

            if (boneIndex == 23)
                return 18;

            // For wrist and forearm
            return -1;
        }

    }
}
