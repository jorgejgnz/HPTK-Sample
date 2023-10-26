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
using TMPro;
using System.Collections.Generic;

namespace Oculus.Interaction.DebugTree
{
    public interface INodeUI<TLeaf>
        where TLeaf : class
    {
        RectTransform ChildArea { get; }
        void Bind(ITreeNode<TLeaf> node, bool isRoot, bool isDuplicate);
    }

    public abstract class DebugTreeUI<TLeaf> : MonoBehaviour
        where TLeaf : class
    {
        [Tooltip("Node prefabs will be instantiated inside of this content area.")]
        [SerializeField]
        private RectTransform _contentArea;

        [Tooltip("This title text will display the GameObject name of the IActiveState.")]
        [SerializeField, Optional]
        private TMP_Text _title;

        [Tooltip("If true, the tree UI will be built on Start.")]
        [SerializeField]
        private bool _buildTreeOnStart;

        protected abstract TLeaf Value { get; }
        protected abstract INodeUI<TLeaf> NodePrefab { get; }

        private DebugTree<TLeaf> _tree;

        private Dictionary<ITreeNode<TLeaf>, INodeUI<TLeaf>> _nodeToUI
            = new Dictionary<ITreeNode<TLeaf>, INodeUI<TLeaf>>();

        protected virtual void Start()
        {
            this.AssertField(Value, nameof(Value));
            this.AssertField(NodePrefab, nameof(NodePrefab));
            this.AssertField(_contentArea, nameof(_contentArea));

            if (_buildTreeOnStart)
            {
                BuildTree();
            }
        }

        public void BuildTree()
        {
            _nodeToUI.Clear();
            ClearContentArea();
            SetTitleText();
            _tree = InstantiateTree(Value);
            BuildTreeRecursive(_contentArea, _tree.GetRootNode(), true);
        }

        private void BuildTreeRecursive(
            RectTransform parent, ITreeNode<TLeaf> node, bool isRoot)
        {
            INodeUI<TLeaf> nodeUI = Instantiate(NodePrefab as Object, parent) as INodeUI<TLeaf>;

            bool isDuplicate = _nodeToUI.ContainsKey(node);
            nodeUI.Bind(node, isRoot, isDuplicate);

            if (!isDuplicate)
            {
                _nodeToUI.Add(node, nodeUI);
                foreach (var child in node.Children)
                {
                    BuildTreeRecursive(nodeUI.ChildArea, child, false);
                }
            }
        }

        private void ClearContentArea()
        {
            for (int i = 0; i < _contentArea.childCount; ++i)
            {
                Transform child = _contentArea.GetChild(i);
                if (child != null && child.TryGetComponent<INodeUI<TLeaf>>(out _))
                {
                    Destroy(child.gameObject);
                }
            }
        }

        private void SetTitleText()
        {
            if (_title != null)
            {
                _title.text = TitleForValue(Value);
            }
        }

        protected abstract DebugTree<TLeaf> InstantiateTree(TLeaf value);
        protected abstract string TitleForValue(TLeaf value);

#if UNITY_EDITOR
        private void OnValidate()
        {
            SetTitleText();
        }
#endif
    }
}
