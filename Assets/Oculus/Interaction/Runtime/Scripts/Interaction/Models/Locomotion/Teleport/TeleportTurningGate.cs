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
using UnityEngine;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This Gate reads the Hand orientation towards the shoulder and decides
    /// if it should be in Teleport mode (hand horizontal) or Turning mode (hand vertical).
    /// It enables/disables said modes based on some Input ActiveStates (EnableShape and DisableShape).
    /// It outputs it result into two ActiveStates (for Teleport and Turn)
    /// </summary>
    public class TeleportTurningGate : MonoBehaviour
    {
        /// <summary>
        /// Hand that will be performing the Turn and Teleport
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private MonoBehaviour _hand;
        private IHand Hand { get; set; }

        /// <summary>
        /// Shoulder of the relevant Hand, used for correctly
        /// measuring the angle of the wrist to swap between Turning or Teleport
        /// </summary>
        [SerializeField]
        private Transform _shoulder;

        [SerializeField]
        private bool _allowPalmDownGating = false;
        public bool AllowPalmDownGating
        {
            get
            {
                return _allowPalmDownGating;
            }
            set
            {
                _allowPalmDownGating = value;
            }
        }

        [SerializeField]
        private Vector2 _palmUpToTurnThresholds = new Vector2(50f, 95f);
        [SerializeField]
        private Vector2 _turnToPalmDownToThresholds = new Vector2(110f, 140f);

        /// <summary>
        /// When it becomes Active, if the hand is within the valid threshold, the
        /// gate will enter Teleport or Turning mode
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _enableShape;
        private IActiveState EnableShape { get; set; }

        /// <summary>
        /// When active, the gate will exit Teleport and Turning modes
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private MonoBehaviour _disableShape;
        private IActiveState DisableShape { get; set; }

        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Turning mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _turningState;
        /// <summary>
        /// Used as an Output. The gate will enable this ActiveState when in Teleport mode
        /// </summary>
        [SerializeField]
        private VirtualActiveState _teleportState;

        protected bool _started;
        private bool _previousShapeEnabled;

        private LocomotionMode _activeMode = LocomotionMode.None;
        private LocomotionMode ActiveMode
        {
            get
            {
                return _activeMode;
            }
            set
            {
                _activeMode = value;
                _teleportState.Active = _activeMode == LocomotionMode.Teleport;
                _turningState.Active = _activeMode == LocomotionMode.Turn;
            }
        }

        private enum LocomotionMode
        {
            None,
            Teleport,
            Turn
        }

        private const float _selectModeOnEnterThreshold = 0.5f;
        private const float _enterPoseThreshold = 0.5f;

        protected virtual void Awake()
        {
            Hand = _hand as IHand;
            EnableShape = _enableShape as IActiveState;
            DisableShape = _disableShape as IActiveState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(Hand, nameof(Hand));
            this.AssertField(EnableShape, nameof(EnableShape));
            this.AssertField(DisableShape, nameof(DisableShape));
            this.AssertField(_teleportState, nameof(_teleportState));
            this.AssertField(_turningState, nameof(_turningState));
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated += HandleHandupdated;
                Disable();
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Hand.WhenHandUpdated -= HandleHandupdated;
                Disable();
            }
        }

        private void HandleHandupdated()
        {
            if (!Hand.GetRootPose(out Pose handPose))
            {
                Disable();
                return;
            }

            Vector3 trackingUp = Vector3.up;
            Vector3 shoulderToHand = (handPose.position - _shoulder.position).normalized;
            Vector3 trackingRight = Vector3.Cross(trackingUp, shoulderToHand).normalized;
            trackingRight = Hand.Handedness == Handedness.Right ? trackingRight : -trackingRight;
            Vector3 wristDir = Hand.Handedness == Handedness.Left ? handPose.forward : -handPose.forward;
            Vector3 fingersDir = Hand.Handedness == Handedness.Left ? handPose.right : -handPose.right;

            Vector3 palmDir = Hand.Handedness == Handedness.Left ? handPose.up : -handPose.up;
            bool palmUp = (Vector3.Dot(palmDir, trackingUp) * 0.5 + 0.5f) > _enterPoseThreshold;
            bool flatHand = Mathf.Abs(Vector3.Dot(wristDir, trackingRight)) > _selectModeOnEnterThreshold;
            bool fingersAway = (Vector3.Dot(fingersDir, Vector3.ProjectOnPlane(shoulderToHand, trackingUp).normalized) * 0.5 + 0.5f) > _enterPoseThreshold;
            float angle = Vector3.SignedAngle(wristDir, trackingRight, shoulderToHand);
            angle = Hand.Handedness == Handedness.Right ? -angle : angle;
            bool shapeGateEnabled = false;
            if (EnableShape.Active && !_previousShapeEnabled)
            {
                shapeGateEnabled = true;
            }
            _previousShapeEnabled = EnableShape.Active;

            if (ActiveMode == LocomotionMode.None
                && shapeGateEnabled
                && fingersAway)
            {
                if (flatHand)
                {
                    if (palmUp || _allowPalmDownGating)
                    {
                        ActiveMode = LocomotionMode.Teleport;
                    }
                }
                else
                {
                    ActiveMode = LocomotionMode.Turn;
                }
                return;
            }

            if (ActiveMode != LocomotionMode.None
                && DisableShape.Active)
            {
                ActiveMode = LocomotionMode.None;
                return;
            }

            if (ActiveMode == LocomotionMode.Teleport)
            {
                if (angle > _palmUpToTurnThresholds.y
                    && angle < _turnToPalmDownToThresholds.x)
                {
                    ActiveMode = LocomotionMode.Turn;
                }
            }
            else if (ActiveMode == LocomotionMode.Turn)
            {
                if (angle <= _palmUpToTurnThresholds.x
                    || angle >= _turnToPalmDownToThresholds.y)
                {
                    ActiveMode = LocomotionMode.Teleport;
                }
            }
        }

        private void Disable()
        {
            ActiveMode = LocomotionMode.None;
        }

        #region Inject

        public void InjectAllTeleportTurningGate(IHand hand, Transform shoulder,
            IActiveState enableShape, IActiveState disableShape,
            VirtualActiveState turningState, VirtualActiveState teleportState)
        {
            InjectHand(hand);
            InjectShoulder(shoulder);
            InjectEnableShape(enableShape);
            InjectDisableShape(disableShape);
            InjectTurningState(turningState);
            InjectTeleportState(teleportState);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as MonoBehaviour;
            Hand = hand;
        }

        public void InjectShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        public void InjectEnableShape(IActiveState enableShape)
        {
            _enableShape = enableShape as MonoBehaviour;
            EnableShape = enableShape;
        }

        public void InjectDisableShape(IActiveState disableShape)
        {
            _disableShape = disableShape as MonoBehaviour;
            DisableShape = disableShape;
        }

        public void InjectTurningState(VirtualActiveState turningState)
        {
            _turningState = turningState;
        }

        public void InjectTeleportState(VirtualActiveState teleportState)
        {
            _teleportState = teleportState;
        }

        #endregion
    }
}
