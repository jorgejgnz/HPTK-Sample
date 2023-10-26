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
using UnityEngine;
using Oculus.Interaction.PoseDetection.Debug;

namespace Oculus.Interaction
{
    public class ActiveStateToggle : MonoBehaviour, IActiveState
    {
        public enum StatePrecedence
        {
            On,
            Off,
        }

        [Tooltip("When this ActiveState is Active, the " +
            "ActiveStateToggle will be Active.")]
        [SerializeField]
        [Interface(typeof(IActiveState))]
        private UnityEngine.Object _on;
        private IActiveState On;

        [Tooltip("When this ActiveState is Inactive, the " +
            "ActiveStateToggle will be Inactive.")]
        [SerializeField]
        [Interface(typeof(IActiveState))]
        private UnityEngine.Object _off;
        private IActiveState Off;

        [Tooltip("If both On and Off conditions are Active " +
            "simultaneously, this condition will take precedence " +
            "and dictate the output state.")]
        [SerializeField]
        private StatePrecedence _precedence = StatePrecedence.On;

        public StatePrecedence Precedence
        {
            get { return _precedence; }
            set { _precedence = value; }
        }

        private bool _internalActive;

        protected virtual void Awake()
        {
            On = _on as IActiveState;
            Off = _off as IActiveState;
        }

        protected virtual void Start()
        {
            this.AssertField(On, nameof(On));
            this.AssertField(Off, nameof(Off));
        }

        public bool Active
        {
            get
            {
                if (Precedence == StatePrecedence.Off)
                {
                    if (Off.Active)
                    {
                        _internalActive = false;
                    }
                    else if (On.Active)
                    {
                        _internalActive = true;
                    }
                }
                else if (Precedence == StatePrecedence.On)
                {
                    if (On.Active)
                    {
                        _internalActive = true;
                    }
                    else if (Off.Active)
                    {
                        _internalActive = false;
                    }
                }

                return _internalActive && isActiveAndEnabled;
            }
        }

        static ActiveStateToggle()
        {
            ActiveStateDebugTree.RegisterModel<ActiveStateToggle>(new DebugModel());
        }

        private class DebugModel : ActiveStateModel<ActiveStateToggle>
        {
            protected override IEnumerable<IActiveState> GetChildren(ActiveStateToggle activeState)
            {
                return new[] { activeState.On, activeState.Off };
            }
        }

        #region Inject

        public void InjectAllActiveStateToggle(IActiveState on, IActiveState off)
        {
            InjectOn(on);
            InjectOff(off);
        }

        public void InjectOn(IActiveState activeState)
        {
            _on = activeState as UnityEngine.Object;
            On = activeState;
        }

        public void InjectOff(IActiveState activeState)
        {
            _off = activeState as UnityEngine.Object;
            Off = activeState;
        }

        #endregion
    }
}
