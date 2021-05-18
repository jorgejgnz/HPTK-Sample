using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HandPhysicsToolkit.Utils
{
    public class OculusTrackingMockup : MonoBehaviour
    {
        public bool setInAwake = true;
        public Transform cameraMockup;
        public Transform trackingSpaceMockup;

        [Header("Read Only")]
        [ReadOnly]
        public bool validTrackingRefs = false;

        private void Awake()
        {
            if (setInAwake)
            {
                HPTK.core.trackedCamera = cameraMockup;
                HPTK.core.trackingSpace = trackingSpaceMockup;
            }
        }

        private void Update()
        {
            validTrackingRefs = ValidTrackingRefs();
            if (!validTrackingRefs) FindTrackingRefs();
        }

        void FindTrackingRefs()
        {
            // Camera
            OVRCameraRig cameraRig = GameObject.FindObjectOfType<OVRCameraRig>();

            if (!cameraRig) return;

            HPTK.core.trackingSpace = cameraRig.trackingSpace;
            HPTK.core.trackedCamera = cameraRig.trackingSpace.Find("CenterEyeAnchor").transform;

            Destroy(cameraMockup.gameObject);
            Destroy(trackingSpaceMockup.gameObject);
            Destroy(this);
        }

        bool ValidTrackingRefs()
        {
            if (!HPTK.core.trackingSpace || !HPTK.core.trackedCamera) return false;
            if (HPTK.core.trackingSpace == trackingSpaceMockup || HPTK.core.trackedCamera == cameraMockup) return false;
            return true;
        }
    }
}
