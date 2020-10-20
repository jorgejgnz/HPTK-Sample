using HPTK;
using HPTK.Helpers;
using HPTK.Input;
using HPTK.Models.Avatar;
using HPTK.Views.Handlers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class OVRFinder : MonoBehaviour
{
    public AvatarHandler avatar;

    [Range(0.0f,5.0f)]
    public float waitFor = 3.0f;

    [Header("InputDataProviders")]
    public bool findOVRHandsForIdps = false;
    public OVRSkeletonTracker ovrSkeletonTrackerL;
    public OVRSkeletonTracker ovrSkeletonTrackerR;

    [Header("HPTK Slave SMR")]
    public bool copyDefaultSMRMaterial = false;

    [Header("Default SMR")]
    public bool disableDefaultSMR = true;
    public bool applyMatToDefaultSMR = false;
    public Material matToApply;

    [Header("Parent")]
    public bool trackingSpaceIsParent = false;
    public Transform rootHPTK;

    [Header("Layer")]
    public bool applyLayerUnderRoot = true;
    public string layerToApply = "HPTK";

    [Header("Events")]
    public UnityEvent onReady;

    // Private
    private bool ready = false;
    SkinnedMeshRenderer defaultSMR_L;
    SkinnedMeshRenderer defaultSMR_R;

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

                        defaultSMR_L = smr;

                        // Copy materials from origin
                        if (copyDefaultSMRMaterial)
                            avatar.viewModel.leftHand.viewModel.slave.skinnedMR.material = defaultSMR_L.material;

                        break;

                    case OVRSkeleton.SkeletonType.HandRight:

                        if (!ovrSkeletonTrackerR.handData || !ovrSkeletonTrackerR.boneData)
                        {
                            ovrSkeletonTrackerR.handData = ovrHand;
                            ovrSkeletonTrackerR.boneData = ovrSkelton;
                        }

                        defaultSMR_R = smr;

                        // Copy materials from origin
                        if (copyDefaultSMRMaterial)
                            avatar.viewModel.rightHand.viewModel.slave.skinnedMR.material = defaultSMR_R.material;

                        break;
                }
            }

            // Apply materials to origin
            if (applyMatToDefaultSMR)
                smr.material = matToApply;
        }

        ready = true;
        onReady.Invoke();
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
