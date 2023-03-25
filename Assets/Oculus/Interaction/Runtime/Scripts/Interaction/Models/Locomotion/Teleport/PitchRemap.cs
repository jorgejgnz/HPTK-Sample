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

namespace Oculus.Interaction
{
    /// <summary>
    /// Adjust the pitch of a transform using a Curve.
    /// A vector pointing straight up has +90 degrees pitch,
    /// and a vector pointing down has a -90 degrees pitch.
    /// </summary>
    public class PitchRemap : MonoBehaviour
    {
        [SerializeField]
        private Transform _referencePoint;

        [SerializeField]
        private AnimationCurve _remapCurve = new AnimationCurve(
            new Keyframe(-90f, -90f),
            new Keyframe(+90f, +90f));

        private readonly Vector3 _up = Vector3.up;

        protected bool _started;

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(_referencePoint, nameof(_referencePoint));
            this.AssertField(_remapCurve, nameof(_remapCurve));
            this.EndStart(ref _started);
        }

        protected virtual void Update()
        {
            Vector3 dir = RemapPitch();
            if (dir.sqrMagnitude != 0)
            {
                this.transform.rotation = Quaternion.LookRotation(dir, this.transform.up);
            }
        }

        private Vector3 RemapPitch()
        {
            Vector3 direction = this.transform.forward;
            Vector3 forward = Vector3.ProjectOnPlane(this.transform.position - _referencePoint.position, _up).normalized;

            Vector3 flatDir = Vector3.ProjectOnPlane(direction, _up).normalized;
            Vector3 right = Vector3.Cross(flatDir, _up);
            if (Vector3.Dot(flatDir, forward) < 0)
            {
                flatDir = -flatDir;
            }
            float angle = Vector3.SignedAngle(flatDir, direction, right);
            angle = _remapCurve.Evaluate(angle);
            Quaternion delta = Quaternion.AngleAxis(angle, right);

            return delta * flatDir;
        }

        #region Inject
        public void InjectAllPitchRemap(Transform referencePoint,
            AnimationCurve remapCurve)
        {
            InjectReferencePoint(referencePoint);
            InjectRemapCurve(remapCurve);
        }

        public void InjectReferencePoint(Transform referencePoint)
        {
            _referencePoint = referencePoint;
        }

        public void InjectRemapCurve(AnimationCurve remapCurve)
        {
            _remapCurve = remapCurve;
        }
        #endregion
    }
}
