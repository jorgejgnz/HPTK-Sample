using HPTK;
using HPTK.Helpers;
using HPTK.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OVRFinder : MonoBehaviour
{
    [Range(0.0f,5.0f)]
    public float waitFor = 3.0f;

    [Header("InputDataProviders")]
    public bool findOVRHandsForIdps = false;
    public OVRSkeletonTracker ovrSkeletonTrackerL;
    public OVRSkeletonTracker ovrSkeletonTrackerR;

    [Header("Default SMR")]
    public bool disableDefaultSMR = true;
    public bool applyMatToDefaultSMR = true;
    public Material matToApply;

    [Header("Parent")]
    public bool trackingSpaceIsParent = false;
    public Transform rootHPTK;

    [Header("Layer")]
    public bool applyLayerUnderRoot = true;
    public string layerToApply = "HPTK";

    private void Start()
    {
        StartCoroutine(PhysHelpers.DoAfter(waitFor,() => { Find(); }));
    }

    void Find()
    {
        // Camera
        OVRCameraRig cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();
        HPTKCore.core.model.trackedCamera = cameraRig.trackingSpace.Find("CenterEyeAnchor").transform;

        // Reparenting
        if (!rootHPTK)
            rootHPTK = transform;

        if (trackingSpaceIsParent)
            rootHPTK.parent = cameraRig.trackingSpace;

        // Hands
        OVRHand[] ovrHands = cameraRig.GetComponentsInChildren<OVRHand>();

        foreach (var ovrHand in ovrHands)
        {
            OVRSkeleton.IOVRSkeletonDataProvider skeltonDataProvider = ovrHand as OVRSkeleton.IOVRSkeletonDataProvider;
            OVRSkeleton.SkeletonType skeltonType = skeltonDataProvider.GetSkeletonType();

            // SMR
            SkinnedMeshRenderer smr = ovrHand.GetComponent<SkinnedMeshRenderer>();

            if (disableDefaultSMR)
                smr.enabled = false;

            if (applyMatToDefaultSMR)
                smr.material = matToApply;

            OVRSkeleton ovrSkelton = ovrHand.GetComponent<OVRSkeleton>();
            if (ovrSkelton == null)
            {
                continue;
            }

            // Fix for static hands in Unity Editor
#if UNITY_EDITOR
            ovrSkelton.ShouldUpdateBonePoses = true;
#endif

            // IDPs
            if (findOVRHandsForIdps)
            {
                switch (skeltonType)
                {
                    case OVRSkeleton.SkeletonType.HandLeft:
                        if (!ovrSkeletonTrackerL.handData || !ovrSkeletonTrackerL.boneData)
                        {
                            ovrSkeletonTrackerL.handData = ovrHand;
                            ovrSkeletonTrackerL.boneData = ovrSkelton;
                        }
                        break;
                    case OVRSkeleton.SkeletonType.HandRight:
                        if (!ovrSkeletonTrackerR.handData || !ovrSkeletonTrackerR.boneData)
                        {
                            ovrSkeletonTrackerR.handData = ovrHand;
                            ovrSkeletonTrackerR.boneData = ovrSkelton;
                        }
                        break;
                }
            }
        }
    }

    public static void ApplyLayerRecursively(Transform root, int layer)
    {
        Transform[] children = root.GetComponentsInChildren<Transform>();

        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.layer = layer;
        }
    }
}
