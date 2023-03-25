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

namespace Oculus.Interaction.Locomotion
{
    public class TeleportArcGravity : MonoBehaviour, ITeleportArc
    {
        [SerializeField]
        private float _maxDistance = 10;
        public float MaxDistance
        {
            get
            {
                return _maxDistance;
            }
            set
            {
                _maxDistance = value;
            }
        }

        [SerializeField]
        private float _gravityModifier = 2.3f;
        public float GravityModifier
        {
            get
            {
                return _gravityModifier;
            }
            set
            {
                _gravityModifier = value;
            }
        }

        [SerializeField, Min(2)]
        private int _arcPointsCount = 30;
        public int ArcPointsCount
        {
            get
            {
                return _arcPointsCount;
            }
            set
            {
                _arcPointsCount = value;
            }
        }

        private static readonly Vector3 GRAVITY = new Vector3(0f, -9.81f, 0f);
        private static readonly float GROUND_MARGIN = 2f;

        public Vector3 PointAtIndex(Pose origin, int index)
        {
            float t = index / (_arcPointsCount - 1f);
            return EvaluateGravityArc(origin, t);
        }

        private Vector3 EvaluateGravityArc(Pose origin, float t)
        {
            Vector3 point = origin.position
                + origin.forward * _maxDistance * t
                + 0.5f * t * t * GRAVITY * _gravityModifier;
            if (t >= 1f
                && point.y > origin.position.y - GROUND_MARGIN)
            {
                point.y = origin.position.y - GROUND_MARGIN;
            }
            return point;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Pose origin = this.transform.GetPose();
            Vector3 prevPoint = PointAtIndex(origin, 0);
            Gizmos.color = new Color(0f, 1f, 1f, 1f);
            for (int i = 1; i < ArcPointsCount; i++)
            {
                Vector3 point = PointAtIndex(origin, i);
                Gizmos.DrawLine(prevPoint, point);
                prevPoint = point;
            }
        }
#endif
    }
}
