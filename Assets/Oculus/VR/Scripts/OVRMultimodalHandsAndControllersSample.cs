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
/// Simple script for running the MultimodalHandsAndControllers Sample
/// </summary>
[DisallowMultipleComponent]
public class OVRMultimodalHandsAndControllersSample : MonoBehaviour
{
    [SerializeField]
    private Button enableButton;
    [SerializeField]
    private Button disableButton;
    [SerializeField]
    public Text displayText;

    // Unity event functions
    void Awake()
    {
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
        displayText.text = OVRInput.GetActiveController().ToString();
    }

    public void EnableMultiModality()
    {
        OVRInput.EnableSimultaneousHandsAndControllers();
        enableButton.interactable = false;
        disableButton.interactable = true;
    }

    public void DisableMultiModality()
    {
        OVRInput.DisableSimultaneousHandsAndControllers();
        enableButton.interactable = true;
        disableButton.interactable = false;
    }
}
