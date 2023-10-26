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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class OVRProjectSetupUtils
{
    public static T FindComponentInScene<T>() where T : Component
    {
        var scene = SceneManager.GetActiveScene();
        var rootGameObjects = scene.GetRootGameObjects();
        return rootGameObjects.FirstOrDefault(go => go.GetComponentInChildren<T>())?.GetComponentInChildren<T>();
    }

    public static List<T> FindComponentsInScene<T>() where T : Component
    {
        var activeScene = SceneManager.GetActiveScene();
        var foundComponents = new List<T>();

        var rootObjects = activeScene.GetRootGameObjects();
        foreach (var rootObject in rootObjects)
        {
            var components = rootObject.GetComponentsInChildren<T>(true);
            foundComponents.AddRange(components);
        }

        return foundComponents;
    }

    public static bool HasComponentInParents<T>(GameObject obj) where T : Component
    {
        var currentTransform = obj.transform;

        while (currentTransform != null)
        {
            if (currentTransform.GetComponent<T>() != null)
            {
                return true;
            }
            currentTransform = currentTransform.parent;
        }

        return false;
    }

    public static T FindScriptableObjectInProject<T>() where T : ScriptableObject
    {
        var guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);

        if (guids.Length == 0)
        {
            return null;
        }

        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<T>(path);
    }



    private static ListRequest _packageManagerListRequest;

    static OVRProjectSetupUtils()
    {
        RefreshPackageList(false);
        OVRGUIContent.RegisterContentPath(OVRGUIContent.Source.ProjectSetupToolIcons, "OVRProjectSetup/Icons");
    }

    public static bool PackageManagerListAvailable => _packageManagerListRequest.Status == StatusCode.Success;

    public static bool IsPackageInstalled(string packageName) =>
        PackageManagerListAvailable &&
        (_packageManagerListRequest.Result?.Any(package => package.name == packageName) ?? false);

    public static bool RefreshPackageList(bool blocking)
    {
        _packageManagerListRequest = Client.List(offlineMode: false, includeIndirectDependencies: true);
        if (blocking)
        {
            while (!PackageManagerListAvailable)
            {
                Thread.Sleep(100);
            }
        }

        return PackageManagerListAvailable;
    }

    public static bool InstallPackage(string packageName)
    {
        var request = Client.Add(packageName);

        // TODO: make this async later
        while (!request.IsCompleted)
        {
            Thread.Sleep(100);
        }

        // Refresh the Client list
        RefreshPackageList(false);

        return request.Status == StatusCode.Success;
    }

    public static bool UninstallPackage(string packageName)
    {
        var request = Client.Remove(packageName);

        // TODO: make this async later
        while (!request.IsCompleted)
        {
            Thread.Sleep(1);
        }

        // Refresh the Client list
        RefreshPackageList(false);

        return request.Status == StatusCode.Success;
    }

    public static BuildTarget GetBuildTarget(this BuildTargetGroup buildTargetGroup)
    {
        // It is a bit tricky to get the build target from the build target group
        // because of some additional variations on build targets that the build target group doesn't know about
        // This function aims at offering an approximation of the build target, but it's not guaranteed
        return buildTargetGroup switch
        {
            BuildTargetGroup.Android => BuildTarget.Android,
            BuildTargetGroup.Standalone => BuildTarget.StandaloneWindows64,
            _ => BuildTarget.NoTarget
        };
    }
}
