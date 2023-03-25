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

namespace Oculus.Interaction.Grab
{
    public static class GrabPoseHelper
    {
        public delegate Pose PoseCalculator(in Pose desiredPose, in Pose referencePose);

        /// <summary>
        /// Finds the best pose comparing the one that requires the minimum rotation
        /// and minimum translation.
        /// </summary>
        /// <param name="desiredPose">Pose to measure from.</param>
        /// <param name="referencePose">Reference pose of the surface.</param>
        /// <param name="bestPose">Nearest pose to the desired one at the surface.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="minimalTranslationPoseCalculator">Delegate to calculate the nearest, by position, pose at a surface.</param>
        /// <param name="minimalRotationPoseCalculator">Delegate to calculate the nearest, by rotation, pose at a surface.</param>
        /// <returns>The score, normalized, of the best pose.</returns>
        public static GrabPoseScore CalculateBestPoseAtSurface(in Pose desiredPose, in Pose referencePose, out Pose bestPose,
            in PoseMeasureParameters scoringModifier,
            PoseCalculator minimalTranslationPoseCalculator, PoseCalculator minimalRotationPoseCalculator)
        {
            if (scoringModifier.PositionRotationWeight == 1f)
            {
                bestPose = minimalRotationPoseCalculator(desiredPose, referencePose);
                return new GrabPoseScore(desiredPose, referencePose, 1f);
            }

            if (scoringModifier.PositionRotationWeight == 0f)
            {
                bestPose = minimalTranslationPoseCalculator(desiredPose, referencePose);
                return new GrabPoseScore(desiredPose, referencePose, 0f);
            }

            Pose minimalTranslationPose = minimalTranslationPoseCalculator(desiredPose, referencePose);
            Pose minimalRotationPose = minimalRotationPoseCalculator(desiredPose, referencePose);
            bestPose = SelectBestPose(minimalRotationPose, minimalTranslationPose,
                desiredPose, scoringModifier, out GrabPoseScore bestScore);
            return bestScore;

        }

        /// <summary>
        /// Compares two poses to a reference and returns the most similar one
        /// </summary>
        /// <param name="poseA">First Pose to compare with the reference.</param>
        /// <param name="poseB">Second Pose to compare with the reference.</param>
        /// <param name="reference">Reference pose to measure from.</param>
        /// <param name="scoringModifier">Modifiers for the score based in rotation and distance.</param>
        /// <param name="bestScore">Out value with the score of the best pose.</param>
        /// <returns>The most similar pose to reference out of the poses</returns>
        public static Pose SelectBestPose(in Pose poseA, in Pose poseB, in Pose reference, PoseMeasureParameters scoringModifier,
            out GrabPoseScore bestScore)
        {
            GrabPoseScore poseAScore = new GrabPoseScore(reference, poseA,
                scoringModifier.PositionRotationWeight);
            GrabPoseScore poseBScore = new GrabPoseScore(reference, poseB,
                scoringModifier.PositionRotationWeight);

            if (poseAScore.IsBetterThan(poseBScore))
            {
                bestScore = poseAScore;
                return poseA;
            }
            else
            {
                bestScore = poseBScore;
                return poseB;
            }
        }

        public static GrabPoseScore CollidersScore(Vector3 position, Collider[] colliders,
            out Vector3 hitPoint)
        {
            GrabPoseScore bestScore = GrabPoseScore.Max;
            GrabPoseScore score;
            hitPoint = position;
            foreach (Collider collider in colliders)
            {
                bool isPointInsideCollider = Collisions.IsPointWithinCollider(position, collider);
                Vector3 measuringPoint = isPointInsideCollider ? collider.bounds.center : collider.ClosestPoint(position);

                score = new GrabPoseScore(position, measuringPoint,
                    isPointInsideCollider);

                if (score.IsBetterThan(bestScore))
                {
                    hitPoint = isPointInsideCollider ? position : measuringPoint;
                    bestScore = score;
                }
            }

            return bestScore;
        }
    }
}
