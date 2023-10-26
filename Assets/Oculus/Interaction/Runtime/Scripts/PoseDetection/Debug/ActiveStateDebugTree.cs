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
using System;
using System.Collections.Generic;

namespace Oculus.Interaction.PoseDetection.Debug
{
    public class ActiveStateDebugTree : DebugTree<IActiveState>
    {
        public ActiveStateDebugTree(IActiveState root) : base(root)
        {
        }

        private static Dictionary<Type, IActiveStateModel> _models =
            new Dictionary<Type, IActiveStateModel>();

        public static void RegisterModel<TType>(IActiveStateModel stateModel)
            where TType : class, IActiveState
        {
            Type key = typeof(TType);
            if (_models.ContainsKey(key))
            {
                _models[key] = stateModel;
            }
            else
            {
                _models.Add(key, stateModel);
            }
        }

        protected override bool TryGetChildren(IActiveState node, out IEnumerable<IActiveState> children)
        {
            if (_models.TryGetValue(node.GetType(), out IActiveStateModel model)
                && model != null)
            {
                children = model.GetChildren(node);
                return true;
            }
            children = null;
            return false;
        }
    }
}
