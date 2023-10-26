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

#if UNITY_2021_2_OR_NEWER
#define OVR_BB_DRAGANDDROP
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    public class BuildingBlocksWindow : EditorWindow
    {
        private class Styles
        {
            internal const int IdealThumbnailWidth = 280;
            internal const float ThumbnailRatio = 1.8f;
            internal const int BlockMargin = 8;
            internal const int Border = 1;
            internal const int DescriptionHeight = 48;
            internal const int Padding = 4;
            internal const int TightPadding = 2;

#if OVR_BB_DRAGANDDROP
            internal const float DragOpacity = 0.5f;
            internal static readonly Color DragColor = new Color(1.0f, 1.0f, 1.0f, DragOpacity);
#endif // OVR_BB_DRAGANDDROP
            internal const float DisabledTint = 0.6f;
            internal static readonly Color DisabledColor = new Color(DisabledTint, DisabledTint, DisabledTint, 1.0f);

            internal readonly GUIStyle GridItemStyle = new GUIStyle()
            {
                margin = new RectOffset(BlockMargin, BlockMargin, BlockMargin, BlockMargin),
                padding = new RectOffset(Border, Border, Border, Border),
                stretchWidth = false,
                stretchHeight = false,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#1d1d1d"))
                },
                hover =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#6d6d6d"))
                }
            };

            internal readonly GUIStyle GridItemDisabledStyle = new GUIStyle()
            {
                margin = new RectOffset(BlockMargin, BlockMargin, BlockMargin, BlockMargin),
                padding = new RectOffset(Border, Border, Border, Border),
                stretchWidth = false,
                stretchHeight = false,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#1d1d1d"))
                }
            };

            internal readonly GUIStyle ThumbnailAreaStyle = new GUIStyle()
            {
                stretchHeight = false
            };

            internal readonly GUIStyle SeparatorAreaStyle = new GUIStyle()
            {
                fixedHeight = Border,
                stretchHeight = false,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#363636"))
                }
            };

            internal readonly GUIStyle DescriptionAreaStyle = new GUIStyle()
            {
                stretchHeight = false,
                fixedHeight = Styles.DescriptionHeight,
                padding = new RectOffset(Padding, Padding, Padding, Padding),
                margin = new RectOffset(0, 0, 0, Border),

                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#3e3e3e"))
                }
            };

            internal readonly GUIStyle EmptyAreaStyle = new GUIStyle()
            {
                stretchHeight = true,
                fixedWidth = 0,
                fixedHeight = Styles.DescriptionHeight,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
            };

            internal readonly GUIStyle DescriptionAreaHoverStyle = new GUIStyle()
            {
                stretchHeight = false,
                fixedHeight = Styles.DescriptionHeight,
                padding = new RectOffset(Padding, Padding, Padding, Padding),
                margin = new RectOffset(0, 0, 0, Border),

                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#4e4e4e"))
                }
            };

            internal readonly GUIStyle BoldLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                stretchHeight = true,
                fixedHeight = 32,
                fontSize = 16,
                normal =
                {
                    textColor = Color.white
                },
                hover =
                {
                    textColor = Color.white
                },
                alignment = TextAnchor.MiddleLeft
            };

            internal readonly GUIStyle MiniButton = new GUIStyle(EditorStyles.miniButton)
            {
                clipping = TextClipping.Overflow,
                fixedHeight = 18.0f,
                fixedWidth = 18.0f,
                margin = new RectOffset(2, 2, 2, 2),
                padding = new RectOffset(1, 1, 1, 1)
            };

            internal readonly GUIStyle LargeButton = new GUIStyle(EditorStyles.miniButton)
            {
                clipping = TextClipping.Overflow,
                fixedHeight = DescriptionHeight - Padding * 2,
                fixedWidth = DescriptionHeight - Padding * 2,
                margin = new RectOffset(2, 2, 0, 2),
                padding = new RectOffset(Padding, Padding, Padding, Padding)
            };

            internal readonly GUIStyle IssuesTitleLabel = new GUIStyle(EditorStyles.label)
            {
                fontSize = 14,
                wordWrap = false,
                stretchWidth = false,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(10, 10, 0, 0)
            };

            internal readonly GUIStyle Header = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                fixedHeight = 32 + BlockMargin * 2,
                padding = new RectOffset(BlockMargin, BlockMargin, BlockMargin, BlockMargin),
                margin = new RectOffset(0, 0, 0, 0),

                wordWrap = true,
                normal =
                {
                    background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#3e3e3e"))
                }
            };

            internal readonly GUIStyle HeaderIconStyle = new GUIStyle()
            {
                fixedHeight = 32.0f,
                fixedWidth = 32.0f,
                stretchWidth = false,
                alignment = TextAnchor.MiddleCenter
            };

            internal readonly GUIStyle SubtitleHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize = 12,
                margin = new RectOffset(BlockMargin, BlockMargin, BlockMargin, BlockMargin),
                wordWrap = true
            };

            internal readonly GUIStyle InternalHelpBox = new GUIStyle(EditorStyles.helpBox)
            {
                margin = new RectOffset(BlockMargin, BlockMargin, BlockMargin, BlockMargin)
            };

            internal readonly GUIStyle InternalHelpText = new GUIStyle(EditorStyles.miniLabel)
            {
                margin = new RectOffset(10, 0, 0, 0),
                wordWrap = true,
                fontStyle = FontStyle.Italic,
                normal =
                {
                    textColor = new Color(0.58f, 0.72f, 0.95f)
                }
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

            private static Texture2D _infoExperimentalBgTexture;
            private static Texture2D InfoExperimentalBgTexture
            {

                get
                {
                    if (_infoExperimentalBgTexture != null)
                    {
                        return _infoExperimentalBgTexture;
                    }

                    _infoExperimentalBgTexture = new Texture2D(1, 1);
                    _infoExperimentalBgTexture.SetPixel(0, 0, new Color(0.22f, 0.22f, 0.22f, 0.25f));
                    _infoExperimentalBgTexture.Apply();

                    return _infoExperimentalBgTexture;
                }
            }

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

            internal readonly OVRGUIContent AddIcon =
                OVREditorUtils.CreateContent("ovr_icon_addblock.png", OVRGUIContent.Source.BuildingBlocksIcons, "Add Block to current scene");

            internal readonly OVRGUIContent ExperimentalIcon =
                OVREditorUtils.CreateContent("ovr_icon_experimental.png", OVRGUIContent.Source.BuildingBlocksIcons, "Experimental");

            internal readonly OVRGUIContent DownloadIcon =
                OVREditorUtils.CreateContent("ovr_icon_download.png", OVRGUIContent.Source.BuildingBlocksIcons, "Download Block to your project");

            internal readonly OVRGUIContent SelectIcon =
                OVREditorUtils.CreateContent("ovr_icon_link.png", OVRGUIContent.Source.BuildingBlocksIcons, "Select Block in current scene");

            internal readonly OVRGUIContent ConfigIcon =
                OVREditorUtils.CreateContent("_Popup", OVRGUIContent.Source.BuiltIn, "Additional options");

            internal readonly OVRGUIContent DocumentationIcon =
                OVREditorUtils.CreateContent("ovr_icon_documentation.png", OVRGUIContent.Source.GenericIcons, "Go to Documentation");


            internal readonly OVRGUIContent HeaderIcon =
                OVREditorUtils.CreateContent("ovr_icon_bbw.png", OVRGUIContent.Source.BuildingBlocksIcons, null);

            internal static readonly Color AccentColor = OVREditorUtils.HexToColor("#a29de5");
            internal static readonly Color ExperimentalColor = OVREditorUtils.HexToColor("#eba333");
        }

        private static Styles _styles;
        private static Styles styles => _styles ??= new Styles();

        private const string MenuPath = "Oculus/Tools/Building Blocks";
        private const int MenuPriority = 2;
        private static readonly string WindowName = Utils.BlocksPublicName;
        private const string AddButtonLabel = "Add";

#if OVR_BB_DRAGANDDROP
        private const string DragAndDropLabel = "Dragging Block";
        private const string DragAndDropBlockDataLabel = "block";
        private const string DragAndDropBlockThumbnailLabel = "blockThumbnail";
#endif // OVR_BB_DRAGANDDROP

        private static readonly GUIContent Title = new GUIContent(Utils.BlocksPublicName);

        private static readonly GUIContent Description =
            new GUIContent("Drag and drop blocks onto your scene to add XR features to your project.");

        private const string DocumentationUrl =
        "https://developer.oculus.com/documentation/unity/unity-buildingblocks-overview";


        private Vector2 _scrollPosition;

        private OVRAnimatedContent _outline = null;
        private OVRAnimatedContent _tutorial = null;
        private static readonly OVRProjectSetupSettingBool _tutorialCompleted =
            new OVRProjectSetupUserSettingBool("BBTutorialCompleted", false);
        private static bool _shouldShowTutorial = false;

        [MenuItem(MenuPath, false, MenuPriority)]
        private static void ShowWindow()
        {
            ShowWindow("MenuItem");
        }

        public static void ShowWindow(string source)
        {
            GetWindow<BuildingBlocksWindow>(WindowName);

            OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.OpenWindow)
                .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.ActionTrigger, source)
                .Send();
        }

        private void OnGUI()
        {
            OnHeaderGUI();

            var windowWidth = position.width - Styles.BlockMargin;
            windowWidth = Mathf.Max(Styles.IdealThumbnailWidth + Styles.Padding * 3, windowWidth);
            var scrollableAreaWidth = windowWidth - 18;
            var blockWidth = Styles.IdealThumbnailWidth;
            var numberOfColumns = Mathf.FloorToInt(scrollableAreaWidth / blockWidth);
            if (numberOfColumns < 1) numberOfColumns = 1;


            var marginToRemove = numberOfColumns * Styles.BlockMargin;
            var expectedThumbnailWidth = Mathf.FloorToInt((scrollableAreaWidth - marginToRemove) / numberOfColumns);
            var expectedThumbnailHeight = Mathf.FloorToInt(expectedThumbnailWidth / Styles.ThumbnailRatio);

            ShowList(_blockList, numberOfColumns, expectedThumbnailWidth, expectedThumbnailHeight, windowWidth);
#if OVR_BB_DRAGANDDROP
            RefreshDragAndDrop(expectedThumbnailWidth, expectedThumbnailHeight);
#endif // OVR_BB_DRAGANDDROP

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        private void OnHeaderGUI()
        {
            EditorGUILayout.BeginHorizontal(styles.Header);
            {
                using (new OVREditorUtils.OVRGUIColorScope(OVREditorUtils.OVRGUIColorScope.Scope.Content, Styles.AccentColor))

                {
                    EditorGUILayout.LabelField(styles.HeaderIcon, styles.HeaderIconStyle, GUILayout.Width(32.0f),
                        GUILayout.ExpandWidth(false));
                }
                EditorGUILayout.LabelField(Title, styles.BoldLabel);

                EditorGUILayout.Space(0, true);
                if (GUILayout.Button(styles.ConfigIcon, styles.MiniButton))
                {
                }

                if (GUILayout.Button(styles.DocumentationIcon, styles.MiniButton))
                {
                    Application.OpenURL(DocumentationUrl);
                }

            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(styles.SubtitleHelpText);
            GUILayout.Label(Description, styles.LabelStyle);
            EditorGUILayout.EndHorizontal();
        }

        private void OnEnable()
        {
            RefreshBlockList();
#if OVR_BB_DRAGANDDROP
            DragAndDrop.AddDropHandler(SceneDropHandler);
            DragAndDrop.AddDropHandler(HierarchyDropHandler);
#endif // OVR_BB_DRAGANDDROP
            wantsMouseMove = true;

            _shouldShowTutorial = ShouldShowTutorial();
        }

        private void RefreshBlockList()
        {
            _blockList = GetList();
        }

        private void OnDisable()
        {
#if OVR_BB_DRAGANDDROP
            DragAndDrop.RemoveDropHandler(SceneDropHandler);
            DragAndDrop.RemoveDropHandler(HierarchyDropHandler);
#endif // OVR_BB_DRAGANDDROP
        }

        private List<BlockBaseData> _blockList;

        private static List<BlockBaseData> GetList()
        {
            var blockGuids = AssetDatabase.FindAssets($"t:{nameof(BlockBaseData)}");

            return blockGuids.Select(id =>
                    AssetDatabase.LoadAssetAtPath<BlockBaseData>(AssetDatabase.GUIDToAssetPath(id)))
                .Where(obj =>
                    !string.IsNullOrEmpty(obj.name)
                    && obj.DisplayOnContentTab)
                .OrderBy(block => block.Order)
                .ThenBy(block => block.BlockName)
                .ToList();
        }

        private void ShowList(List<BlockBaseData> blocks, int numberOfColumns, int expectedThumbnailWidth, int expectedThumbnailHeight, float expectedScrollWidth)
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Width(expectedScrollWidth));

            var columnIndex = 0;
            var showTutorial = _shouldShowTutorial;
            GUILayout.BeginHorizontal();
            foreach (var block in blocks)
            {
                var rect = Show(block, expectedThumbnailWidth, expectedThumbnailHeight);

                if (showTutorial && block.CanBeAdded)
                {
                    ShowTutorial(rect);
                    showTutorial = false;
                }

                columnIndex++;
                if (columnIndex >= numberOfColumns)
                {
                    columnIndex = 0;
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }

            GUILayout.EndHorizontal();

            EditorGUILayout.EndScrollView();
        }

        private Rect Show(BlockBaseData block, int expectedThumbnailWidth, int expectedThumbnailHeight)
        {
            var blockData = block as BlockData;

            var canBeAdded = block.CanBeAdded;
            var numberInScene = blockData != null ? blockData.ComputeNumberOfBlocksInScene() : 0;

            var canBeSelected = numberInScene > 0;
            var previousColor = GUI.color;
            var expectedColor = canBeAdded ? Color.white : Styles.DisabledColor;
            GUI.color = expectedColor;
            var gridItemStyle = canBeAdded ? styles.GridItemStyle : styles.GridItemDisabledStyle;
            gridItemStyle.fixedWidth = expectedThumbnailWidth;
            var dragArea = EditorGUILayout.BeginVertical(gridItemStyle);
            var hover = canBeAdded && dragArea.Contains(Event.current.mousePosition);
            var thumbnailAreaStyle = styles.ThumbnailAreaStyle;
            thumbnailAreaStyle.fixedHeight = expectedThumbnailHeight;
            var thumbnailArea = EditorGUILayout.BeginVertical(thumbnailAreaStyle);
            {
                GUI.DrawTexture(thumbnailArea, block.Thumbnail, ScaleMode.ScaleAndCrop);
                if (block.Experimental)
                {
                    const string experimentalStr = "Experimental";
                    var width = styles.ExperimentalTextStyle.CalcSize(new GUIContent(experimentalStr)).x;
                    var height = styles.ExperimentalAreaStyle.fixedHeight - Styles.TightPadding * 2.0f;
                    EditorGUILayout.BeginHorizontal(styles.ExperimentalAreaStyle, GUILayout.Width(height + width), GUILayout.Height(height));
                    EditorGUILayout.LabelField(styles.ExperimentalIcon, styles.ExperimentalTextStyle, GUILayout.Width(height), GUILayout.Height(height));
                    EditorGUILayout.LabelField(experimentalStr, styles.ExperimentalTextStyle, GUILayout.Width(width), GUILayout.Height(height));
                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    // This space fills the area, otherwise the area will have a height of null
                    // despite the fixedHeight set
                    EditorGUILayout.Space();
                }
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(styles.SeparatorAreaStyle);
            {
                // This space fills the area, otherwise the area will have a height of null
                // despite the fixedHeight set
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndVertical();

            var descriptionStyle = hover ? styles.DescriptionAreaHoverStyle : styles.DescriptionAreaStyle;
            EditorGUILayout.BeginVertical(descriptionStyle);
            EditorGUILayout.BeginHorizontal();
            var numberOfIcons = 0;
            if (canBeAdded) numberOfIcons++;
            if (canBeSelected) numberOfIcons++;
            var iconWidth = styles.LargeButton.fixedWidth + styles.LargeButton.margin.horizontal;
            var padding = descriptionStyle.padding.horizontal;
            var style = new GUIStyle(styles.EmptyAreaStyle);
            style.fixedWidth = expectedThumbnailWidth - padding - numberOfIcons * iconWidth;
            EditorGUILayout.BeginVertical(style);
            EditorGUILayout.BeginHorizontal();
            var labelStyle = hover ? styles.LabelHoverStyle : styles.LabelStyle;
            var labelContent = new GUIContent(block.BlockName);
            EditorGUILayout.LabelField(block.BlockName, labelStyle, GUILayout.Width(labelStyle.CalcSize(labelContent).x));
            labelStyle = styles.SubtitleStyle;
            labelContent = new GUIContent(block.Sdk.ToString());
            EditorGUILayout.LabelField(block.Sdk.ToString(), styles.SubtitleStyle, GUILayout.Width(labelStyle.CalcSize(labelContent).x));
            EditorGUILayout.EndHorizontal();
            var info = numberInScene > 0 ? $"{numberInScene} {OVREditorUtils.ChoosePlural(numberInScene, "instance", "instances")} in current scene" : block.Description;
            EditorGUILayout.LabelField(info, styles.InfoStyle);
            EditorGUILayout.EndVertical();

            if (canBeAdded)
            {
            var addIcon = styles.AddIcon;
                if (ShowLargeButton(addIcon))
                {
                    block.AddToProject(block.RequireListRefreshAfterInstall ? RefreshBlockList : (Action)null);

                }
            }

            if (canBeSelected)
            {
                if (ShowLargeButton(styles.SelectIcon))
                {
                    blockData.SelectBlockInScene();
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

#if OVR_BB_DRAGANDDROP
            if (canBeAdded)
            {
                EditorGUIUtility.AddCursorRect(dragArea, MouseCursor.Pan);
                if (dragArea.Contains(Event.current.mousePosition) && Event.current.type == EventType.MouseDown)
                {
                    SetDragAndDrop(block);
                }
            }

#endif // OVR_BB_DRAGANDDROP
            GUI.color = previousColor;

            return dragArea;
        }

        private bool ShouldShowTutorial()
        {
            _shouldShowTutorial = !_tutorialCompleted.Value;
            if (_shouldShowTutorial)
            {
                // Make sure the scene doesn't have a non block version of the OVRCameraRig
                _shouldShowTutorial = !BlockData.HasNonBBCameraRig();
            }

            return _shouldShowTutorial;
        }

        private void ShowTutorial(Rect dragArea)
        {
            if (_outline == null && OVRGUIContent.BuildPath("bb_outline.asset", OVRGUIContent.Source.BuildingBlocksAnimations, out var outlinePath))

            {
                _outline = AssetDatabase.LoadAssetAtPath<OVRAnimatedContent>(outlinePath);
            }

            if (_outline != null)
            {
                _outline.Update();
                GUI.DrawTexture(dragArea, _outline.CurrentFrame);
            }

            if (_tutorial == null && OVRGUIContent.BuildPath("bb_tutorial.asset", OVRGUIContent.Source.BuildingBlocksAnimations, out var tutorialPath))

            {
                _tutorial = AssetDatabase.LoadAssetAtPath<OVRAnimatedContent>(tutorialPath);
            }

            if (_tutorial != null)
            {
                _tutorial.Update();
                GUI.DrawTexture(dragArea, _tutorial.CurrentFrame);
            }

            Repaint();
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

        private static void OnAdd(BlockBaseData block)
        {
            block.AddToProject();
        }

#if OVR_BB_DRAGANDDROP
        private void RefreshDragAndDrop(int expectedThumbnailWidth, int expectedThumbnailHeight)
        {
            var blockThumbnail = DragAndDrop.GetGenericData(DragAndDropBlockThumbnailLabel) as Texture2D;
            if (blockThumbnail)
            {
                var cursorOffset = new Vector2(expectedThumbnailWidth / 2.0f, expectedThumbnailHeight / 2.0f);
                var cursorRect = new Rect(Event.current.mousePosition - cursorOffset, new Vector2(expectedThumbnailWidth, expectedThumbnailHeight));
                GUI.color = new Color(1, 1, 1, Styles.DragOpacity);
                GUI.DrawTexture(cursorRect, blockThumbnail, ScaleMode.ScaleAndCrop);
                GUI.color = Color.white;

                // Enforce a repaint next frame, as we need to move this thumbnail everyframe
                Repaint();
            }

            if (Event.current.type == EventType.DragExited)
            {
                ResetDragThumbnail();
            }

            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Move;
            }
        }

        private static DragAndDropVisualMode HierarchyDropHandler(
            int dropTargetInstanceID,
            HierarchyDropFlags dropMode,
            Transform parentForDraggedObjects,
            bool perform) => DropHandler(perform);

        private static DragAndDropVisualMode SceneDropHandler(
            UnityEngine.Object dropUpon,
            Vector3 worldPosition,
            Vector2 viewportPosition,
            Transform parentForDraggedObjects,
            bool perform) => DropHandler(perform);

        private static DragAndDropVisualMode DropHandler(bool perform)
        {
            var block = DragAndDrop.GetGenericData(DragAndDropBlockDataLabel) as BlockBaseData;
            if (block != null)
            {
                if (perform)
                {
                    block.AddToProject();
                    ResetDragAndDrop();
                    _tutorialCompleted.Value = true;
                    _shouldShowTutorial = false;
                }

                return DragAndDropVisualMode.Generic;
            }
            return DragAndDropVisualMode.None;
        }

        private static void SetDragAndDrop(BlockBaseData block)
        {
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.SetGenericData(DragAndDropBlockDataLabel, block);
            DragAndDrop.SetGenericData(DragAndDropBlockThumbnailLabel, block.Thumbnail);
            DragAndDrop.StartDrag(DragAndDropLabel);
        }

        private static void ResetDragThumbnail()
        {
            DragAndDrop.SetGenericData(DragAndDropBlockThumbnailLabel, null);
        }

        private static void ResetDragAndDrop()
        {
            DragAndDrop.SetGenericData(DragAndDropBlockDataLabel, null);
        }
#endif // OVR_BB_DRAGANDDROP
    }
}
