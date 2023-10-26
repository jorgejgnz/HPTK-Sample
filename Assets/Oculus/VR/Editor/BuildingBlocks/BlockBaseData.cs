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
using UnityEngine;
using NUnit.Framework;

namespace Meta.XR.BuildingBlocks.Editor
{
    public abstract class BlockBaseData : ScriptableObject
    {
        [SerializeField, OVRReadOnly] protected string id = Guid.NewGuid().ToString();
        public string Id => id;

        [SerializeField, OVRReadOnly] internal int version = 1;
        public int Version => version;

        public enum SDKType
        {
            Interaction,
            Movement,
            Passthrough,
            Voice,
            Scene,
            VR
        }

        private static readonly OVRGUIContent DefaultThumbnailTexture = OVREditorUtils.CreateContent("bb_thumb_default.png",
            OVRGUIContent.Source.BuildingBlocksThumbnails);

        private static readonly OVRGUIContent DefaultInternalThumbnailTexture = OVREditorUtils.CreateContent("bb_thumb_internal.png",
            OVRGUIContent.Source.BuildingBlocksThumbnails);

        [SerializeField] internal string blockName;
        public string BlockName => blockName;

        [SerializeField] private string description;
        public string Description => description;

        [SerializeField] private SDKType sdk;
        public SDKType Sdk => sdk;

        [SerializeField] private Texture2D thumbnail;

        public Texture2D Thumbnail
        {
            get
            {
                if (thumbnail != null)
                {
                    return thumbnail;
                }

                if (DisplayOnContentTab)
                {
                    return DefaultThumbnailTexture.Content.image as Texture2D;
                }

                return DefaultInternalThumbnailTexture.Content.image as Texture2D;
            }
        }

        [Tooltip("Indicates whether this blocks is displayed in the Building Blocks window and is installable by BB users or is an internal block that's meant to be a dependency of other blocks and not directly accessible.")]
        [SerializeField] private bool displayOnContentTab = true;
        public virtual bool DisplayOnContentTab => displayOnContentTab;

        [Tooltip("Indicates whether this blocks is still in an experimental phase and may go through significant changes in the upcoming SDK versions.")]
        [SerializeField] private bool experimental;
        public bool Experimental => experimental;

        [SerializeField] private int order;
        public int Order => order;

        [ContextMenu("Assign ID")]
        internal void AssignId()
        {
            id = Guid.NewGuid().ToString();
        }

        [ContextMenu("Copy ID to clipboard")]
        internal void CopyIdToClipboard()
        {
            GUIUtility.systemCopyBuffer = Id;
        }

        [ContextMenu("Increment Version")]
        internal void IncrementVersion()
        {
            version++;
        }

        [ContextMenu("Validate")]
        internal virtual void Validate()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Id), $"{nameof(Id)} cannot be null or empty");
            Assert.IsFalse(string.IsNullOrEmpty(BlockName), $"{nameof(BlockName)} cannot be null or empty");
            Assert.IsFalse(string.IsNullOrEmpty(Description), $"{nameof(Description)} cannot be null or empty");
        }

        internal abstract bool CanBeAdded { get; }

        internal abstract void AddToProject(Action onInstall = null);

        internal virtual bool RequireListRefreshAfterInstall => false;
    }
}
