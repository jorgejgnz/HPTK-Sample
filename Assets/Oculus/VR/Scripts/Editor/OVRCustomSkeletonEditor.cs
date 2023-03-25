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
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using BoneId = OVRSkeleton.BoneId;

[CustomEditor(typeof(OVRCustomSkeleton))]
public class OVRCustomSkeletonEditor : Editor
{
	public override void OnInspectorGUI()
	{
		DrawPropertiesExcluding(serializedObject, new string[] { "_customBones" });
		serializedObject.ApplyModifiedProperties();

		var skeleton = (OVRCustomSkeleton)target;

		if (skeleton.GetSkeletonType() == OVRSkeleton.SkeletonType.None)
		{
			EditorGUILayout.HelpBox("Please select a SkeletonType.", MessageType.Warning);
		}
		else
		{
			DrawBonesMapping(skeleton);
		}
	}

	private void DrawBonesMapping(OVRCustomSkeleton skeleton)
	{
		var enumValues = Enum.GetNames(typeof(OVRCustomSkeleton.RetargetingType));
		skeleton.retargetingType = (OVRCustomSkeleton.RetargetingType)
			EditorGUILayout.Popup("Custom skeleton structure", (int) skeleton.retargetingType, enumValues);

		if (GUILayout.Button($"Auto Map Bones ({enumValues[(int) skeleton.retargetingType]})"))
		{
			skeleton.AutoMapBones(skeleton.retargetingType);
			EditorUtility.SetDirty(skeleton);
			EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);
		}

		EditorGUILayout.LabelField("Bones", EditorStyles.boldLabel);
		var start = skeleton.GetCurrentStartBoneId();
		var end = skeleton.GetCurrentEndBoneId();
		if (start != BoneId.Invalid && end != BoneId.Invalid)
		{
			for (var i = (int)start; i < (int)end; ++i)
			{
				var boneName = OVRSkeleton.BoneLabelFromBoneId(skeleton.GetSkeletonType(), (BoneId)i);
				skeleton.CustomBones[i] =
					(Transform)EditorGUILayout.ObjectField(boneName, skeleton.CustomBones[i], typeof(Transform), true);
			}
		}
	}
}

/// <summary>
/// Extensions class for the editor methods of <see cref="OVRCustomSkeleton"/>.
/// </summary>
public static class OVRCustomSkeletonEditorExtensions
{
	/// <summary>
	/// This method tries to retarget the skeleton structure present in the current <see cref="GameObject"/> to the one supported by the body tracking system.
	/// </summary>
	/// <param name="customSkeleton" cref="OVRCustomSkeleton">The custom skeleton to run this method on</param>
	/// <param name="type" cref="OVRCustomSkeleton.RetargetingType">The skeleton structure to auto map from</param>
	public static void AutoMapBones(this OVRCustomSkeleton customSkeleton, OVRCustomSkeleton.RetargetingType type)
	{
		try
		{
			switch (type)
			{
				case OVRCustomSkeleton.RetargetingType.OculusSkeleton:
					customSkeleton.AutoMapBonesFromOculusSkeleton();
					break;
				default:
					throw new InvalidEnumArgumentException($"Invalid {nameof(OVRCustomSkeleton.RetargetingType)}");
			}
		}
		catch (Exception e)
		{
			EditorUtility.DisplayDialog($"Auto Map Bones Error", e.Message, "Ok");
		}
	}

	public static void TryAutoMapBonesByName(this OVRCustomSkeleton customSkeleton)
	{
		customSkeleton.AutoMapBonesFromOculusSkeleton();
	}

	internal static void AutoMapBonesFromOculusSkeleton(this OVRCustomSkeleton customSkeleton)
	{
		var start = customSkeleton.GetCurrentStartBoneId();
		var end = customSkeleton.GetCurrentEndBoneId();
		var skeletonType = customSkeleton.GetSkeletonType();
		if (start != BoneId.Invalid && end != BoneId.Invalid)
		{
			for (var bi = (int)start; bi < (int)end; ++bi)
			{
				string fbxBoneName = FbxBoneNameFromBoneId(skeletonType, (BoneId)bi);
				Transform t = customSkeleton.transform.FindChildRecursive(fbxBoneName);

				if (t == null && skeletonType == OVRSkeleton.SkeletonType.Body)
				{
					var legacyBoneName = fbxBoneName
						.Replace("Little", "Pinky")
						.Replace("Metacarpal", "Meta");
					t = customSkeleton.transform.FindChildRecursive(legacyBoneName);
				}

				if (t != null)
				{
					customSkeleton.CustomBones[bi] = t;
				}
			}
		}
	}

	private static string FbxBoneNameFromBoneId(OVRSkeleton.SkeletonType skeletonType, BoneId bi)
	{
		if (skeletonType == OVRSkeleton.SkeletonType.Body)
		{
			return FBXBodyBoneNames[(int)bi];
		}
		else
		{
			if (bi >= BoneId.Hand_ThumbTip && bi <= BoneId.Hand_PinkyTip)
			{
				return FBXHandSidePrefix[(int)skeletonType] + FBXHandFingerNames[(int)bi - (int)BoneId.Hand_ThumbTip] + "_finger_tip_marker";
			}
			else
			{
				return FBXHandBonePrefix + FBXHandSidePrefix[(int)skeletonType] + FBXHandBoneNames[(int)bi];
			}
		}
	}

	private static readonly string[] FBXBodyBoneNames =
	{
		"Root",
		"Hips",
		"SpineLower",
		"SpineMiddle",
		"SpineUpper",
		"Chest",
		"Neck",
		"Head",
		"LeftShoulder",
		"LeftScapula",
		"LeftArmUpper",
		"LeftArmLower",
		"LeftHandWristTwist",
		"RightShoulder",
		"RightScapula",
		"RightArmUpper",
		"RightArmLower",
		"RightHandWristTwist",
		"LeftHandPalm",
		"LeftHandWrist",
		"LeftHandThumbMetacarpal",
		"LeftHandThumbProximal",
		"LeftHandThumbDistal",
		"LeftHandThumbTip",
		"LeftHandIndexMetacarpal",
		"LeftHandIndexProximal",
		"LeftHandIndexIntermediate",
		"LeftHandIndexDistal",
		"LeftHandIndexTip",
		"LeftHandMiddleMetacarpal",
		"LeftHandMiddleProximal",
		"LeftHandMiddleIntermediate",
		"LeftHandMiddleDistal",
		"LeftHandMiddleTip",
		"LeftHandRingMetacarpal",
		"LeftHandRingProximal",
		"LeftHandRingIntermediate",
		"LeftHandRingDistal",
		"LeftHandRingTip",
		"LeftHandLittleMetacarpal",
		"LeftHandLittleProximal",
		"LeftHandLittleIntermediate",
		"LeftHandLittleDistal",
		"LeftHandLittleTip",
		"RightHandPalm",
		"RightHandWrist",
		"RightHandThumbMetacarpal",
		"RightHandThumbProximal",
		"RightHandThumbDistal",
		"RightHandThumbTip",
		"RightHandIndexMetacarpal",
		"RightHandIndexProximal",
		"RightHandIndexIntermediate",
		"RightHandIndexDistal",
		"RightHandIndexTip",
		"RightHandMiddleMetacarpal",
		"RightHandMiddleProximal",
		"RightHandMiddleIntermediate",
		"RightHandMiddleDistal",
		"RightHandMiddleTip",
		"RightHandRingMetacarpal",
		"RightHandRingProximal",
		"RightHandRingIntermediate",
		"RightHandRingDistal",
		"RightHandRingTip",
		"RightHandLittleMetacarpal",
		"RightHandLittleProximal",
		"RightHandLittleIntermediate",
		"RightHandLittleDistal",
		"RightHandLittleTip"
	};

	private static readonly string[] FBXHandSidePrefix = { "l_", "r_" };
	private const string FBXHandBonePrefix = "b_";

	private static readonly string[] FBXHandBoneNames =
	{
		"wrist",
		"forearm_stub",
		"thumb0",
		"thumb1",
		"thumb2",
		"thumb3",
		"index1",
		"index2",
		"index3",
		"middle1",
		"middle2",
		"middle3",
		"ring1",
		"ring2",
		"ring3",
		"pinky0",
		"pinky1",
		"pinky2",
		"pinky3"
	};

	private static readonly string[] FBXHandFingerNames =
	{
		"thumb",
		"index",
		"middle",
		"ring",
		"pinky"
	};
}
