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
    /// <summary>
    /// Defines an arc in 3D space as a series of straight segments
    /// </summary>
    public interface ITeleportArc
    {
        /// <summary>
        /// Max distance in the Y plane this arc can reach
        /// </summary>
        float MaxDistance { get; set; }
        /// <summary>
        /// Max number of points that define the arc
        /// </summary>
        int ArcPointsCount { get; }

        /// <summary>
        /// Calculates the position N vertex of the arc
        /// </summary>
        /// <param name="origin">The origin of the arc,
        /// the position defines the start point and forward establishes the direction</param>
        /// <param name="index">The N vertex of the arc been queried.</param>
        /// <returns>The position of the arc at the index-th point</returns>
        Vector3 PointAtIndex(Pose origin, int index);
    }
}
