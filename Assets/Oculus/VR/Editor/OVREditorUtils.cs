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

internal static class OVREditorUtils
{
    static OVREditorUtils()
    {
        OVRGUIContent.RegisterContentPath(OVRGUIContent.Source.GenericIcons, "Icons");

        var statusItem = new OVRStatusMenu.Item()
        {
            Name = "Oculus Settings",
            Color = OVREditorUtils.HexToColor("#4e4e4e"),
            Icon = CreateContent("ovr_icon_settings.png", OVRGUIContent.Source.GenericIcons),
            InfoTextDelegate = ComputeMenuSubText,
            OnClickDelegate = OnStatusMenuClick,
            Order = 2
        };
        OVRStatusMenu.RegisterItem(statusItem);
    }

    public static string ComputeMenuSubText()
    {
        return "Open settings menu.";
    }

    private static void OnStatusMenuClick()
    {
        OVRProjectSettingsProvider.OpenSettingsWindow(OVRProjectSetupSettingsProvider.Origins.Icon);
    }

    // Helper function to create a texture with a given color
    public static Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = col;
        }

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pixels);
        result.Apply();

        return result;
    }

    public static Color HexToColor(string hex)
    {
        hex = hex.Replace("#", string.Empty);
        byte r = (byte)(Convert.ToInt32(hex.Substring(0, 2), 16));
        byte g = (byte)(Convert.ToInt32(hex.Substring(2, 2), 16));
        byte b = (byte)(Convert.ToInt32(hex.Substring(4, 2), 16));
        byte a = 255;

        if (hex.Length == 8)
        {
            a = (byte)(Convert.ToInt32(hex.Substring(6, 2), 16));
        }

        return new Color32(r, g, b, a);
    }

    public static string ChoosePlural(int number, string singular, string plural)
    {
        return number > 1 ? plural : singular;
    }

    public static OVRGUIContent CreateContent(string name, OVRGUIContent.Source source, string tooltip = null)
    {
        return new OVRGUIContent(name, source, tooltip);
    }

    public static bool IsMainEditor()
    {
        // Early Return when the process service is not the Editor itself
#if UNITY_2021_1_OR_NEWER
        return (uint)UnityEditor.MPE.ProcessService.level != (uint)UnityEditor.MPE.ProcessLevel.Secondary;
#else
        return (uint)UnityEditor.MPE.ProcessService.level != (uint)UnityEditor.MPE.ProcessLevel.Slave;
#endif
    }

    public struct OVRGUIColorScope : System.IDisposable
    {
        public enum Scope
        {
            All,
            Background,
            Content
        }

        private Color _previousColor;
        private Scope _scope;

        public OVRGUIColorScope(Scope scope, Color newColor)
        {
            _scope = scope;
            _previousColor = Color.white;
            switch (scope)
            {
                case Scope.All:
                    _previousColor = GUI.color;
                    GUI.color = newColor;
                    break;
                case Scope.Background:
                    _previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = newColor;
                    break;
                case Scope.Content:
                    _previousColor = GUI.contentColor;
                    GUI.contentColor = newColor;
                    break;
            }
        }

        public void Dispose()
        {
            switch (_scope)
            {
                case Scope.All:
                    GUI.color = _previousColor;
                    break;
                case Scope.Background:
                    GUI.backgroundColor = _previousColor;
                    break;
                case Scope.Content:
                    GUI.contentColor = _previousColor;
                    break;
            }
        }
    }
}
