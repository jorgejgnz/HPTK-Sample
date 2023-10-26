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
using UnityEditor;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(PlaneSurface))]
    public class PlaneSurfaceEditor : UnityEditor.Editor
    {
        private const int NUM_SEGMENTS = 80;
        private const float MAX_DISTANCE = 20f;

        private static readonly Color ColorFront = EditorConstants.PRIMARY_COLOR;
        private static readonly Color ColorBack = EditorConstants.ERROR_COLOR;

        private static float Interval => MAX_DISTANCE / NUM_SEGMENTS;

        private static Vector3[] _lines = new Vector3[NUM_SEGMENTS * 2 * 2];

        public void OnSceneGUI()
        {
            PlaneSurface plane = target as PlaneSurface;
            Draw(plane);
        }

        public void Draw(PlaneSurface plane)
        {
            Vector3 origin = plane.transform.position;
            Color color = ColorFront;

            if (SceneView.lastActiveSceneView?.camera != null)
            {
                Transform camTransform = SceneView.lastActiveSceneView.camera.transform;
                bool isBehind = Vector3.Dot(camTransform.forward, plane.transform.forward) < 0f;
                if (isBehind)
                {
                    color = ColorBack;
                }

                if (plane.ClosestSurfacePoint(camTransform.position, out SurfaceHit hit, 0))
                {
                    Vector3 hitDelta = PoseUtils.Delta(plane.transform, new Pose(hit.Point, plane.transform.rotation)).position;
                    hitDelta.x = Mathf.RoundToInt(hitDelta.x / Interval) * Interval;
                    hitDelta.y = Mathf.RoundToInt(hitDelta.y / Interval) * Interval;
                    hitDelta.z = 0f;
                    origin = PoseUtils.Multiply(plane.transform.GetPose(), new Pose(hitDelta, Quaternion.identity)).position;
                }

            }


            DrawLines(origin, plane.Normal, plane.transform.up, color);
            DrawLines(origin, plane.Normal, plane.transform.right, color);
        }

        private void DrawLines(in Vector3 origin,
                               in Vector3 normal,
                               in Vector3 direction,
                               in Color color)
        {
            Vector3 step = direction * Interval;
            Vector3 offsetOrigin = origin - step * NUM_SEGMENTS;
            int index = 0;
            for (int i = -NUM_SEGMENTS; i < NUM_SEGMENTS; ++i)
            {
                Vector3 cross = Vector3.Cross(normal, direction).normalized * MAX_DISTANCE;
                Vector3 start = offsetOrigin - cross;
                Vector3 end = offsetOrigin + cross;

                _lines[index++] = start;
                _lines[index++] = end;

                offsetOrigin += step;
            }

            Color prevColor = Handles.color;
            Handles.color = color;
            Handles.DrawLines(_lines);
            Handles.color = prevColor;
        }
    }
}
