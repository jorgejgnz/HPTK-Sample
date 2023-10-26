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
using UnityEngine.Events;

namespace Oculus.Interaction.Locomotion
{
    public class LocomotionGateUnityEventWrapper : MonoBehaviour
    {
        [SerializeField]
        private LocomotionGate _locomotionGate;

        public UnityEvent WhenEnterLocomotion;
        public UnityEvent WhenExitLocomotion;
        public UnityEvent WhenChangedToTurn;
        public UnityEvent WhenChangedToTeleport;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_locomotionGate, nameof(_locomotionGate));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _locomotionGate.WhenActiveModeChanged += HandleActiveModeChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _locomotionGate.WhenActiveModeChanged -= HandleActiveModeChanged;
            }
        }

        private void HandleActiveModeChanged(LocomotionGate.LocomotionModeEventArgs locomotionModeArgs)
        {
            if (locomotionModeArgs.PreviousMode == LocomotionGate.LocomotionMode.None)
            {
                WhenEnterLocomotion.Invoke();
            }
            else if (locomotionModeArgs.NewMode == LocomotionGate.LocomotionMode.Teleport)
            {
                WhenChangedToTeleport.Invoke();
            }
            else if (locomotionModeArgs.NewMode == LocomotionGate.LocomotionMode.Turn)
            {
                WhenChangedToTurn.Invoke();
            }
            else if (locomotionModeArgs.NewMode == LocomotionGate.LocomotionMode.None)
            {
                WhenExitLocomotion.Invoke();
            }
        }

        #region Inject
        public void InjectAllLocomotionGateUnityEventWrapper(LocomotionGate locomotionGate)
        {
            InjectLocomotionGate(locomotionGate);
        }

        public void InjectLocomotionGate(LocomotionGate locomotionGate)
        {
            _locomotionGate = locomotionGate;
        }
        #endregion
    }
}
