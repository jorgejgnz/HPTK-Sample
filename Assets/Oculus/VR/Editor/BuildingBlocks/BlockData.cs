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
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Meta.XR.BuildingBlocks.Editor
{
    public class BlockData : BlockBaseData
    {
        [SerializeField] internal GameObject prefab;
        public GameObject Prefab => prefab;

        [SerializeField] internal List<string> dependencies;

        public IReadOnlyList<BlockData> Dependencies =>
            dependencies?.Select(Utils.GetBlockData).Where(dep => dep != null).ToList()
            ?? EmptyBlockList;

        private static readonly IReadOnlyList<BlockData> EmptyBlockList = new List<BlockData>();

        [Tooltip("Indicates whether only one instance of this block can be installed per scene.")] [SerializeField]
        internal bool isSingleton;

        public bool IsSingleton => isSingleton;

        [ContextMenu("Validate")]
        internal override void Validate()
        {
            base.Validate();

            Assert.NotNull(Prefab, $"{nameof(Prefab)} cannot be null");
            Assert.IsNull(Prefab.GetComponent<BuildingBlock>(),
                $"{nameof(Prefab)} must not contain the {nameof(BuildingBlock)} component, it'll be added dynamically");
            Assert.IsFalse(HasCyclicDependencies(), $"{nameof(Dependencies)} cannot have cycles");
            Assert.IsFalse(HasDuplicateDependencies(), $"{nameof(Dependencies)} cannot have duplicates");
        }

        internal override void AddToProject(Action onInstall = null)
        {
            InstallWithDependenciesAndCommit();
            onInstall?.Invoke();
        }

        [ContextMenu("Install")]
        private void InstallWithDependenciesAndCommit()
        {
            if (HasNonBBCameraRig())
            {
                if (!EditorUtility.DisplayDialog("Confirmation",
                        $"You already have a scene setup with OVRCameraRig that may not be compatible with {Utils.BlocksPublicName}. Do you want to proceed?", "Yes", "No"))
                {
                    return;
                }
            }

            Exception installException = null;
            try
            {
                var installedObjects = InstallWithDependencies();
                SaveScene();
                FixSetupRules();

                EditorApplication.delayCall += () => { Selection.objects = installedObjects.Cast<Object>().ToArray(); };
            } catch (Exception e) {
                installException = e;
                throw;
            } finally {
                OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.InstallBlockData)
                    .SetResult(installException == null ? OVRPlugin.Qpl.ResultType.Success : OVRPlugin.Qpl.ResultType.Fail)
                    .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.BlockId, Id)
                    .AddAnnotationIfNotNullOrEmpty(OVRTelemetryConstants.BB.AnnotationType.Error, installException?.Message)
                    .Send();
            }
        }

        internal static bool HasNonBBCameraRig()
        {
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            return cameraRig != null && cameraRig.GetComponent<BuildingBlock>() == null;
        }

        private static void FixSetupRules()
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            UpdateTasks(buildTargetGroup, FixTasks);
        }

        private static void FixTasks(OVRConfigurationTaskProcessor processor)
        {
            OVRProjectSetup.FixTasks(processor.BuildTargetGroup, tasks => tasks
                .Where(task =>
                    !task.IsDone(processor.BuildTargetGroup)
                    && !task.IsIgnored(processor.BuildTargetGroup)
                    && task.Level.GetValue(processor.BuildTargetGroup) == OVRProjectSetup.TaskLevel.Required)
                .ToList(), blocking: false, onCompleted: AfterFixApply);
        }

        private static void UpdateTasks(BuildTargetGroup buildTargetGroup,
            Action<OVRConfigurationTaskProcessor> onCompleted = null)
        {
            OVRProjectSetup.UpdateTasks(buildTargetGroup, logMessages: OVRProjectSetup.LogMessages.Disabled,
                blocking: false, onCompleted: onCompleted);
        }

        private static void AfterFixApply(OVRConfigurationTaskProcessor processor)
        {
            AssetDatabase.SaveAssets();
            UpdateTasks(processor.BuildTargetGroup);
        }

        internal override bool CanBeAdded => !HasMissingDependencies && !IsSingletonAndAlreadyPresent;
        private bool HasMissingDependencies => Dependencies.Any(dependency => dependency == null);
        private bool IsSingletonAndAlreadyPresent => IsSingleton && IsBlockPresentInScene(Id);

        internal List<GameObject> InstallWithDependencies()
        {
            if (IsSingletonAndAlreadyPresent)
            {
                throw new InvalidOperationException(
                    $"Block {BlockName} is a singleton and already present in the scene so it cannot be installed.");
            }

            if (HasMissingDependencies)
            {
                throw new InvalidOperationException($"A dependency of block {BlockName} is not present in the project.");
            }

            InstallDependencies(Dependencies);
            return Install();
        }

        internal List<GameObject> Install()
        {
            var spawnedObjects = InstallRoutine();

            foreach (var spawnedObject in spawnedObjects)
            {
                var block = spawnedObject.AddComponent<BuildingBlock>();
                block.blockId = Id;
                block.version = Version;
                while (UnityEditorInternal.ComponentUtility.MoveComponentUp(block))
                {
                }

                OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.AddBlock)
                    .AddBlockInfo(block)
                    .Send();
            }

            return spawnedObjects;
        }

        protected virtual List<GameObject> InstallRoutine()
        {
            var instance = Instantiate(Prefab, Vector3.zero, Quaternion.identity);
            instance.SetActive(true);
            instance.name = $"[BB] {BlockName}";
            return new List<GameObject> { instance };
        }

        private static void InstallDependencies(IEnumerable<BlockData> dependencies)
        {
            foreach (var dependency in dependencies)
            {
                if (IsBlockPresentInScene(dependency.Id))
                {
                    continue;
                }

                dependency.InstallWithDependencies();
            }
        }

        internal static bool IsBlockPresentInScene(string blockId)
        {
            return FindObjectsOfType<BuildingBlock>().Any(x => x.BlockId == blockId);
        }

        public bool IsBlockPresentInScene()
        {
            return IsBlockPresentInScene(Id);
        }

        internal bool IsUpdateAvailableForBlock(BuildingBlock block)
        {
            return Version > block.Version;
        }

        internal void UpdateBlockToLatestVersion(BuildingBlock block)
        {
            if (!IsUpdateAvailableForBlock(block))
            {
                throw new InvalidOperationException(
                    $"Block {BlockName} is already in the latest version.");
            }

            DestroyImmediate(block.gameObject);
            InstallWithDependenciesAndCommit();

            OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.UpdateBlock)
                .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.BlockId, Id)
                .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.Version, Version.ToString())
                .Send();
        }

        internal bool HasDuplicateDependencies()
        {
            var depSet = new HashSet<string>();
            return dependencies.Any(dependency => !depSet.Add(dependency));
        }

        internal bool HasCyclicDependencies()
        {
            var visited = new Dictionary<BlockData, bool>();
            var recStack = new Dictionary<BlockData, bool>();

            visited[this] = true;
            recStack[this] = true;

            foreach (var dependency in Dependencies)
            {
                if (IsCyclicUntil(dependency, visited, recStack))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsCyclicUntil(BlockData blockData,
            Dictionary<BlockData, bool> visited,
            Dictionary<BlockData, bool> recStack)
        {
            if (recStack.TryGetValue(blockData, out var hasRec) && hasRec)
            {
                return true;
            }

            if (visited.TryGetValue(blockData, out var hasVisited) && hasVisited)
            {
                return false;
            }

            visited[blockData] = true;
            recStack[blockData] = true;

            foreach (var dependency in blockData.Dependencies)
            {
                if (IsCyclicUntil(dependency, visited, recStack))
                {
                    return true;
                }
            }

            recStack[blockData] = false;
            return false;
        }

        private static void SaveScene()
        {
            if (!IsCurrentSceneSaved())
            {
                return;
            }

            var activeScene = SceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);
        }

        private static bool IsCurrentSceneSaved()
        {
            var scenePath = SceneManager.GetActiveScene().path;
            return !string.IsNullOrEmpty(scenePath);
        }
    }
}
