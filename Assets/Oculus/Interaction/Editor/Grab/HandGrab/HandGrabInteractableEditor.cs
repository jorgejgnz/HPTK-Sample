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

using Oculus.Interaction.HandGrab.Visuals;
using Oculus.Interaction.Input;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.HandGrab.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(HandGrabInteractable))]
    public partial class HandGrabInteractableEditor : UnityEditor.Editor
    {
        private HandGrabInteractable _target;

        private HandGhostProvider _ghostVisualsProvider;
        private HandGhost _handGhost;
        private HandPose _ghostHandPose = new HandPose();
        private Handedness _lastHandedness;

        private const float _poseRectWidth = 40f;
        private const float _leftMargin = 15;
        private const float _rightMarging = 65f;

        private const float _minScale = 0.5f;
        private const float _maxScale = 1.5f;

        private float _selectedScale = (_maxScale + _minScale) / 2;
        private HashSet<float> _scalesSet = new HashSet<float>();

        private void Awake()
        {
            _target = target as HandGrabInteractable;
            HandGhostProviderUtils.TryGetDefaultProvider(out _ghostVisualsProvider);
        }

        private void OnDisable()
        {
            DestroyGhost();
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            GUILayout.Space(20f);
            DrawGenerationMenu();
        }

        private void DrawGenerationMenu()
        {
            CheckUniqueScales(_target.HandGrabPoses);
            CheckUniqueHandedness(_target.HandGrabPoses);

            ScaledHandPoseSlider();

            if (GUILayout.Button($"Add HandGrabPose Key with Scale {_selectedScale.ToString("F2")}"))
            {
                AddHandGrabPose(_selectedScale);
                EditorUtility.SetDirty(_target);
            }

            GUILayout.Space(20f);

            if (GUILayout.Button("Refresh HandGrab Pose"))
            {
                RefreshHandPoses();
                EditorUtility.SetDirty(_target);
            }

            if (GUILayout.Button("Create Mirrored HandGrabInteractable"))
            {
                Mirror();
            }

            UpdateGhost();
        }

        private void ScaledHandPoseSlider()
        {
            EditorGUILayout.LabelField("Scaled HandGrabPose Keys", EditorStyles.label);
            _selectedScale = EditorGUILayout.Slider(_selectedScale, _minScale, _maxScale);
            Rect backRect = GUILayoutUtility.GetLastRect();
            backRect.x += _leftMargin;
            backRect.width -= _rightMarging;

            foreach (HandGrabPose grabPose in _target.HandGrabPoses)
            {
                if (grabPose == null)
                {
                    continue;
                }
                float x = backRect.x + Mathf.InverseLerp(_minScale, _maxScale, grabPose.Scale) * backRect.width;
                Rect poseRect = new Rect(x - _poseRectWidth * 0.5f, backRect.y, _poseRectWidth, backRect.height);
                EditorGUI.LabelField(poseRect, EditorGUIUtility.IconContent("curvekeyframeselected"));
            }
        }

        private void RefreshHandPoses()
        {
            _target.HandGrabPoses.Clear();
            HandGrabPose[] handGrabPoses = _target.GetComponentsInChildren<HandGrabPose>();
            _target.HandGrabPoses.AddRange(handGrabPoses);
        }

        private void CheckUniqueScales(List<HandGrabPose> handGrabPoses)
        {
            _scalesSet.Clear();
            for (int i = 0; i < handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = handGrabPoses[i];
                if (grabPose == null)
                {
                    continue;
                }

                if (_scalesSet.Contains(grabPose.Scale))
                {
                    EditorGUILayout.HelpBox($"Duplicated {nameof(HandGrabPose)} of scale {grabPose.Scale} at index {i}.", MessageType.Warning);
                }
                _scalesSet.Add(grabPose.Scale);
            }
        }

        private void CheckUniqueHandedness(List<HandGrabPose> handGrabPoses)
        {
            bool handednessSet = false;
            Handedness validHandedness = Handedness.Left;
            for (int i = 0; i < handGrabPoses.Count; i++)
            {
                HandGrabPose grabPose = handGrabPoses[i];
                if (grabPose == null || grabPose.HandPose == null)
                {
                    continue;
                }
                Handedness grabPoseHandedness = grabPose.HandPose.Handedness;
                if (!handednessSet)
                {
                    handednessSet = true;
                    validHandedness = grabPoseHandedness;
                }
                else if (grabPoseHandedness != validHandedness)
                {
                    EditorGUILayout.HelpBox($"Different Handedness at index {i}. " +
                        $"Ensure all HandGrabPoses have the same Handedness", MessageType.Warning);
                }
            }
        }

        private void AddHandGrabPose(float scale)
        {
            HandGrabPose result = HandGrabUtils.CreateHandGrabPose(_target);
            result.transform.localScale = Vector3.one * scale;

            GrabPoseFinder.FindInterpolationRange(scale, _target.HandGrabPoses,
                out HandGrabPose from, out HandGrabPose to, out float t);

            if (from != null && to == null)
            {
                result.InjectAllHandGrabPose(from.RelativeTo);
                HandPose resultHandPose = new HandPose(from.HandPose);
                result.InjectOptionalHandPose(resultHandPose);
                result.InjectOptionalSurface(from.SnapSurface);
                result.transform.position = from.transform.position;
                result.transform.rotation = from.transform.rotation;
            }
            else if (from == null && to != null)
            {
                result.InjectAllHandGrabPose(to.RelativeTo);
                HandPose resultHandPose = new HandPose(to.HandPose);
                result.InjectOptionalHandPose(resultHandPose);
                result.InjectOptionalSurface(to.SnapSurface);
                result.transform.position = to.transform.position;
                result.transform.rotation = to.transform.rotation;
            }
            else if (from != null && to != null)
            {
                result.InjectAllHandGrabPose(from.RelativeTo);
                HandPose resultHandPose = new HandPose(from.HandPose);
                HandPose.Lerp(from.HandPose, to.HandPose, t, ref resultHandPose);
                result.InjectOptionalHandPose(resultHandPose);
                result.InjectOptionalSurface(from.SnapSurface);
                result.transform.position = Vector3.LerpUnclamped(from.transform.position, to.transform.position, t);
                result.transform.rotation = Quaternion.SlerpUnclamped(from.transform.rotation, to.transform.rotation, t);
            }

            _target.HandGrabPoses.Add(result);
        }

        private void Mirror()
        {
            HandGrabInteractable mirrorInteractable =
                   HandGrabUtils.CreateHandGrabInteractable(_target.RelativeTo,
                       $"{_target.gameObject.name}_mirror");

            var data = HandGrabUtils.SaveData(_target);
            data.poses = null;
            HandGrabUtils.LoadData(mirrorInteractable, data);
            foreach (HandGrabPose point in _target.HandGrabPoses)
            {
                HandGrabPose mirrorPose = HandGrabUtils.CreateHandGrabPose(mirrorInteractable);
                HandGrabUtils.MirrorHandGrabPose(point, mirrorPose);
                mirrorPose.transform.SetParent(mirrorInteractable.transform);
                mirrorInteractable.HandGrabPoses.Add(mirrorPose);
            }
        }

        #region Ghost

        private void UpdateGhost()
        {
            GrabPoseFinder.FindInterpolationRange(_selectedScale, _target.HandGrabPoses,
                out HandGrabPose from, out HandGrabPose to, out float t);

            Pose rootPose = Pose.identity;
            if (from != null && to == null)
            {
                _ghostHandPose.CopyFrom(from.HandPose);
                PoseUtils.Multiply(from.RelativeTo.GetPose(), from.RelativeGrip, ref rootPose);
                DisplayGhost(_ghostHandPose, rootPose, _selectedScale);
            }
            else if (from == null && to != null)
            {
                _ghostHandPose.CopyFrom(to.HandPose);
                PoseUtils.Multiply(to.RelativeTo.GetPose(), to.RelativeGrip, ref rootPose);
                DisplayGhost(_ghostHandPose, rootPose, _selectedScale);
            }
            else if (from != null && to != null)
            {
                HandPose.Lerp(from.HandPose, to.HandPose, t, ref _ghostHandPose);
                Pose fromPose = PoseUtils.Multiply(from.RelativeTo.GetPose(), from.RelativeGrip);
                Pose toPose = PoseUtils.Multiply(to.RelativeTo.GetPose(), to.RelativeGrip);
                PoseUtils.Lerp(fromPose, toPose, t, ref rootPose);
                DisplayGhost(_ghostHandPose, rootPose, _selectedScale);
            }
            else
            {
                DestroyGhost();
            }
        }

        private void DisplayGhost(HandPose handPose, Pose rootPose, float scale)
        {
            if (_handGhost != null
                && _lastHandedness != handPose.Handedness)
            {
                DestroyGhost();
            }

            _lastHandedness = handPose.Handedness;
            if (_handGhost == null)
            {
                HandGhost ghostPrototype = _ghostVisualsProvider.GetHand(_lastHandedness);
                _handGhost = GameObject.Instantiate(ghostPrototype, _target.transform);
                _handGhost.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }
            _handGhost.transform.localScale = Vector3.one * scale;
            _handGhost.SetPose(handPose, rootPose);
        }

        private void DestroyGhost()
        {
            if (_handGhost == null)
            {
                return;
            }
            GameObject.DestroyImmediate(_handGhost.gameObject);
            _handGhost = null;
        }
        #endregion
    }
}
