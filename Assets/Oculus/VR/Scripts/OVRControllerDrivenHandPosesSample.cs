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
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

/// <summary>
/// Simple script for running the ControllerDrivenHandPosesSample
/// </summary>
[DisallowMultipleComponent]
[HelpURL("https://developer.oculus.com/reference/unity/latest/class_o_v_r_controller_driven_hand_poses_sample")]
public class OVRControllerDrivenHandPosesSample : MonoBehaviour
{
    [SerializeField]
    private Button buttonOff;
    [SerializeField]
    private Button buttonConforming;
    [SerializeField]
    private Button buttonNatural;
    [SerializeField]
    private LineRenderer leftLinePointer;
    [SerializeField]
    private LineRenderer rightLinePointer;

    public OVRCameraRig cameraRig;

    // Unity event functions
    void Awake()
    {
        switch (OVRManager.instance.controllerDrivenHandPosesType)
        {
            case OVRManager.ControllerDrivenHandPosesType.None:
                SetControllerDrivenHandPosesTypeToNone();
                break;
            case OVRManager.ControllerDrivenHandPosesType.ConformingToController:
                SetControllerDrivenHandPosesTypeToControllerConforming();
                break;
            case OVRManager.ControllerDrivenHandPosesType.Natural:
                SetControllerDrivenHandPosesTypeToNatural();
                break;

        }
    }

    void OnDestroy()
    {

    }

    void OnEnable()
    {

    }

    void OnDisable()
    {

    }


    private void Update()
    {
        UpdateLineRenderer();
    }

    private void UpdateLineRenderer()
    {
        leftLinePointer.enabled = false;
        rightLinePointer.enabled = false;

        UpdateLineRendererForHand(false);
        UpdateLineRendererForHand(true);
    }

    private void UpdateLineRendererForHand(bool isLeft)
    {
        Transform inputTransform = null;

        if (isLeft)
        {
            if (OVRInput.IsControllerConnected(OVRInput.Controller.LTouch))
            {
                inputTransform = cameraRig.leftControllerAnchor;
            }
        }
        else
        {
            if (OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
            {
                inputTransform = cameraRig.rightControllerAnchor;
            }
        }
        if (inputTransform == null)
        {
            return;
        }

        var inputPosition = inputTransform.position;

        LineRenderer linePointer = (isLeft) ? leftLinePointer : rightLinePointer;
        var ray = new Ray(inputPosition, inputTransform.rotation * Vector3.forward);

        linePointer.enabled = true;
        linePointer.SetPosition(0, inputTransform.position + ray.direction * 0.05f);
        linePointer.SetPosition(1, inputPosition + ray.direction * 2.5f);
    }

    public void SetControllerDrivenHandPosesTypeToNone()
    {
        OVRManager.instance.controllerDrivenHandPosesType = OVRManager.ControllerDrivenHandPosesType.None;
        buttonOff.interactable = false;
        buttonConforming.interactable = true;
        buttonNatural.interactable = true;
    }

    public void SetControllerDrivenHandPosesTypeToControllerConforming()
    {
        OVRManager.instance.controllerDrivenHandPosesType = OVRManager.ControllerDrivenHandPosesType.ConformingToController;
        buttonOff.interactable = true;
        buttonConforming.interactable = false;
        buttonNatural.interactable = true;
    }

    public void SetControllerDrivenHandPosesTypeToNatural()
    {
        OVRManager.instance.controllerDrivenHandPosesType = OVRManager.ControllerDrivenHandPosesType.Natural;
        buttonOff.interactable = true;
        buttonConforming.interactable = true;
        buttonNatural.interactable = false;
    }
}
