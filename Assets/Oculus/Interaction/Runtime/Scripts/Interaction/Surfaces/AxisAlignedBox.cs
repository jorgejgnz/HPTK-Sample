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


using System;
using UnityEngine;

namespace Oculus.Interaction.Surfaces
{
    public class AxisAlignedBox : MonoBehaviour, ISurface
    {
        [SerializeField, Tooltip("Size of the axis-aligned box, default to mesh size")]
        private Vector3 _size = new Vector3(0.0f, 0.0f, 0.0f);

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

        public Transform Transform => transform;

        private Bounds Bounds
        {
            get
            {
                return new Bounds(transform.position, _size);
            }
        }

        protected void Start()
        {
            if (GetComponent<MeshFilter>())
            {
                // get the local size of mesh as the size of axis-aligned box; does not account for the scale of parent objects
                _size = Vector3.Scale(transform.localScale, GetComponent<MeshFilter>().mesh.bounds.size);
            }
            if (_size.magnitude == 0.0f)
            {
                // if no mesh is found, default to 0.1f
                _size = new Vector3(0.1f, 0.1f, 0.1f);
            }
            Size = _size;
        }

        private bool isWithinVolume(Vector3 point)
        {

            return point.x >= Bounds.min.x && point.x <= Bounds.max.x && point.y >= Bounds.min.y &&
                point.y <= Bounds.max.y && point.z >= Bounds.min.z && point.z <= Bounds.max.z;
        }

        // find closest surface and return corresponding index:
        // 0 - XMin, 1 - YMin, 2 - ZMin, 3 - XMax, 4 - YMax, 5 - ZMax
        private int findClosestBoxSide(Vector3 point)
        {
            Vector3 pointRef = transform.position - point;
            Vector3 halfSize = Bounds.extents;
            float[] sideDist = new float[] {halfSize.x - pointRef.x,
                                            halfSize.y - pointRef.y,
                                            halfSize.z - pointRef.z,
                                            halfSize.x + pointRef.x,
                                            halfSize.y + pointRef.y,
                                            halfSize.z + pointRef.z };
            float value = float.PositiveInfinity;
            int index = -1;
            for (int i = 0; i < sideDist.Length; i++)
            {
                if (sideDist[i] < value)
                {
                    index = i;
                    value = sideDist[i];
                }
            }
            return index;
        }

        private Vector3 ClosestSurfaceNormal(Vector3 point, int side)
        {
            int closestSide = -1;
            if (0 <= side && side >= 5)
            {
                closestSide = side;
            } else
            {
                closestSide = findClosestBoxSide(point);
            }
            switch (closestSide)
            {
                case 0:
                    return new Vector3(-1.0f, 0.0f, 0.0f);

                case 1:
                    return new Vector3(0.0f, -1.0f, 0.0f);

                case 2:
                    return new Vector3(0.0f, 0.0f, -1.0f);

                case 3:
                    return new Vector3(1.0f, 0.0f, 0.0f);

                case 4:
                    return new Vector3(0.0f, 1.0f, 0.0f);

                case 5:
                    return new Vector3(0.0f, 0.0f, 1.0f);
            }
            return new Vector3(0, 0, 0);
        }

        public bool ClosestSurfacePoint(in Vector3 point, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();
            Vector3 boundPoint = Vector3.Min(Vector3.Max(point, Bounds.min), Bounds.max);
            int closestSide = findClosestBoxSide(point);
            hit.Normal = ClosestSurfaceNormal(point, closestSide);
            if (!isWithinVolume(point))
            {
                // if boundPoint is inside the Axis-Aligned box, push to boundary
                switch (closestSide)
                {
                    case 0:
                        boundPoint.x = Bounds.min.x;
                        break;

                    case 1:
                        boundPoint.y = Bounds.min.y;
                        break;

                    case 2:
                        boundPoint.z = Bounds.min.z;
                        break;

                    case 3:
                        boundPoint.x = Bounds.max.x;
                        break;

                    case 4:
                        boundPoint.y = Bounds.max.y;
                        break;

                    case 5:
                        boundPoint.z = Bounds.max.z;
                        break;
                }
            }

            hit.Point = boundPoint;
            hit.Distance = Vector3.Distance(hit.Point, point);
            if (maxDistance > 0 && hit.Distance > maxDistance)
            {
                return false;
            }
            return true;
        }

        public bool Raycast(in Ray ray, out SurfaceHit hit, float maxDistance)
        {
            hit = new SurfaceHit();

            Vector3 dirInv = new Vector3(1.0f / ray.direction.x, 1.0f / ray.direction.y, 1.0f / ray.direction.z);

            Vector3 vecMin = Vector3.Scale(Bounds.min - ray.origin, dirInv);
            Vector3 vecMax = Vector3.Scale(Bounds.max - ray.origin, dirInv);

            float tmin = Mathf.Max(Mathf.Max(Mathf.Min(vecMin.x, vecMax.x), Mathf.Min(vecMin.y, vecMax.y)), Mathf.Min(vecMin.z, vecMax.z));
            float tmax = Mathf.Min(Mathf.Min(Mathf.Max(vecMin.x, vecMax.x), Mathf.Max(vecMin.y, vecMax.y)), Mathf.Max(vecMin.z, vecMax.z));

            if (tmax < 0)
            {
                hit.Distance = tmax;
                return false;
            }

            if (tmin > tmax)
            {
                hit.Distance = tmax;
                return false;
            }

            hit.Distance = tmin;

            if (maxDistance > 0 && hit.Distance > maxDistance)
            {
                return false;
            }

            hit.Point = ray.origin + ray.direction * tmin;
            hit.Normal = ClosestSurfaceNormal(hit.Point, -1);
            return true;
        }
    }
}
