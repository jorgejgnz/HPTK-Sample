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

namespace Oculus.Interaction.Samples
{
    public class LocomotionTutorialTurnVisual : MonoBehaviour
    {
        [SerializeField, Range(-1f, 1f)]
        private float _value;
        [SerializeField, Range(0f, 1f)]
        private float _progress;

        [Header("Visual renderers")]
        [SerializeField]
        private Renderer _leftArrow;
        [SerializeField]
        private Renderer _rightArrow;
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

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_leftTrail, nameof(_leftTrail));
            this.AssertField(_rightTrail, nameof(_rightTrail));
            this.AssertField(_leftArrow, nameof(_leftArrow));
            this.AssertField(_rightArrow, nameof(_rightArrow));

            this.AssertField(_leftMaterialBlock, nameof(_leftMaterialBlock));
            this.AssertField(_rightMaterialBlock, nameof(_rightMaterialBlock));

            InitializeVisuals();
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            _leftTrail.enabled = true;
            _rightTrail.enabled = true;
            _leftArrow.enabled = true;
            _rightArrow.enabled = true;
        }

        protected virtual void OnDisable()
        {
            _leftTrail.enabled = false;
            _rightTrail.enabled = false;
            _leftArrow.enabled = false;
            _rightArrow.enabled = false;
        }

        protected virtual void Update()
        {
            UpdateArrows();
            UpdateColors();
        }

        private void InitializeVisuals()
        {
            TubePoint[] trailPoints = InitializeSegment(new Vector2(_margin, _maxAngle + _squeezeLength));
            _leftTrail.RenderTube(trailPoints, Space.Self);
            _rightTrail.RenderTube(trailPoints, Space.Self);
        }

        private void UpdateArrows()
        {
            float value = _value;
            float angle = Mathf.Lerp(0f, _maxAngle, Mathf.Abs(value));
            bool isLeft = value < 0;
            bool isRight = value > 0;
            bool follow = false;
            float squeeze = Mathf.Lerp(0f, _squeezeLength, _progress);

            angle = Mathf.Max(angle, _trailLength);

            UpdateArrowPosition(isRight ? angle + squeeze : _trailLength, _rightArrow.transform);
            RotateTrail(follow && isRight ? angle - _trailLength : 0f, _rightTrail);
            UpdateTrail(isRight ? (follow ? _trailLength : angle) + squeeze : _trailLength, _rightTrail);

            UpdateArrowPosition(isLeft ? -angle - squeeze : -_trailLength, _leftArrow.transform);
            RotateTrail(follow && isLeft ? -angle + _trailLength : 0f, _leftTrail);
            UpdateTrail(isLeft ? (follow ? _trailLength : angle) + squeeze : _trailLength, _leftTrail);
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

        private void UpdateColors()
        {
            bool isSelection = Mathf.Abs(_progress) >= 1f;
            bool isLeft = _value < 0;
            bool isRight = _value > 0;

            Color activeColor = isSelection? _highligtedColor : _enabledColor;

            _leftMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, isLeft ? activeColor : _disabledColor);
            _rightMaterialBlock.MaterialPropertyBlock.SetColor(_colorShaderPropertyID, isRight ? activeColor : _disabledColor);

            _leftMaterialBlock.UpdateMaterialPropertyBlock();
            _rightMaterialBlock.UpdateMaterialPropertyBlock();
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

    }
}
