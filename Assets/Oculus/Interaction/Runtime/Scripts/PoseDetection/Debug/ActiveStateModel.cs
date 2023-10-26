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

namespace Oculus.Interaction.PoseDetection.Debug
{
    public interface IActiveStateModel
    {
        IEnumerable<IActiveState> GetChildren(IActiveState activeState);
    }

    public abstract class ActiveStateModel<TActiveState> : IActiveStateModel
        where TActiveState : class, IActiveState
    {
        public IEnumerable<IActiveState> GetChildren(IActiveState activeState)
        {
            if (activeState is TActiveState type)
            {
                return GetChildren(type);
            }
            return Enumerable.Empty<IActiveState>();
        }

        protected abstract IEnumerable<IActiveState> GetChildren(TActiveState activeState);
    }
}
