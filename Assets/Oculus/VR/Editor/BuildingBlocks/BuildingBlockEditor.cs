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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    [CustomEditor(typeof(BuildingBlock))]
    public class BuildingBlockEditor : UnityEditor.Editor
    {
        private class Styles
        {
            internal const float ThumbnailRatio = 1.8f;
            internal const int Border = 1;
            internal const float SmallIconSize = 16.0f;
            internal const float ItemHeight = 48.0f;
            internal const int Padding = 4;
            internal const int TightPadding = 2;

            internal readonly GUIStyle GridItemStyle = new GUIStyle()
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(Border, Border, Border, Border),
                stretchWidth = false,
                stretchHeight = false,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#1d1d1d"))
                }
            };

            internal readonly GUIStyle DescriptionAreaStyle = new GUIStyle()
            {
                stretchHeight = false,
                padding = new RectOffset(Padding, Padding, Padding, Padding),
                margin = new RectOffset(0, 0, 0, Border),
                fixedHeight = ItemHeight,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#3e3e3e"))
                }
            };

            internal readonly GUIStyle IconStyle = new GUIStyle(EditorStyles.label)
            {
                margin = new RectOffset(0, 0, 0, 0),
                padding = new RectOffset(0, 0, 0, 0),
                fixedWidth = SmallIconSize,
                stretchWidth = false
            };

            internal readonly GUIStyle LargeButton = new GUIStyle(EditorStyles.miniButton)
            {
                clipping = TextClipping.Overflow,
                fixedHeight = ItemHeight - Padding * 2,
                fixedWidth = ItemHeight - Padding * 2,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(Padding, Padding, Padding, Padding)
            };

            internal readonly GUIStyle LabelStyle = new GUIStyle(EditorStyles.boldLabel);

            internal readonly GUIStyle LabelHoverStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                normal = { textColor = Color.white }
            };

            internal readonly GUIStyle SubtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Italic,
                normal =
                {
                    textColor = Color.gray
                }
            };

            internal readonly GUIStyle InfoStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 10,
                normal =
                {
                    textColor = Color.gray
                }
            };

            internal readonly GUIStyle ExperimentalAreaStyle = new GUIStyle()
            {
                margin = new RectOffset(Padding, Padding, Padding, Padding),
                padding = new RectOffset(Padding, TightPadding, TightPadding, Padding),
                fixedHeight = 18,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#3e3e3eAA"))
                }
            };

            internal readonly GUIStyle ExperimentalTextStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 10,
                normal = { textColor = ExperimentalColor },
                wordWrap = true
            };

            internal static readonly OVRGUIContent ErrorIcon =
                OVREditorUtils.CreateContent("ovr_error_greybg.png", OVRGUIContent.Source.BuildingBlocksIcons);

            internal static readonly OVRGUIContent SuccessIcon =
                OVREditorUtils.CreateContent("ovr_success_greybg.png", OVRGUIContent.Source.BuildingBlocksIcons);

            internal static readonly OVRGUIContent ExperimentalIcon =
                OVREditorUtils.CreateContent("ovr_icon_experimental.png", OVRGUIContent.Source.BuildingBlocksIcons, "Experimental");

            internal static readonly Color AccentColor = OVREditorUtils.HexToColor("#a29de5");
            internal static readonly Color ExperimentalColor = OVREditorUtils.HexToColor("#eba333");
        }

        private static Styles _styles;
        private static Styles styles => _styles ??= new Styles();

        private BuildingBlock _block;
        private BlockData _blockData;

        public override void OnInspectorGUI()
        {
            _block = target as BuildingBlock;
            _blockData = _block.GetBlockData();

            if (_blockData == null)
            {
                return;
            }

            var currentWidth = EditorGUIUtility.currentViewWidth;
            var expectedHeight = currentWidth / Styles.ThumbnailRatio;
            expectedHeight *= 0.5f;

            // Thumbnail
            var rect = GUILayoutUtility.GetRect(currentWidth, expectedHeight);
            rect.x -= 20;
            rect.width += 40;
            rect.y -= 4;
            GUI.DrawTexture(rect, _blockData.Thumbnail, ScaleMode.ScaleAndCrop);

            // Experimental tag
            if (_blockData.Experimental)
            {
                GUILayout.BeginArea(new Rect(styles.ExperimentalAreaStyle.margin.left, styles.ExperimentalAreaStyle.margin.top, currentWidth, expectedHeight));
                const string experimentalStr = "Experimental";
                var width = styles.ExperimentalTextStyle.CalcSize(new GUIContent(experimentalStr)).x;
                var height = styles.ExperimentalAreaStyle.fixedHeight - Styles.TightPadding * 2.0f;
                GUILayout.BeginHorizontal(styles.ExperimentalAreaStyle, GUILayout.Width(height + width), GUILayout.Height(height));
                EditorGUILayout.LabelField(Styles.ExperimentalIcon, styles.ExperimentalTextStyle, GUILayout.Width(height), GUILayout.Height(height));
                EditorGUILayout.LabelField(experimentalStr, styles.ExperimentalTextStyle, GUILayout.Width(width), GUILayout.Height(height));
                EditorGUILayout.EndHorizontal();
                GUILayout.EndArea();
            }

            // Separator
            rect = GUILayoutUtility.GetRect(currentWidth, 1);
            rect.x -= 20;
            rect.width += 40;
            rect.y -= 4;
            GUI.DrawTexture(rect, OVREditorUtils.MakeTexture(1, 1, Styles.AccentColor),
                ScaleMode.ScaleAndCrop);

            ShowBlock(_blockData, _block, false, false, true);

            ShowBlockDataList("Dependencies", _blockData.GetAllDependencyDatas());
            ShowBlockList("Used by", _blockData.GetUsingBlocksInScene());

        }



        private bool ShowLargeButton(GUIContent icon)
        {
            var previousColor = GUI.color;
            GUI.color = Color.white;
            var hit = GUILayout.Button(icon, styles.LargeButton);
            GUI.color = previousColor;
            EditorGUIUtility.AddCursorRect(GUILayoutUtility.GetLastRect(), MouseCursor.Link);
            return hit;
        }

        private void ShowBlockDataList(string name, List<BlockData> list)
        {
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);

            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("No dependency blocks are required.", EditorStyles.helpBox);
            }
            else
            {
                foreach (var dependency in list)
                {
                    ShowBlock(dependency, null, true, true, false);
                }
            }
        }

        private void ShowBlockList(string name, List<BuildingBlock> list)
        {
            EditorGUILayout.LabelField(name, EditorStyles.boldLabel);

            if (list.Count == 0)
            {
                EditorGUILayout.LabelField("No dependency blocks are required.", EditorStyles.helpBox);
            }
            else
            {
                foreach (var dependency in list)
                {
                    ShowBlock(null, dependency, true, true, false);
                }
            }
        }

        private void ShowBlock(BlockData data, BuildingBlock block, bool asGridItem,
            bool showAction, bool showBb)
        {
            var previousIndent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;

            data = data ? data : block.GetBlockData();
            block = block ? block : data.GetBlock();

            // Thumbnail
            if (asGridItem)
            {
                EditorGUILayout.BeginHorizontal(styles.GridItemStyle);
                EditorGUILayout.BeginHorizontal(styles.DescriptionAreaStyle);

                var expectedSize = Styles.ItemHeight;
                var rect = GUILayoutUtility.GetRect(0, expectedSize);
                rect.y -= Styles.Padding;
                rect.x -= Styles.Padding;
                rect.width = Styles.ItemHeight;
                GUI.DrawTexture(rect, data.Thumbnail, ScaleMode.ScaleAndCrop);

                EditorGUILayout.Space(Styles.ItemHeight - Styles.Padding - Styles.SmallIconSize * 0.5f - 2);

                EditorGUILayout.LabelField(block != null ? Styles.SuccessIcon : Styles.ErrorIcon, styles.IconStyle,
                    GUILayout.Width(Styles.SmallIconSize), GUILayout.Height(Styles.ItemHeight - Styles.Padding * 2));
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginHorizontal();
            }

            // Label
            EditorGUILayout.BeginVertical();
            EditorGUILayout.BeginHorizontal();
            var labelStyle = styles.LabelStyle;
            var labelContent = new GUIContent(data.BlockName);
            EditorGUILayout.LabelField(labelContent, labelStyle, GUILayout.Width(labelStyle.CalcSize(labelContent).x));
            labelStyle = styles.SubtitleStyle;
            labelContent = new GUIContent(data.Sdk.ToString());
            EditorGUILayout.LabelField(labelContent, styles.SubtitleStyle,
                GUILayout.Width(labelStyle.CalcSize(labelContent).x));
            EditorGUILayout.EndHorizontal();
            labelContent = new GUIContent(block ? block.name : "Not Installed");
            EditorGUILayout.LabelField(labelContent, styles.InfoStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (showAction)
            {
                if (block != null)
                {
                    if (ShowLargeButton(Utils.GotoIcon))
                    {
                        block.SelectBlockInScene();
                    }
                }
                else
                {
                    if (ShowLargeButton(Utils.AddIcon))
                    {
                        data.AddToProject();
                    }
                }
            }

            if (showBb && ShowLargeButton(Utils.StatusIcon))
            {
                BuildingBlocksWindow.ShowWindow("BuildingBlockEditor");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel = previousIndent;
        }
    }
}
