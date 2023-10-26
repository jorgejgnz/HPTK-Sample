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

using Oculus.Interaction.DebugTree;
using UnityEngine;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class ActiveStateDebugTreeUI : DebugTreeUI<IActiveState>
    {
        [Tooltip("The IActiveState to debug.")]
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _activeState;

        [Tooltip("The node prefab which will be used to build the visual tree.")]
        [SerializeField, Interface(typeof(INodeUI<IActiveState>))]
        private UnityEngine.Component _nodePrefab;

        protected override IActiveState Value
        {
            get => _activeState as IActiveState;
        }

        protected override INodeUI<IActiveState> NodePrefab
        {
            get => _nodePrefab as INodeUI<IActiveState>;
        }

        protected override DebugTree<IActiveState> InstantiateTree(IActiveState value)
        {
            return new ActiveStateDebugTree(value);
        }
        protected override string TitleForValue(IActiveState value)
        {
            Object obj = value as Object;
            return obj != null ? obj.name : "";
        }

    }
}
