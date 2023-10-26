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

using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// An Axis1D that switches between two Axis1D based on an ActiveState
    /// </summary>
    public class Axis1DPrioritySelector : MonoBehaviour, IAxis1D
    {
        [Serializable]
        public class AxisData
        {
            [SerializeField, Interface(typeof(IActiveState))]
            private UnityEngine.Object _activeState;
            public IActiveState ActiveState;

            [SerializeField, Interface(typeof(IAxis1D))]
            private UnityEngine.Object _axis;
            public IAxis1D Axis;

            public void Initialize()
            {
                ActiveState = _activeState as IActiveState;
                Axis = _axis as IAxis1D;
            }

            public void Validate (Component context)
            {
                context.AssertField(ActiveState, nameof(ActiveState));
                context.AssertField(Axis, nameof(Axis));
            }
        }

        [SerializeField] private AxisData[] _axisData;

        [SerializeField, Interface(typeof(IAxis1D))]
        private UnityEngine.Object _fallbackIfNoMatchAxis;
        private IAxis1D FallbackIfNoMatchAxis;

        private AxisData ActiveAxis;

        protected IAxis1D Current => GetActiveAxis();

        protected virtual void Awake()
        {
            foreach (var axisDatum in _axisData)
            {
                axisDatum.Initialize();
            }

            FallbackIfNoMatchAxis = _fallbackIfNoMatchAxis as IAxis1D;
        }

        protected virtual void Start()
        {
            foreach (var axisDatum in _axisData)
            {
                axisDatum.Validate(this);
            }

            this.AssertField(FallbackIfNoMatchAxis, nameof(FallbackIfNoMatchAxis));
        }

        public float Value()
        {
            return Current.Value();
        }

        private IAxis1D GetActiveAxis()
        {
            if ((ActiveAxis != null) && ActiveAxis.ActiveState.Active)
            {
                return ActiveAxis.Axis;
            }

            foreach (var axisDatum in _axisData)
            {
                if (axisDatum.ActiveState.Active)
                {
                    ActiveAxis = axisDatum;
                    return ActiveAxis.Axis;
                }
            }

            return FallbackIfNoMatchAxis;
        }

        #region Inject

        public void InjectAll(AxisData[] axisData, IAxis1D fallbackIfNoMatchAxis)
        {
            _axisData = axisData;
            foreach (var axisDatum in axisData)
            {
                axisDatum.Validate(this);
            }

            FallbackIfNoMatchAxis = fallbackIfNoMatchAxis;
            _fallbackIfNoMatchAxis = fallbackIfNoMatchAxis as UnityEngine.Object;
        }

        #endregion
    }
}
