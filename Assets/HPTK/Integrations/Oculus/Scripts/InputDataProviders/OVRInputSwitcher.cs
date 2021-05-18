using HandPhysicsToolkit.Input;
using HandPhysicsToolkit.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace HandPhysicsToolkit.Input
{
    public class OVRInputSwitcher : InputDataProvider
    {
        [Header("Input Data Providers")]
        public OVRSkeletonTracker idpForHands;
        public UnityXRControllerTracker idpForControllers;

        [Header("Events")]
        public UnityEvent onHands;
        public UnityEvent onControllers;

        [Header("Read Only")]
        [ReadOnly]
        public OVRInput.Controller currentConnectedController;
        [ReadOnly]
        public InputDataProvider currentIdp;

        private void Update()
        {
            if (OVRInput.GetConnectedControllers() != currentConnectedController)
            {
                currentConnectedController = OVRInput.GetConnectedControllers();

                if (currentConnectedController.HasFlag(OVRInput.Controller.Hands))
                    onHands.Invoke();
                else if (currentConnectedController.HasFlag(OVRInput.Controller.Touch))
                    onControllers.Invoke();
            }
        }

        public override void InitData()
        {
            base.InitData();

            idpForHands.InitData();
            idpForControllers.InitData();
        }

        public override void UpdateData()
        {
            base.UpdateData();

            if (currentConnectedController.HasFlag(OVRInput.Controller.Hands))
            {
                log = "Using hands! :D";
                currentIdp = idpForHands;
            }
            else if (currentConnectedController.HasFlag(OVRInput.Controller.Touch))
            {
                log = "Using controllers! :)";
                currentIdp = idpForControllers;
            }
            else
            {
                log = "Using nothing :/";
                currentIdp = null;
            }

            if (currentIdp != null)
            {
                currentIdp .UpdateData(); // It calls internally to base.UpdateData();

                bones = currentIdp.bones;

                thumb = currentIdp.thumb;
                index = currentIdp.index;
                middle = currentIdp.middle;
                ring = currentIdp.ring;
                pinky = currentIdp.pinky;

                confidence = currentIdp.confidence;
                scale = currentIdp.scale;
            }
        }
    }
}
