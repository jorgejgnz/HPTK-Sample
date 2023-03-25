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
using UnityEngine.Assertions;

namespace Oculus.Interaction.Locomotion
{
    public class ArcRangeModifier : MonoBehaviour
    {
        [SerializeField, Interface(typeof(ITeleportArc))]
        private MonoBehaviour _teleportArc;
        private ITeleportArc TeleportArc { get; set; }

        [SerializeField]
        private Transform _referencePoint;
        [SerializeField]
        private Transform _targetPoint;

        [SerializeField]
        private AnimationCurve _rangeCurve = new AnimationCurve(
            new Keyframe(0f, 5f),
            new Keyframe(1f, 20f));

        protected bool _started;

        protected virtual void Awake()
        {
            TeleportArc = _teleportArc as ITeleportArc;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(TeleportArc, nameof(TeleportArc));
            this.AssertField(_referencePoint, nameof(_referencePoint));
            this.AssertField(_targetPoint, nameof(_targetPoint));
            this.AssertField(_rangeCurve, nameof(_rangeCurve));
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            Vector3 delta = _targetPoint.position - _referencePoint.position;
            delta.y = 0f;
            float distance = delta.magnitude;
            TeleportArc.MaxDistance = _rangeCurve.Evaluate(distance);
        }

        #region Inject
        public void InjectAllArcRangeModifier(ITeleportArc teleportArc,
            Transform referencePoint, Transform targetPoint,
            AnimationCurve rangeCurve)
        {
            InjectTeleportArc(teleportArc);
            InjectReferencePoint(referencePoint);
            InjectTargetPoint(targetPoint);
            InjectRangeCurve(rangeCurve);
        }

        public void InjectTeleportArc(ITeleportArc teleportArc)
        {
            _teleportArc = teleportArc as MonoBehaviour;
            TeleportArc = teleportArc;
        }

        public void InjectReferencePoint(Transform referencePoint)
        {
            _referencePoint = referencePoint;
        }

        public void InjectTargetPoint(Transform targetPoint)
        {
            _targetPoint = targetPoint;
        }

        public void InjectRangeCurve(AnimationCurve rangeCurve)
        {
            _rangeCurve = rangeCurve;
        }
        #endregion

    }
}
