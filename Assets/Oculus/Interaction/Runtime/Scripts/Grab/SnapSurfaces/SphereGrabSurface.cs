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
using System;
using UnityEngine.Serialization;

namespace Oculus.Interaction.Grab.GrabSurfaces
{
    [Serializable]
    public class SphereGrabSurfaceData : ICloneable
    {
        public object Clone()
        {
            SphereGrabSurfaceData clone = new SphereGrabSurfaceData();
            clone.centre = this.centre;
            return clone;
        }

        public SphereGrabSurfaceData Mirror()
        {
            SphereGrabSurfaceData mirror = Clone() as SphereGrabSurfaceData;
            return mirror;
        }

        public Vector3 centre;
    }

    /// <summary>
    /// Specifies an entire sphere around an object in which the grip point is valid.
    ///
    /// One of the main advantages of spheres is that the rotation of the hand pose does
    /// not really matters, as it will always fit the surface correctly.
    /// </summary>
    [Serializable]
    public class SphereGrabSurface : MonoBehaviour, IGrabSurface
    {

        [SerializeField]
        protected SphereGrabSurfaceData _data = new SphereGrabSurfaceData();

        /// <summary>
        /// Getter for the data-only version of this surface. Used so it can be stored when created
        /// at Play-Mode.
        /// </summary>
        public SphereGrabSurfaceData Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }

        [SerializeField]
        private Transform _relativeTo;

        [SerializeField]
        [FormerlySerializedAs("_gripPoint")]
        private Transform _referencePoint;

        /// <summary>
        /// The center of the sphere in world coordinates.
        /// </summary>
        public Vector3 Centre
        {
            get
            {
                if (_relativeTo != null)
                {
                    return _relativeTo.TransformPoint(_data.centre);
                }
                else
                {
                    return _data.centre;
                }
            }
            set
            {
                if (_relativeTo != null)
                {
                    _data.centre = _relativeTo.InverseTransformPoint(value);
                }
                else
                {
                    _data.centre = value;
                }
            }
        }

        /// <summary>
        /// The radius of the sphere, this is automatically calculated as the distance between
        /// the center and the original grip pose.
        /// </summary>
        public float Radius
        {
            get
            {
                if (_referencePoint == null)
                {
                    return 0f;
                }
                return Vector3.Distance(Centre, _referencePoint.position);
            }
        }

        /// <summary>
        /// The direction of the sphere, measured from the center to the original grip position.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                return (_referencePoint.position - Centre).normalized;
            }
        }

        /// <summary>
        /// The rotation of the sphere from the recorded grip position.
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                return Quaternion.LookRotation(Direction, _referencePoint.forward);
            }
        }

        #region editor events
        private void Reset()
        {
            _referencePoint = this.transform;
            _relativeTo = this.GetComponentInParent<Rigidbody>()?.transform;
        }
        #endregion

        protected virtual void Start()
        {
            this.AssertField(_relativeTo, nameof(_relativeTo));
            this.AssertField(_referencePoint, nameof(_referencePoint));
            this.AssertField(_data, nameof(_data));
        }

        public Pose MirrorPose(in Pose pose)
        {
            Vector3 normal = Quaternion.Inverse(_relativeTo.rotation) * Direction;
            Vector3 tangent = Vector3.Cross(normal, Vector3.up);
            return pose.MirrorPoseRotation(normal, tangent);
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose recordedPose, out Pose bestPose)
        {
            Vector3 projection = Vector3.Project(Centre - targetRay.origin, targetRay.direction);
            Vector3 nearestCentre = targetRay.origin + projection;
            float distanceToSurface = Mathf.Max(Vector3.Distance(Centre, nearestCentre) - Radius);
            if (distanceToSurface < Radius)
            {
                float adjustedDistance = Mathf.Sqrt(Radius * Radius - distanceToSurface * distanceToSurface);
                nearestCentre -= targetRay.direction * adjustedDistance;
            }


            Vector3 surfacePoint = NearestPointInSurface(nearestCentre);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, recordedPose);
            return true;
        }

        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose, in PoseMeasureParameters scoringModifier)
        {
            return GrabPoseHelper.CalculateBestPoseAtSurface(targetPose, reference, out bestPose,
                scoringModifier, MinimalTranslationPoseAtSurface, MinimalRotationPoseAtSurface);
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            SphereGrabSurface surface = gameObject.AddComponent<SphereGrabSurface>();
            surface.Data = _data.Mirror();
            return surface;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            SphereGrabSurface surface = gameObject.AddComponent<SphereGrabSurface>();
            surface.Data = _data;
            return surface;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - Centre).normalized;
            return Centre + direction * Radius;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, in Pose referencePose)
        {
            Quaternion rotCorrection = userPose.rotation * Quaternion.Inverse(referencePose.rotation);
            Vector3 correctedDir = rotCorrection * Direction;
            Vector3 surfacePoint = NearestPointInSurface(Centre + correctedDir * Radius);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, referencePose.rotation, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, in Pose referencePose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;
            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = RotationAtPoint(surfacePoint, baseRot, userPose.rotation);
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion RotationAtPoint(Vector3 surfacePoint, Quaternion baseRot, Quaternion desiredRotation)
        {
            Vector3 desiredDirection = (surfacePoint - Centre).normalized;
            Quaternion targetRotation = Quaternion.FromToRotation(Direction, desiredDirection) * baseRot;
            Vector3 targetProjected = Vector3.ProjectOnPlane(targetRotation * Vector3.forward, desiredDirection).normalized;
            Vector3 desiredProjected = Vector3.ProjectOnPlane(desiredRotation * Vector3.forward, desiredDirection).normalized;
            Quaternion rotCorrection = Quaternion.FromToRotation(targetProjected, desiredProjected);
            return rotCorrection * targetRotation;
        }

        #region Inject

        public void InjectAllSphereSurface(SphereGrabSurfaceData data,
            Transform relativeTo, Transform gripPoint)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
            InjectReferencePoint(gripPoint);
        }

        public void InjectData(SphereGrabSurfaceData data)
        {
            _data = data;
        }

        public void InjectRelativeTo(Transform relativeTo)
        {
            _relativeTo = relativeTo;
        }

        public void InjectReferencePoint(Transform referencePoint)
        {
            _referencePoint = referencePoint;
        }

        #endregion
    }
}
