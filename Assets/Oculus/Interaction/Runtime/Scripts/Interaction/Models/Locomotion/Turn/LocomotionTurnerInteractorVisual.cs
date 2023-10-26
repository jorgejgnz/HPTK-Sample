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
    public class LocomotionTurnerInteractorVisual : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField]
        private LocomotionTurnerInteractor _turner;
        [SerializeField, Optional]
        private TurnerEventBroadcaster _broadcaster;
        [SerializeField, Optional]
        private Transform _lookAt;

        [SerializeField, Interface(typeof(IAxis1D)), Optional]
        private UnityEngine.Object _progress;
        private IAxis1D Progress { get; set; }

        [Header("Visual renderers")]
        [SerializeField]
        private Transform _root;
        [SerializeField]
        private Renderer _leftArrow;
        [SerializeField]
        private Renderer _rightArrow;
        [SerializeField]
        private TubeRenderer _leftRail;
        [SerializeField]
        private TubeRenderer _rightRail;
        [SerializeField]
        private TubeRenderer _leftTrail;
        [SerializeField]
        private TubeRenderer _rightTrail;

        [SerializeField]
        private MaterialPropertyBlockEditor _leftMaterialBlock;
        [SerializeField]
        private MaterialPropertyBlockEditor _rightMaterialBlock;

        [Header("Visual parameters")]
        [SerializeField]
        private float _verticalOffset = 0.02f;
        public float VerticalOffset
        {
            get => _verticalOffset;
            set => _verticalOffset = value;
        }

        [SerializeField]
        private float _radius = 0.07f;
        [SerializeField]
        private float _margin = 2f;
        [SerializeField]
        private float _trailLength = 15f;
        [SerializeField]
        private float _maxAngle = 45f;
        [SerializeField]
        private float _railGap = 0.005f;
        [SerializeField]
        private float _squeezeLength = 5f;

        [SerializeField]
        private Color _disabledColor = new Color(1f, 1f, 1f, 0.2f);
        public Color DisabledColor
        {
            get => _disabledColor;
            set => _disabledColor = value;
        }

        [SerializeField]
        private Color _enabledColor = new Color(1f, 1f, 1f, 0.6f);
        public Color EnabledColor
        {
            get => _enabledColor;
            set => _enabledColor = value;
        }

        [SerializeField]
        private Color _highligtedColor = new Color(1f, 1f, 1f, 1f);
        public Color HighligtedColor
        {
            get => _highligtedColor;
            set => _highligtedColor = value;
        }


        private const float _degreesPerSegment = 1f;

        private static readonly Quaternion _rotationCorrectionLeft = Quaternion.Euler(0f, -90f, 0f);
        private static readonly int _colorShaderPropertyID = Shader.PropertyToID("_Color");

        protected bool _started;

        protected virtual void Awake()
        {
            Progress = _progress as IAxis1D;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_turner, nameof(_turner));
            this.AssertField(_root, nameof(_root));
            this.AssertField(_leftTrail, nameof(_leftTrail));
            this.AssertField(_rightTrail, nameof(_rightTrail));
            this.AssertField(_leftArrow, nameof(_leftArrow));
            this.AssertField(_rightArrow, nameof(_rightArrow));
            this.AssertField(_leftRail, nameof(_leftRail));
            this.AssertField(_rightRail, nameof(_rightRail));

            this.AssertField(_leftMaterialBlock, nameof(_leftMaterialBlock));
            this.AssertField(_rightMaterialBlock, nameof(_rightMaterialBlock));

            InitializeVisuals();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _turner.WhenStateChanged += HandleTurnerStateChanged;
                _turner.WhenPreprocessed += HandleTurnerPostprocessed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _turner.WhenStateChanged -= HandleTurnerStateChanged;
                _turner.WhenPreprocessed -= HandleTurnerPostprocessed;
            }
        }

        private void HandleTurnerStateChanged(InteractorStateChangeArgs stateArgs)
        {
            if (stateArgs.NewState == InteractorState.Disabled)
            {
                _leftTrail.enabled = false;
                _rightTrail.enabled = false;
                _leftArrow.enabled = false;
                _rightArrow.enabled = false;
                _leftRail.enabled = false;
                _rightRail.enabled = false;
            }
            else if (stateArgs.PreviousState == InteractorState.Disabled)
            {
                _leftTrail.enabled = true;
                _rightTrail.enabled = true;
                _leftArrow.enabled = true;
                _rightArrow.enabled = true;
                _leftRail.enabled = true;
                _rightRail.enabled = true;
            }
        }

        private void InitializeVisuals()
        {
            TubePoint[] trailPoints = InitializeSegment(new Vector2(_margin, _maxAngle + _squeezeLength));
            _leftTrail.RenderTube(trailPoints, Space.Self);
            _rightTrail.RenderTube(trailPoints, Space.Self);

            TubePoint[] railPoints = InitializeSegment(new Vector2(_margin, _maxAngle));
            _leftRail.RenderTube(railPoints, Space.Self);
            _rightRail.RenderTube(railPoints, Space.Self);
        }

        private void HandleTurnerPostprocessed()
        {
            if (_turner.State == InteractorState.Disabled)
            {
                return;
            }
            UpdatePose();
            UpdateArrows();
            UpdateColors();
        }

        private void UpdatePose()
        {
            Pose origin = _turner.MidPoint;
            Vector3 forward = origin.forward;
            if (_lookAt != null)
            {
                forward = Vector3.ProjectOnPlane(origin.position - _lookAt.position, origin.up).normalized;
            }
            Vector3 position = origin.position - forward * _radius + origin.up * _verticalOffset;
            Quaternion rotation = Quaternion.LookRotation(forward, origin.up);

            _root.SetPositionAndRotation(position, rotation);
        }

        private void UpdateArrows()
        {
            float value = _turner.Value();
            float angle = Mathf.Lerp(0f, _maxAngle, Mathf.Abs(value));
            bool isLeft = value < 0;
            bool follow = ShouldFollowArrow();
            float squeeze = 0f;
            if (Progress != null)
            {
                squeeze = Mathf.Lerp(0f, _squeezeLength, Progress.Value());
            }

            _rightRail.enabled = !isLeft;
            _leftRail.enabled = isLeft;

            angle = Mathf.Max(angle, _trailLength);

            UpdateArrowPosition(isLeft ? _trailLength : angle + squeeze, _rightArrow.transform);
            RotateTrail(follow && !isLeft ? angle - _trailLength : 0f, _rightTrail);
            UpdateTrail(isLeft ? _trailLength : (follow ? _trailLength : angle) + squeeze, _rightTrail);

            UpdateArrowPosition(!isLeft ? -_trailLength : -angle - squeeze, _leftArrow.transform);
            RotateTrail(follow && isLeft ? -angle + _trailLength : 0f, _leftTrail);
            UpdateTrail(!isLeft ? _trailLength : (follow ? _trailLength : angle) + squeeze, _leftTrail);

            UpdateRail(angle, squeeze, isLeft ? _leftRail : _rightRail);
        }

        private void UpdateArrowPosition(float angle, Transform arrow)
        {
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            arrow.localPosition = rotation * Vector3.forward * _radius;
            arrow.localRotation = rotation * _rotationCorrectionLeft;
        }

        private void RotateTrail(float angle, TubeRenderer trail)
        {
            trail.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.up);
        }

        private void UpdateTrail(float angle, TubeRenderer trail)
        {
            float max = _maxAngle + _squeezeLength;
            float segmentLenght = trail.TotalLength;
            float start = -100;
            float end = (max - angle - _margin) / max;

            trail.StartFadeThresold = segmentLenght * start;
            trail.EndFadeThresold = segmentLenght * end;
            trail.InvertThreshold = false;
            trail.RedrawFadeThresholds();
        }

        private void UpdateRail(float angle, float extra, TubeRenderer rail)
        {
            float segmentLenght = rail.TotalLength;
            float start = (angle - _trailLength - _margin) / _maxAngle;
            float end = (_maxAngle - angle - extra - _margin) / _maxAngle;

            float gap = _railGap + rail.Feather;
            rail.StartFadeThresold = segmentLenght * start - gap;
            rail.EndFadeThresold = segmentLenght * end - gap;
            rail.InvertThreshold = true;
            rail.RedrawFadeThresholds();
        }

        private void UpdateColors()
        {
            bool isSelection = _turner.State == InteractorState.Select;
            if (_turner.Value() > 0)
            {
                _leftMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, _disabledColor);
                _rightMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, isSelection ? _highligtedColor : _enabledColor);
            }
            else
            {
                _leftMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, isSelection ? _highligtedColor : _enabledColor);
                _rightMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, _disabledColor);
            }
            _leftMaterialBlock.UpdateMaterialPropertyBlock();
            _rightMaterialBlock.UpdateMaterialPropertyBlock();
        }

        private bool ShouldFollowArrow()
        {
            if (_broadcaster != null)
            {
                return _broadcaster.TurnMethod == TurnerEventBroadcaster.TurnMode.Snap;
            }
            return true;
        }

        private TubePoint[] InitializeSegment(Vector2 minMax)
        {
            float lowLimit = minMax.x;
            float upLimit = minMax.y;
            int segments = Mathf.RoundToInt(Mathf.Repeat(upLimit - lowLimit, 360f) / _degreesPerSegment);
            TubePoint[] tubePoints = new TubePoint[segments];
            float segmentLenght = 1f / segments;
            for (int i = 0; i < segments; i++)
            {
                Quaternion rotation = Quaternion.AngleAxis(-i * _degreesPerSegment - lowLimit, Vector3.up);
                tubePoints[i] = new TubePoint()
                {
                    position = rotation * Vector3.forward * _radius,
                    rotation = rotation * _rotationCorrectionLeft,
                    relativeLength = i * segmentLenght
                };
            }
            return tubePoints;
        }

        #region Inject

        public void InjectAllLocomotionTurnerInteractorArrowsVisual(
            LocomotionTurnerInteractor turner, Transform root, Renderer leftArrow, Renderer rightArrow,
            TubeRenderer leftRail, TubeRenderer rightRail, TubeRenderer leftTrail, TubeRenderer rightTrail,
            MaterialPropertyBlockEditor leftMaterialBlock, MaterialPropertyBlockEditor rightMaterialBlock,
            float radius, float margin, float trailLength, float maxAngle, float railGap, float squeezeLength)
        {
            InjectTurner(turner);
            InjectRoot(root);
            InjectLeftArrow(leftArrow);
            InjectRightArrow(rightArrow);
            InjectLeftRail(leftRail);
            InjectRightRail(rightRail);
            InjectLeftTrail(leftTrail);
            InjectRightTrail(rightTrail);
            InjectLeftMaterialBlock(leftMaterialBlock);
            InjectRightMaterialBlock(rightMaterialBlock);
            InjectRadius(radius);
            InjectMargin(margin);
            InjectTrailLength(trailLength);
            InjectMaxAngle(maxAngle);
            InjectRailGap(railGap);
            InjectSqueezeLength(squeezeLength);
        }

        public void InjectTurner(LocomotionTurnerInteractor turner)
        {
            _turner = turner;
        }

        public void InjectRoot(Transform root)
        {
            _root = root;
        }
        public void InjectLeftArrow(Renderer leftArrow)
        {
            _leftArrow = leftArrow;
        }
        public void InjectRightArrow(Renderer rightArrow)
        {
            _rightArrow = rightArrow;
        }
        public void InjectLeftRail(TubeRenderer leftRail)
        {
            _leftRail = leftRail;
        }
        public void InjectRightRail(TubeRenderer rightRail)
        {
            _rightRail = rightRail;
        }
        public void InjectLeftTrail(TubeRenderer leftTrail)
        {
            _leftTrail = leftTrail;
        }
        public void InjectRightTrail(TubeRenderer rightTrail)
        {
            _rightTrail = rightTrail;
        }
        public void InjectLeftMaterialBlock(MaterialPropertyBlockEditor leftMaterialBlock)
        {
            _leftMaterialBlock = leftMaterialBlock;
        }
        public void InjectRightMaterialBlock(MaterialPropertyBlockEditor rightMaterialBlock)
        {
            _rightMaterialBlock = rightMaterialBlock;
        }
        public void InjectRadius(float radius)
        {
            _radius = radius;
        }
        public void InjectMargin(float margin)
        {
            _margin = margin;
        }
        public void InjectTrailLength(float trailLength)
        {
            _trailLength = trailLength;
        }
        public void InjectMaxAngle(float maxAngle)
        {
            _maxAngle = maxAngle;
        }
        public void InjectRailGap(float railGap)
        {
            _railGap = railGap;
        }
        public void InjectSqueezeLength(float squeezeLength)
        {
            _squeezeLength = squeezeLength;
        }

        public void InjectOptionalBroadcaster(TurnerEventBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }
        public void InjectOptionalLookAt(Transform lookAt)
        {
            _lookAt = lookAt;
        }

        public void InjectOptionalProgress(IAxis1D progress)
        {
            _progress = progress as UnityEngine.Object;
            Progress = progress;
        }

        #endregion
    }
}
