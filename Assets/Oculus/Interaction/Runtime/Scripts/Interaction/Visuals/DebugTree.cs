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

namespace Oculus.Interaction.DebugTree
{
    public interface ITreeNode<TLeaf>
        where TLeaf : class
    {
        TLeaf Value { get; }
        IEnumerable<ITreeNode<TLeaf>> Children { get; }
    }

    public abstract class DebugTree<TLeaf>
        where TLeaf : class
    {
        private class Node : ITreeNode<TLeaf>
        {
            TLeaf ITreeNode<TLeaf>.Value => Value;
            IEnumerable<ITreeNode<TLeaf>> ITreeNode<TLeaf>.Children => Children;

            public TLeaf Value { get; set; }
            public List<Node> Children { get; set; }
        }

        private Dictionary<TLeaf, Node> _existingNodes =
            new Dictionary<TLeaf, Node>();

        private readonly TLeaf Root;
        private Node _rootNode;

        public DebugTree(TLeaf root)
        {
            Root = root;
        }

        public ITreeNode<TLeaf> GetRootNode()
        {
            if (_rootNode == null)
            {
                _rootNode = BuildTree(Root);
            }
            return _rootNode;
        }

        public void Rebuild()
        {
            _rootNode = BuildTree(Root);
        }

        private Node BuildTree(TLeaf root)
        {
            _existingNodes.Clear();
            return BuildTreeRecursive(root);
        }

        private Node BuildTreeRecursive(TLeaf value)
        {
            if (value == null)
            {
                return null;
            }

            if (_existingNodes.ContainsKey(value))
            {
                return _existingNodes[value];
            }

            List<Node> children = new List<Node>();

            if (TryGetChildren(value, out IEnumerable<TLeaf> c))
            {
                children.AddRange(c
                    .Select((child) => BuildTreeRecursive(child))
                    .Where((child) => child != null));
            }

            Node self = new Node()
            {
                Value = value,
                Children = children,
            };

            _existingNodes.Add(value, self);
            return self;
        }

        protected abstract bool TryGetChildren(TLeaf node, out IEnumerable<TLeaf> children);
    }
}
