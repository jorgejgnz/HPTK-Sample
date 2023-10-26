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
using UnityEngine;

namespace Oculus.Interaction.DebugTree
{
    public class InteractorGroupDebugTreeUI : DebugTreeUI<IInteractor>
    {
        [SerializeField, Interface(typeof(IInteractor))]
        private UnityEngine.Object _root;

        [Tooltip("The node prefab which will be used to build the visual tree.")]
        [SerializeField, Interface(typeof(INodeUI<IInteractor>))]
        private UnityEngine.Component _nodePrefab;

        protected override IInteractor Value
        {
            get => _root as IInteractor;
        }

        protected override INodeUI<IInteractor> NodePrefab
        {
            get => _nodePrefab as INodeUI<IInteractor>;
        }

        protected override DebugTree<IInteractor> InstantiateTree(IInteractor value)
        {
            return new InteractorGroupDebugTree(value);
        }

        protected override string TitleForValue(IInteractor value)
        {
            Object obj = value as Object;
            return obj != null ? obj.name : "";
        }

        private class InteractorGroupDebugTree : DebugTree<IInteractor>
        {
            public InteractorGroupDebugTree(IInteractor root) : base(root)
            {
            }

            protected override bool TryGetChildren(IInteractor node, out IEnumerable<IInteractor> children)
            {
                if (node is InteractorGroup)
                {
                    children = (node as InteractorGroup).Interactors;
                    return true;
                }

                children = Enumerable.Empty<IInteractor>();
                return false;
            }
        }
    }


}
