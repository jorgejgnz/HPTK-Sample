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
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    [InitializeOnLoad]
    public static class Utils
    {
        internal const string BlocksPublicName = "Building Blocks";
        internal const string BlockPublicName = "Building Block";

        internal static readonly OVRGUIContent StatusIcon = OVREditorUtils.CreateContent("ovr_icon_bbw.png", OVRGUIContent.Source.BuildingBlocksIcons, $"Open {BlocksPublicName}");

        internal static readonly OVRGUIContent GotoIcon = OVREditorUtils.CreateContent("ovr_icon_link.png", OVRGUIContent.Source.BuildingBlocksIcons, "Select Block");

        internal static readonly OVRGUIContent AddIcon = OVREditorUtils.CreateContent("ovr_icon_addblock.png", OVRGUIContent.Source.BuildingBlocksIcons, "Add Block to current scene");

        internal static readonly Color AccentColor = OVREditorUtils.HexToColor("#a29de5");

        private static readonly Dictionary<string, BlockData> _idToBlockDataDictionary =
            new Dictionary<string, BlockData>();

        private static bool _dirty = true;


        static Utils()
        {
            OVRGUIContent.RegisterContentPath(OVRGUIContent.Source.BuildingBlocksIcons, "BuildingBlocks/Icons");
            OVRGUIContent.RegisterContentPath(OVRGUIContent.Source.BuildingBlocksThumbnails, "BuildingBlocks/Thumbnails");
            OVRGUIContent.RegisterContentPath(OVRGUIContent.Source.BuildingBlocksAnimations, "BuildingBlocks/Animations");

            var statusItem = new OVRStatusMenu.Item()
            {
                Name = BlocksPublicName,
                Color = AccentColor,
                Icon = StatusIcon,
                InfoTextDelegate = ComputeMenuSubText,
                OnClickDelegate = OnStatusMenuClick,
                Order = 1
            };
            OVRStatusMenu.RegisterItem(statusItem);

            EditorApplication.projectChanged -= OnProjectChanged;
            EditorApplication.projectChanged += OnProjectChanged;
        }

        public static void OnProjectChanged()
        {
            _dirty = true;
        }

        public static string ComputeMenuSubText()
        {
            var numberOfBlocks = GetBlocksInScene().Count;
            return $"{numberOfBlocks} {OVREditorUtils.ChoosePlural(numberOfBlocks, "block", "blocks")} in current scene.";
        }

        private static void OnStatusMenuClick()
        {
            BuildingBlocksWindow.ShowWindow("StatusMenu");
        }

        public static void RefreshList(bool force = false)
        {
            if (!_dirty && !force)
            {
                return;
            }

            var blockGuids = AssetDatabase.FindAssets($"t:{nameof(BlockData)}");
            var blockDataList = blockGuids.Select(id =>
                    AssetDatabase.LoadAssetAtPath<BlockData>(AssetDatabase.GUIDToAssetPath(id)))
                .ToList();

            _idToBlockDataDictionary.Clear();
            foreach (var blockData in blockDataList)
            {
                _idToBlockDataDictionary[blockData.Id] = blockData;
            }


            _dirty = false;
        }

        public static BlockData GetBlockData(this BuildingBlock block)
        {
            return GetBlockData(block.BlockId);
        }

        public static BlockData GetBlockData(string blockId)
        {
            RefreshList();
            _idToBlockDataDictionary.TryGetValue(blockId, out var blockData);
            return blockData;
        }

        public static BuildingBlock GetBlock(this BlockData data)
        {
            return Object.FindObjectsOfType<BuildingBlock>().FirstOrDefault(x => x.BlockId == data.Id);
        }

        public static BuildingBlock GetBlock(string blockId)
        {
            return GetBlockData(blockId)?.GetBlock();
        }

        public static List<BuildingBlock> GetBlocks(this BlockData data)
        {
            return Object.FindObjectsOfType<BuildingBlock>().Where(x => x.BlockId == data.Id).ToList();
        }

        public static List<BuildingBlock> GetBlocks(string blockId)
        {
            return GetBlockData(blockId)?.GetBlocks();
        }

        public static List<T> GetBlocksWithType<T>() where T : Component
        {
            return Object.FindObjectsOfType<T>().Where(controller => controller.GetComponent<BuildingBlock>() != null).ToList();
        }

        public static bool IsRequiredBy(this BlockData data, BlockData other)
        {
            if (data == null || other == null)
            {
                return false;
            }

            if (data == other)
            {
                return true;
            }

            foreach (var dependency in other.Dependencies)
            {
                if (data.IsRequiredBy(dependency))
                {
                    return true;
                }
            }

            return false;
        }

        public static List<BuildingBlock> GetBlocksInScene()
        {
            return Object.FindObjectsOfType<BuildingBlock>().ToList();
        }

        public static List<BuildingBlock> GetUsingBlocksInScene(this BlockData requiredData)
        {
            return Object.FindObjectsOfType<BuildingBlock>().Where(x =>
            {
                var data = x.GetBlockData();
                return requiredData != data && requiredData.IsRequiredBy(data);
            }).ToList();
        }

        public static List<BlockData> GetUsingBlockDatasInScene(this BlockData requiredData)
        {
            return requiredData.GetUsingBlocksInScene().Select(x => x.GetBlockData()).ToList();
        }

        public static List<BlockData> GetAllDependencyDatas(this BlockData data)
        {
            return data.Dependencies
                .SelectMany(dependency => GetAllDependencyDatas(dependency).Concat(new[] { dependency }))
                .Distinct()
                .ToList();
        }

        public static void SelectBlockInScene(this BuildingBlock block)
        {
            Selection.activeGameObject = block.gameObject;
        }

        public static void SelectBlockInScene(this BlockData blockData)
        {
            blockData.GetBlock()?.SelectBlockInScene();
        }

        public static int ComputeNumberOfBlocksInScene(this BlockData blockData)
        {
            return Object.FindObjectsOfType<BuildingBlock>().Count(x => x.BlockId == blockData.Id);
        }
    }
}
