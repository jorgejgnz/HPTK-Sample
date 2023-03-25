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
    public class CylinderSurfaceData : ICloneable
    {
        public object Clone()
        {
            CylinderSurfaceData clone = new CylinderSurfaceData();
            clone.startPoint = this.startPoint;
            clone.endPoint = this.endPoint;
            clone.arcOffset = this.arcOffset;
            clone.arcLength = this.arcLength;
            return clone;
        }

        public CylinderSurfaceData Mirror()
        {
            CylinderSurfaceData mirror = Clone() as CylinderSurfaceData;
            return mirror;
        }

        public Vector3 startPoint = new Vector3(0f, 0.1f, 0f);
        public Vector3 endPoint = new Vector3(0f, -0.1f, 0f);

        [Range(0f, 360f)]
        public float arcOffset = 0f;
        [Range(0f, 360f)]
        [FormerlySerializedAs("angle")]
        public float arcLength = 360f;

    }

    /// <summary>
    /// This type of surface defines a cylinder in which the grip pose is valid around an object.
    /// An angle can be used to constrain the cylinder and not use a full circle.
    /// The radius is automatically specified as the distance from the axis of the cylinder to the original grip position.
    /// </summary>
    [Serializable]
    public class CylinderGrabSurface : MonoBehaviour, IGrabSurface
    {
        [SerializeField]
        protected CylinderSurfaceData _data = new CylinderSurfaceData();

        /// <summary>
        /// Getter for the data-only version of this surface. Used so it can be stored when created
        /// at Play-Mode.
        /// </summary>
        public CylinderSurfaceData Data
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
        /// Direction from the axis of the cylinder to the original grip position.
        /// </summary>
        public Vector3 OriginalDir
        {
            get
            {
                if (_referencePoint == null)
                {
                    return Vector3.forward;
                }
                return Vector3.ProjectOnPlane(_referencePoint.transform.position - StartPoint, Direction).normalized;
            }
        }

        public Vector3 StartArcDir
        {
            get
            {
                return Quaternion.AngleAxis(ArcOffset, Direction) * OriginalDir;
            }
        }

        /// <summary>
        /// Direction from the axis of the cylinder to the maximum angle allowance.
        /// </summary>
        public Vector3 EndArcDir
        {
            get
            {
                return Quaternion.AngleAxis(ArcLength, Direction) * StartArcDir;
            }
        }

        /// <summary>
        /// Base cap of the cylinder, in world coordinates.
        /// </summary>
        public Vector3 StartPoint
        {
            get
            {
                if (_relativeTo != null)
                {
                    return _relativeTo.TransformPoint(_data.startPoint);
                }
                else
                {
                    return _data.startPoint;
                }
            }
            set
            {
                if (_relativeTo != null)
                {
                    _data.startPoint = _relativeTo.InverseTransformPoint(value);
                }
                else
                {
                    _data.startPoint = value;
                }
            }
        }

        /// <summary>
        /// End cap of the cylinder, in world coordinates.
        /// </summary>
        public Vector3 EndPoint
        {
            get
            {
                if (_relativeTo != null)
                {
                    return _relativeTo.TransformPoint(_data.endPoint);
                }
                else
                {
                    return _data.endPoint;
                }
            }
            set
            {
                if (_relativeTo != null)
                {
                    _data.endPoint = _relativeTo.InverseTransformPoint(value);
                }
                else
                {
                    _data.endPoint = value;
                }
            }
        }

        public float ArcOffset
        {
            get
            {
                return _data.arcOffset;
            }
            set
            {
                if (value != 0 && value % 360f == 0)
                {
                    _data.arcOffset = 360f;
                }
                else
                {
                    _data.arcOffset = Mathf.Repeat(value, 360f);
                }
            }
        }

        /// <summary>
        /// The maximum angle for the surface of the cylinder, starting from the original grip position.
        /// To invert the direction of the angle, swap the caps order.
        /// </summary>
        public float ArcLength
        {
            get
            {
                return _data.arcLength;
            }
            set
            {
                if (value != 0 && value % 360f == 0)
                {
                    _data.arcLength = 360f;
                }
                else
                {
                    _data.arcLength = Mathf.Repeat(value, 360f);
                }
            }
        }

        /// <summary>
        /// The generated radius of the cylinder.
        /// Represents the distance from the axis of the cylinder to the original grip position.
        /// </summary>
        public float Radius
        {
            get
            {
                if (_referencePoint == null)
                {
                    return 0f;
                }
                Vector3 start = StartPoint;
                Vector3 projectedPoint = start + Vector3.Project(_referencePoint.position - start, Direction);
                return Vector3.Distance(projectedPoint, _referencePoint.position);
            }
        }

        /// <summary>
        /// The direction of the central axis of the cylinder.
        /// </summary>
        public Vector3 Direction
        {
            get
            {
                Vector3 dir = (EndPoint - StartPoint);
                if (dir.sqrMagnitude == 0f)
                {
                    return _relativeTo ? _relativeTo.up : Vector3.up;
                }
                return dir.normalized;
            }
        }

        private float Height
        {
            get
            {
                return (EndPoint - StartPoint).magnitude;
            }
        }

        /// <summary>
        /// The rotation of the central axis of the cylinder.
        /// </summary>
        private Quaternion Rotation
        {
            get
            {
                if (_data.startPoint == _data.endPoint)
                {
                    return Quaternion.LookRotation(Vector3.forward);
                }
                return Quaternion.LookRotation(OriginalDir, Direction);
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
            Vector3 normal = Quaternion.Inverse(_relativeTo.rotation) * OriginalDir;
            Vector3 tangent = Quaternion.Inverse(_relativeTo.rotation) * Direction;

            return pose.MirrorPoseRotation(normal, tangent);
        }

        private Vector3 PointAltitude(Vector3 point)
        {
            Vector3 start = StartPoint;
            Vector3 projectedPoint = start + Vector3.Project(point - start, Direction);
            return projectedPoint;
        }


        public GrabPoseScore CalculateBestPoseAtSurface(in Pose targetPose, in Pose reference, out Pose bestPose, in PoseMeasureParameters scoringModifier)
        {
            return GrabPoseHelper.CalculateBestPoseAtSurface(targetPose, reference, out bestPose,
                scoringModifier, MinimalTranslationPoseAtSurface, MinimalRotationPoseAtSurface);
        }

        public IGrabSurface CreateMirroredSurface(GameObject gameObject)
        {
            CylinderGrabSurface surface = gameObject.AddComponent<CylinderGrabSurface>();
            surface.Data = _data.Mirror();
            return surface;
        }

        public IGrabSurface CreateDuplicatedSurface(GameObject gameObject)
        {
            CylinderGrabSurface surface = gameObject.AddComponent<CylinderGrabSurface>();
            surface.Data = _data;
            return surface;
        }

        protected Vector3 NearestPointInSurface(Vector3 targetPosition)
        {
            Vector3 start = StartPoint;
            Vector3 dir = Direction;
            Vector3 projectedVector = Vector3.Project(targetPosition - start, dir);

            if (projectedVector.magnitude > Height)
            {
                projectedVector = projectedVector.normalized * Height;
            }
            if (Vector3.Dot(projectedVector, dir) < 0f)
            {
                projectedVector = Vector3.zero;
            }

            Vector3 projectedPoint = StartPoint + projectedVector;
            Vector3 targetDirection = Vector3.ProjectOnPlane((targetPosition - projectedPoint), dir).normalized;
            //clamp of the surface
            float desiredAngle = Mathf.Repeat(Vector3.SignedAngle(StartArcDir, targetDirection, dir), 360f);
            if (desiredAngle > ArcLength)
            {
                if (Mathf.Abs(desiredAngle - ArcLength) >= Mathf.Abs(360f - desiredAngle))
                {
                    targetDirection = StartArcDir;
                }
                else
                {
                    targetDirection = EndArcDir;
                }
            }
            Vector3 surfacePoint = projectedPoint + targetDirection * Radius;
            return surfacePoint;
        }

        public bool CalculateBestPoseAtSurface(Ray targetRay, in Pose recordedPose, out Pose bestPose)
        {
            Vector3 lineToCylinder = StartPoint - targetRay.origin;

            float perpendiculiarity = Vector3.Dot(targetRay.direction, Direction);
            float rayToLineDiff = Vector3.Dot(lineToCylinder, targetRay.direction);
            float cylinderToLineDiff = Vector3.Dot(lineToCylinder, Direction);

            float determinant = 1f / (perpendiculiarity * perpendiculiarity - 1f);

            float lineOffset = (perpendiculiarity * cylinderToLineDiff - rayToLineDiff) * determinant;
            float cylinderOffset = (cylinderToLineDiff - perpendiculiarity * rayToLineDiff) * determinant;

            Vector3 pointInLine = targetRay.origin + targetRay.direction * lineOffset;
            Vector3 pointInCylinder = StartPoint + Direction * cylinderOffset;
            float distanceToSurface = Mathf.Max(Vector3.Distance(pointInCylinder, pointInLine) - Radius);
            if (distanceToSurface < Radius)
            {
                float adjustedDistance = Mathf.Sqrt(Radius * Radius - distanceToSurface * distanceToSurface);
                pointInLine -= targetRay.direction * adjustedDistance;
            }
            Vector3 surfacePoint = NearestPointInSurface(pointInLine);
            Pose desiredPose = new Pose(surfacePoint, recordedPose.rotation);
            bestPose = MinimalTranslationPoseAtSurface(desiredPose, recordedPose);

            return true;
        }

        protected Pose MinimalRotationPoseAtSurface(in Pose userPose, in Pose referencePose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion desiredRot = userPose.rotation;
            Quaternion baseRot = referencePose.rotation;
            Quaternion rotDif = (desiredRot) * Quaternion.Inverse(baseRot);
            Vector3 desiredDirection = (rotDif * Rotation) * Vector3.forward;
            Vector3 projectedDirection = Vector3.ProjectOnPlane(desiredDirection, Direction).normalized;
            Vector3 altitudePoint = PointAltitude(desiredPos);
            Vector3 surfacePoint = NearestPointInSurface(altitudePoint + projectedDirection * Radius);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint) * baseRot;
            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Pose MinimalTranslationPoseAtSurface(in Pose userPose, in Pose referencePose)
        {
            Vector3 desiredPos = userPose.position;
            Quaternion baseRot = referencePose.rotation;

            Vector3 surfacePoint = NearestPointInSurface(desiredPos);
            Quaternion surfaceRotation = CalculateRotationOffset(surfacePoint) * baseRot;

            return new Pose(surfacePoint, surfaceRotation);
        }

        protected Quaternion CalculateRotationOffset(Vector3 surfacePoint)
        {
            Vector3 recordedDirection = Vector3.ProjectOnPlane(_referencePoint.position - StartPoint, Direction);
            Vector3 desiredDirection = Vector3.ProjectOnPlane(surfacePoint - StartPoint, Direction);
            return Quaternion.FromToRotation(recordedDirection, desiredDirection);
        }

        #region Inject

        public void InjectAllCylinderSurface(CylinderSurfaceData data,
            Transform relativeTo, Transform gripPoint)
        {
            InjectData(data);
            InjectRelativeTo(relativeTo);
            InjectReferencePoint(gripPoint);
        }

        public void InjectData(CylinderSurfaceData data)
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
