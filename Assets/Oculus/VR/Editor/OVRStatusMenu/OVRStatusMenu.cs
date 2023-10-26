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
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal class OVRStatusMenu : EditorWindow
{
    public struct Item
    {
        public string InfoText => InfoTextDelegate?.Invoke();
        public void OnClick() => OnClickDelegate?.Invoke();

        public string Name;
        public Color Color;
        public int Order;
        public OVRGUIContent Icon;
        public Func<string> InfoTextDelegate;
        public Action OnClickDelegate;
    }

    private class Styles
    {
        internal const float Width = 360;
        internal const int LeftMargin = 4;
        internal const int Border = 1;
        internal const int Padding = 4;
        internal const float ItemHeight = 48.0f;

        internal readonly GUIStyle BackgroundAreaStyle = new GUIStyle()
        {
            stretchHeight = true,
            padding = new RectOffset(Border, Border, Border, Border),
            normal =
            {
                background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#1d1d1d"))
            }
        };

        internal readonly GUIStyle DescriptionAreaStyle = new GUIStyle()
        {
            stretchHeight = true,
            fixedHeight = Styles.ItemHeight,
            padding = new RectOffset(LeftMargin + Padding, Padding, Padding, Padding),
            margin = new RectOffset(0,0,0, Border),
            normal =
            {
                background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#3e3e3e"))
            },
            hover =
            {
                background = OVREditorUtils.MakeTexture(1, 1, OVREditorUtils.HexToColor("#4e4e4e"))
            }
        };

        internal readonly GUIStyle LabelStyle = new GUIStyle(EditorStyles.boldLabel);

        internal readonly GUIStyle LabelHoverStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            normal = { textColor = Color.white }
        };

        internal readonly GUIStyle SubtitleStyle = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Italic
        };

        internal readonly GUIStyle IconStyle = new GUIStyle(EditorStyles.label)
        {
            fixedWidth = 48 - Padding * 2,
            fixedHeight = 48 - Padding * 2,
            stretchHeight = true,
            padding = new RectOffset(8,8,8,8),
        };
    }

    private static Styles _styles;
    private static Styles styles => _styles ??= new Styles();
    private static readonly List<Item> Items = new List<Item>();
    private static OVRStatusMenu _instance;

    public static void RegisterItem(Item item)
    {
        Items.Add(item);
        Items.Sort((x, y) => x.Order.CompareTo(y.Order));
    }

    public static void ShowDropdown(Vector2 position)
    {
        if (_instance != null)
        {
            _instance.Close();
        }

        if (Items.Count == 0)
        {
            return;
        }

        _instance = CreateInstance<OVRStatusMenu>();
        _instance.ShowAsDropDown(new Rect(position, Vector2.zero), new Vector2(Styles.Width, _instance.ComputeHeight()));
        _instance.wantsMouseMove = true;
        _instance.Focus();
    }

    private float ComputeHeight()
    {
        return Styles.ItemHeight * Items.Count + 2;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(styles.BackgroundAreaStyle);
        {
            foreach (var item in Items)
            {
                ShowItem(item);
            }
        }
        EditorGUILayout.EndVertical();

        if (Event.current.type == EventType.MouseMove)
        {
            Repaint();
        }
    }

    private void ShowItem(Item item)
    {
        var buttonRect = EditorGUILayout.BeginVertical(styles.DescriptionAreaStyle);
        var hover = buttonRect.Contains(Event.current.mousePosition);
        {
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField(item.Icon, styles.IconStyle, GUILayout.Width(Styles.ItemHeight));
                EditorGUILayout.BeginVertical();
                {
                    EditorGUILayout.LabelField(item.Name, hover ? styles.LabelHoverStyle : styles.LabelStyle);
                    EditorGUILayout.LabelField(item.InfoText, styles.SubtitleStyle);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();

        var leftMarginRect = buttonRect;
        leftMarginRect.width = Styles.LeftMargin;
        EditorGUI.DrawRect(leftMarginRect, item.Color);
        EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
        if (hover && Event.current.type == EventType.MouseUp)
        {
            item.OnClick();
            Close();
        }
    }
}
