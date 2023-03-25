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

using Oculus.Interaction.Editor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Oculus.Interaction.Grab.GrabSurfaces.Editor
{
    [CustomEditor(typeof(SphereGrabSurface))]
    [CanEditMultipleObjects]
    public class SphereGrabSurfaceEditor : UnityEditor.Editor
    {
        private SphereBoundsHandle _sphereHandle = new SphereBoundsHandle();
        private SphereGrabSurface _surface;
        private SerializedProperty _relativeToProperty;

        private void OnEnable()
        {
            _sphereHandle.SetColor(EditorConstants.PRIMARY_COLOR);
            _sphereHandle.midpointHandleDrawFunction = null;

            _surface = (target as SphereGrabSurface);
            _relativeToProperty = serializedObject.FindProperty("_relativeTo");
        }

        public void OnSceneGUI()
        {
            DrawCentre(_surface);
            Handles.color = Color.white;
            DrawSphereEditor(_surface);

            if (Event.current.type == EventType.Repaint)
            {
                DrawSurfaceVolume(_surface);
            }
        }

        private void DrawCentre(SphereGrabSurface surface)
        {
            EditorGUI.BeginChangeCheck();
            Transform relative = _relativeToProperty.objectReferenceValue as Transform ?? surface.transform;
            Quaternion handleRotation = relative.rotation;
            Vector3 centrePosition = Handles.PositionHandle(surface.Centre, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(surface, "Change Centre Sphere Position");
                surface.Centre = centrePosition;
            }
        }

        private void DrawSurfaceVolume(SphereGrabSurface surface)
        {
            Handles.color = EditorConstants.PRIMARY_COLOR;
            Vector3 startLine = surface.Centre;
            Vector3 endLine = startLine + surface.Rotation * Vector3.forward * surface.Radius;
            Handles.DrawDottedLine(startLine, endLine, 5);
        }
        private void DrawSphereEditor(SphereGrabSurface surface)
        {
            _sphereHandle.radius = surface.Radius;
            _sphereHandle.center = surface.Centre;

            EditorGUI.BeginChangeCheck();
            _sphereHandle.DrawHandle();
        }
    }
}
