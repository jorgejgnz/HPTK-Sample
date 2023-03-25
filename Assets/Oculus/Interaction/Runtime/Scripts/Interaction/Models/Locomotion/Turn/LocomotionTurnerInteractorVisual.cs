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
        [SerializeField]
        private LocomotionTurnerInteractor _turner;

        [SerializeField, Optional, Interface(typeof(IAxis1D))]
        private MonoBehaviour _progress;
        private IAxis1D Progress;

        [SerializeField, Interface(typeof(IActiveState)), Optional]
        private MonoBehaviour _highlight;
        private IActiveState Highlight;

        [SerializeField]
        private Transform _ring;
        [SerializeField]
        private Transform _pointer;

        [SerializeField]
        private float _signalsDistance = 0.1f;
        public float SignalsDistance
        {
            get
            {
                return _signalsDistance;
            }
            set
            {
                _signalsDistance = value;
            }
        }

        [SerializeField, Optional]
        private Renderer _ringRenderer;
        [SerializeField, Optional]
        private Renderer _pointerRenderer;

        private Vector3 _originalPointerScale;
        private bool _highlighted;

        private static readonly Vector3 TINY_SCALE_FACTOR = new Vector3(0.4f, 0.4f, 0.4f);
        private static readonly Quaternion RING_ROTATION = Quaternion.Euler(0f, 90f, 180f);

        private static readonly int _highlightShaderID = Shader.PropertyToID("_Highlight");

        protected bool _started;

        protected virtual void Awake()
        {
            Progress = _progress as IAxis1D;
            Highlight = _highlight as IActiveState;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_turner, nameof(_turner));
            this.AssertField(_ring, nameof(_ring));
            this.AssertField(_pointer, nameof(_pointer));

            _originalPointerScale = _pointer.localScale;

            if (_ringRenderer == null)
            {
                _ring.TryGetComponent(out _ringRenderer);
            }

            if (_pointerRenderer == null)
            {
                _pointer.TryGetComponent(out _pointerRenderer);
            }
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
                _ringRenderer.enabled = false;
                _pointerRenderer.enabled = false;
            }
            else
            {
                _ringRenderer.enabled = true;
                _pointerRenderer.enabled = true;

            }
        }

        private void HandleTurnerPostprocessed()
        {
            UpdatePoses();
            UpdateScale();
            UpdateHighlight();
        }

        private void UpdatePoses()
        {
            Pose origin = _turner.MidPoint;
            float offset = _turner.Value();

            _ring.SetPositionAndRotation(
                origin.position,
                origin.rotation * RING_ROTATION);

            _pointer.SetPositionAndRotation(
                _turner.Origin.position,
                Quaternion.LookRotation(offset < 0 ? -origin.right : origin.right, origin.up));
        }

        private void UpdateScale()
        {
            if (Highlight != null && Highlight.Active)
            {
                _pointer.localScale = _originalPointerScale;
            }
            else
            {
                float pointerScaleFactor = Progress != null ? Progress.Value() : 1f;
                _pointer.localScale = Vector3.Lerp(_originalPointerScale, TINY_SCALE_FACTOR, pointerScaleFactor);
            }
        }

        private void UpdateHighlight()
        {
            if (Highlight == null
                || Highlight.Active == _highlighted)
            {
                return;
            }

            _highlighted = Highlight.Active;
            float highlightFactor = _highlighted ? 1f : 0f;
            _ringRenderer.material.SetFloat(_highlightShaderID, highlightFactor);
            _pointerRenderer.material.SetFloat(_highlightShaderID, highlightFactor);
        }

        #region Inject
        public void InjectAllLocomotionTurnerInteractorVisual(LocomotionTurnerInteractor turner, Transform ring, Transform pointer)
        {
            InjectTurner(turner);
            InjectRing(ring);
            InjectPointer(pointer);
        }

        public void InjectTurner(LocomotionTurnerInteractor turner)
        {
            _turner = turner;
        }

        public void InjectRing(Transform leftRing)
        {
            _ring = leftRing;
        }

        public void InjectPointer(Transform pointer)
        {
            _pointer = pointer;
        }

        public void InjectOptionalHighlight(IActiveState highlight)
        {
            _highlight = highlight as MonoBehaviour;
            Highlight = highlight;
        }

        public void InjectOptionalRingRenderer(Renderer ringRenderer)
        {
            _ringRenderer = ringRenderer;
        }

        public void InjectOptionalPointerRenderer(Renderer pointerRenderer)
        {
            _pointerRenderer = pointerRenderer;
        }

        public void InjectOptionalProgress(IAxis1D progress)
        {
            _progress = progress as MonoBehaviour;
            Progress = progress;
        }
        #endregion
    }
}
