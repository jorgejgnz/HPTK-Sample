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
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction
{
    /// <summary>
    /// Tools for working Surfaces on Interactables
    /// </summary>
    public static class SurfaceUtils
    {
        /// <summary>
        /// The distance above a surface along the closest normal.
        /// Returns 0 for where the sphere touches the surface along the normal.
        /// </summary>
        /// <param name="interactable"> The PokeInteractable surface to test</param>
        /// <param name="point"> The origin point to compute distance from the surface</param>
        /// <param name="radius"> The radius of the sphere positioned at the origin point</param>
        public static float ComputeDistanceAbove(ISurfacePatch surfacePatch, Vector3 point, float radius)
        {
            surfacePatch.BackingSurface.ClosestSurfacePoint(point, out SurfaceHit hit);
            Vector3 surfaceToPoint = point - hit.Point;
            return Vector3.Dot(surfaceToPoint, hit.Normal) - radius;
        }

        /// <summary>
        /// The distance to a surface along the tangent.
        /// </summary>
        /// <param name="interactable"> The PokeInteractable surface to test</param>
        /// <param name="point"> The origin point to compute distance from the surface</param>
        /// <param name="radius"> The radius of the sphere positioned at the origin point</param>
        public static float ComputeTangentDistance(ISurfacePatch surfacePatch, Vector3 point, float radius)
        {
            surfacePatch.ClosestSurfacePoint(point, out SurfaceHit patchHit);
            surfacePatch.BackingSurface.ClosestSurfacePoint(point, out SurfaceHit backingHit);
            Vector3 proximityToPoint = point - patchHit.Point;
            Vector3 projOnNormal = Vector3.Dot(proximityToPoint, backingHit.Normal) *
                backingHit.Normal;
            Vector3 lateralVec = proximityToPoint - projOnNormal;
            return lateralVec.magnitude - radius;
        }

        /// <summary>
        /// The distance below a surface along the closest normal. always positive.
        /// </summary>
        /// <param name="interactable"> the pokeinteractable surface to test</param>
        /// <param name="point"> the origin point to compute distance from the surface</param>
        /// <param name="radius"> the radius of the sphere positioned at the origin point</param>
        public static float ComputeDepth(ISurfacePatch surfacePatch, Vector3 point, float radius)
        {
            return Mathf.Max(0f, -ComputeDistanceAbove(surfacePatch, point, radius));
        }

        /// <summary>
        /// The distance from the closest point as computed by the proximity field and surface.
        /// Returns the distance to the point without taking into account the surface normal.
        /// </summary>
        /// <param name="interactable"> the pokeinteractable surface to test</param>
        /// <param name="point"> the origin point to compute distance from the surface</param>
        /// <param name="radius"> the radius of the sphere positioned at the origin point</param>
        public static float ComputeDistanceFrom(ISurfacePatch surfacePatch, Vector3 point, float radius)
        {
            surfacePatch.ClosestSurfacePoint(point, out SurfaceHit hit);
            Vector3 surfaceToPoint = point - hit.Point;
            return surfaceToPoint.magnitude - radius;
        }
    }
}
