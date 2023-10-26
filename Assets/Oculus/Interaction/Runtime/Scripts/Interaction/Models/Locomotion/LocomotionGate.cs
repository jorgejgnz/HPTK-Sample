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

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// This Gate reads the Hand orientation towards the shoulder and decides
    /// if it should be in Teleport mode  or Turning mode.
    /// It enables/disables said modes based on some Input ActiveStates (EnableShape and DisableShape).
    /// It outputs it result into two ActiveStates (for Teleport and Turn)
    /// </summary>
    public class LocomotionGate : MonoBehaviour
    {
        /// <summary>
        /// Hand that will be performing the Turn and Teleport
        /// </summary>
        [SerializeField, Interface(typeof(IHand))]
        private UnityEngine.Object _hand;
        public IHand Hand { get; private set; }

        /// <summary>
        /// Shoulder of the relevant Hand, used for correctly
        /// measuring the angle of the wrist to swap between Turning or Teleport
        /// </summary>
        [SerializeField]
        private Transform _shoulder;

        [SerializeField]
        private GateSection[] _gateSections = new GateSection[] {
            new GateSection(){locomotionMode = LocomotionMode.Teleport, maxAngle = 95f },
            new GateSection(){locomotionMode = LocomotionMode.Turn, minAngle = 40f, maxAngle =  165f },
            new GateSection(){locomotionMode = LocomotionMode.Teleport, minAngle = 120f, canEnterDirectly = false }
        };

        /// <summary>
        /// When it becomes Active, if the hand is within the valid threshold, the
        /// gate will enter Teleport or Turning mode
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _enableShape;
        private IActiveState EnableShape { get; set; }

        /// <summary>
        /// When active, the gate will exit Teleport and Turning modes
        /// </summary>
        [SerializeField, Interface(typeof(IActiveState))]
        private UnityEngine.Object _disableShape;
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
        private int _currentGateIndex = -1;

        private LocomotionMode _activeMode = LocomotionMode.None;
        public LocomotionMode ActiveMode
        {
            get
            {
                return _activeMode;
            }
            private set
            {
                if (_activeMode != value)
                {
                    LocomotionMode prevMode = _activeMode;
                    _activeMode = value;
                    _teleportState.Active = _activeMode == LocomotionMode.Teleport;
                    _turningState.Active = _activeMode == LocomotionMode.Turn;
                    _whenActiveModeChanged.Invoke(new LocomotionModeEventArgs(prevMode, _activeMode));
                }
            }
        }

        public enum LocomotionMode
        {
            None,
            Teleport,
            Turn
        }

        [System.Serializable]
        public class GateSection
        {
            public float minAngle = _wristLimit;
            public float maxAngle = 360f + _wristLimit;
            public bool canEnterDirectly = true;
            public LocomotionMode locomotionMode = LocomotionMode.None;

            public float ScoreToAngle(float angle)
            {
                float dif = Mathf.Repeat(angle - minAngle, 360f);
                float range = Mathf.Repeat(maxAngle - minAngle, 360f);
                if (dif > range)
                {
                    return float.PositiveInfinity;
                }

                float average = (minAngle + maxAngle) / 2f;
                float score = Mathf.Repeat(Mathf.DeltaAngle(angle, average), 360f);
                return score;
            }
        }

        public struct LocomotionModeEventArgs
        {
            public LocomotionMode PreviousMode { get; }
            public LocomotionMode NewMode { get; }

            public LocomotionModeEventArgs(
                LocomotionMode previousMode,
                LocomotionMode newMode)
            {
                PreviousMode = previousMode;
                NewMode = newMode;
            }
        }

        public float CurrentAngle { get; private set; }
        public Vector3 WristDirection { get; private set; }
        public Pose StabilizationPose { get; private set; } = Pose.identity;

        private Action<LocomotionModeEventArgs> _whenActiveModeChanged = delegate { };
        public event Action<LocomotionModeEventArgs> WhenActiveModeChanged
        {
            add => _whenActiveModeChanged += value;
            remove => _whenActiveModeChanged -= value;
        }

        private static readonly GateSection DefaultSection = new GateSection() { locomotionMode = LocomotionMode.None };
        private const float _enterPoseThreshold = 0.5f;
        private const float _wristLimit = -70f;

        private bool _cancelled = false;

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

        /// <summary>
        /// Disables the Gate, forcing the user to perform the enable shape
        /// </summary>
        public void Disable()
        {
            ActiveMode = LocomotionMode.None;
            _currentGateIndex = -1;
            _cancelled = false;
        }

        /// <summary>
        /// Disables the Gate, forcing the user to perform the disable shape
        /// and then the enable shape
        /// </summary>
        public void Cancel()
        {
            ActiveMode = LocomotionMode.None;
            if (_currentGateIndex >= 0)
            {
                _cancelled = true;
            }
        }

        private void HandleHandupdated()
        {
            if (!Hand.GetRootPose(out Pose handPose))
            {
                Disable();
                return;
            }

            bool isRightHand = Hand.Handedness == Handedness.Right;
            Vector3 trackingUp = Vector3.up;
            Vector3 shoulderToHand = (handPose.position - _shoulder.position).normalized;
            Vector3 trackingRight = Vector3.Cross(trackingUp, shoulderToHand).normalized;
            trackingRight = isRightHand ? trackingRight : -trackingRight;
            Vector3 wristDir = isRightHand ? -handPose.forward : handPose.forward;
            Vector3 fingersDir = isRightHand ? -handPose.right : handPose.right;
            bool fingersAway = (Vector3.Dot(fingersDir, Vector3.ProjectOnPlane(shoulderToHand, trackingUp).normalized) * 0.5 + 0.5f) > _enterPoseThreshold;

            wristDir = Vector3.ProjectOnPlane(wristDir, shoulderToHand).normalized;
            float angle = Vector3.SignedAngle(wristDir, trackingRight, shoulderToHand);

            angle = Hand.Handedness == Handedness.Right ? -angle : angle;
            if (angle < _wristLimit)
            {
                angle += 360f;
            }

            CurrentAngle = angle;
            StabilizationPose = new Pose(_shoulder.position, Quaternion.LookRotation(shoulderToHand));
            WristDirection = wristDir;

            bool shapeGateEnabled = false;
            if (EnableShape.Active && !_previousShapeEnabled)
            {
                shapeGateEnabled = true;
            }
            _previousShapeEnabled = EnableShape.Active;

            if (_currentGateIndex < 0
                && shapeGateEnabled
                && fingersAway)
            {
                GateSection gateSection = GetBestGateSection(CurrentAngle, out _currentGateIndex);
                if (gateSection.canEnterDirectly)
                {
                    ActiveMode = gateSection.locomotionMode;
                }
                else
                {
                    _currentGateIndex = -1;
                }
                return;
            }

            if (_currentGateIndex >= 0
                && DisableShape.Active)
            {
                Disable();
                return;
            }

            if (_currentGateIndex < 0 || _cancelled)
            {
                return;
            }

            GateSection currentGate = _gateSections[_currentGateIndex];

            if (CurrentAngle < currentGate.minAngle)
            {
                _currentGateIndex = Mathf.Max(0, _currentGateIndex - 1);
                ActiveMode = _gateSections[_currentGateIndex].locomotionMode;
            }
            else if (CurrentAngle > currentGate.maxAngle)
            {
                _currentGateIndex = Mathf.Min(_gateSections.Length - 1, _currentGateIndex + 1);
                ActiveMode = _gateSections[_currentGateIndex].locomotionMode;
            }
        }

        private GateSection GetBestGateSection(float angle, out int index)
        {
            float bestScore = float.PositiveInfinity;
            index = -1;

            for (int i = 0; i < _gateSections.Length; i++)
            {
                float score = _gateSections[i].ScoreToAngle(angle);
                if (score < bestScore)
                {
                    bestScore = score;
                    index = i;
                }
            }

            if (index == -1)
            {
                return DefaultSection;
            }
            return _gateSections[index];
        }

        #region Inject

        public void InjectAllLocomotionGate(IHand hand, Transform shoulder,
            IActiveState enableShape, IActiveState disableShape,
            VirtualActiveState turningState, VirtualActiveState teleportState,
            GateSection[] gateSections)
        {
            InjectHand(hand);
            InjectShoulder(shoulder);
            InjectEnableShape(enableShape);
            InjectDisableShape(disableShape);
            InjectTurningState(turningState);
            InjectTeleportState(teleportState);
            InjectGateSections(gateSections);
        }

        public void InjectHand(IHand hand)
        {
            _hand = hand as UnityEngine.Object;
            Hand = hand;
        }

        public void InjectShoulder(Transform shoulder)
        {
            _shoulder = shoulder;
        }

        public void InjectEnableShape(IActiveState enableShape)
        {
            _enableShape = enableShape as UnityEngine.Object;
            EnableShape = enableShape;
        }

        public void InjectDisableShape(IActiveState disableShape)
        {
            _disableShape = disableShape as UnityEngine.Object;
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

        public void InjectGateSections(GateSection[] gateSections)
        {
            _gateSections = gateSections;
        }

        #endregion
    }
}
