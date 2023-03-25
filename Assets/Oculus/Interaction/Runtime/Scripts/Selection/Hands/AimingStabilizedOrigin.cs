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
    /// Updates a transform to a pose specified by the
    /// position of the AimingPoint and the Forward
    /// established by the StabilizingPoint to AimingPoint direction.
    /// </summary>
    public class AimingStabilizedOrigin : MonoBehaviour
    {
        [SerializeField]
        private Transform _stabilizingPoint;
        [SerializeField]
        private Transform _aimingPoint;
        [SerializeField]
        private AnimationCurve _stabilizationMixCurve;

        protected virtual void Start()
        {
            this.AssertField(_stabilizingPoint, nameof(_stabilizingPoint));
            this.AssertField(_aimingPoint, nameof(_aimingPoint));
        }

        public void Update()
        {
            Vector3 difference = _aimingPoint.position - _stabilizingPoint.position;
            Vector3 direction = difference.normalized;
            Quaternion stabilizedRotation = Quaternion.LookRotation(direction);

            float mixing = Vector3.Dot(direction, Vector3.up) * 0.5f + 0.5f;
            mixing = _stabilizationMixCurve.Evaluate(mixing);
            Quaternion mixedRotation = Quaternion.Lerp(_aimingPoint.rotation, stabilizedRotation, mixing);
            this.transform.SetPositionAndRotation(_aimingPoint.position, mixedRotation);
        }

        #region Inject

        public void InjectAllAimingStabilizedOrigin(Transform stabilizingPoint,
            Transform aimingPoint)
        {
            InjectStabilizingPoint(stabilizingPoint);
            InjectAimingPoint(aimingPoint);
        }

        public void InjectStabilizingPoint(Transform stabilizingPoint)
        {
            _stabilizingPoint = stabilizingPoint;
        }

        public void InjectAimingPoint(Transform aimingPoint)
        {
            _aimingPoint = aimingPoint;
        }

        #endregion
    }
}
