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

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

internal static class OVRMovementTool
{
	private const string k_SetupCharacterForBodyTracking = "Setup Character for Body Tracking";
	const string k_SetupCharacterForBodyTrackingMovementToolsMenuStr = "GameObject/Movement/" + k_SetupCharacterForBodyTracking;

	[MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr, true)]
	static bool ValidateSetupCharacterForBodyTracking()
	{
		return Selection.activeGameObject != null;
	}

	[MenuItem(k_SetupCharacterForBodyTrackingMovementToolsMenuStr)]
	private static void SetupCharacterForBodyTracking()
	{
		Undo.IncrementCurrentGroup();
		var gameObject = Selection.activeGameObject;

		var body = gameObject.GetComponent<OVRBody>();
		if (!body)
		{
			body = gameObject.AddComponent<OVRBody>();
			Undo.RegisterCreatedObjectUndo(body, "Create OVRBody component");
		}

		var skeleton = gameObject.GetComponent<OVRCustomSkeleton>();
		if (!skeleton)
		{
			skeleton = gameObject.AddComponent<OVRCustomSkeleton>();
			Undo.RegisterCreatedObjectUndo(skeleton, "Create OVRCustomSkeleton component");
		}

		Undo.RegisterFullObjectHierarchyUndo(skeleton, "Auto-map OVRCustomSkeleton bones");
		skeleton.SetSkeletonType(OVRSkeleton.SkeletonType.Body);
		skeleton.AutoMapBones(OVRCustomSkeleton.RetargetingType.OculusSkeleton);
		EditorUtility.SetDirty(skeleton);
		EditorSceneManager.MarkSceneDirty(skeleton.gameObject.scene);

		Undo.SetCurrentGroupName(k_SetupCharacterForBodyTracking);
	}
}
