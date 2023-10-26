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
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[InitializeOnLoad]
internal static class OVRProjectSetupStatusIcon
{
    private static readonly Type _toolbarType;
    private static readonly PropertyInfo _guiBackend;
    private static readonly PropertyInfo _visualTree;
    private static readonly FieldInfo _onGuiHandler;
    private static readonly OVRGUIContent _iconSuccess;
    private static readonly OVRGUIContent _iconNeutral;
    private static readonly OVRGUIContent _iconWarning;
    private static readonly OVRGUIContent _iconError;
    private static readonly string OpenOculusSettings = "Open Oculus Settings";

    private static GUIStyle _iconStyle;
    private static OVRGUIContent _currentIcon;
    private static Object _appStatusBar;
    private static VisualElement _container;

    internal static OVRGUIContent CurrentIcon => _currentIcon;


    static OVRProjectSetupStatusIcon()
    {
        if (!OVREditorUtils.IsMainEditor()) return;

        var editorAssembly = typeof(UnityEditor.Editor).Assembly;
        var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

        _toolbarType = editorAssembly.GetType("UnityEditor.AppStatusBar");
        var guiViewType = editorAssembly.GetType("UnityEditor.GUIView");
        var backendType = editorAssembly.GetType("UnityEditor.IWindowBackend");
        var containerType = typeof(IMGUIContainer);

        _guiBackend = guiViewType?.GetProperty("windowBackend", bindingFlags);
        _visualTree = backendType?.GetProperty("visualTree", bindingFlags);
        _onGuiHandler = containerType?.GetField("m_OnGUIHandler", bindingFlags);

        _iconSuccess = OVREditorUtils.CreateContent("ovr_icon_success.png",  OVRGUIContent.Source.GenericIcons);
        _iconNeutral = OVREditorUtils.CreateContent("ovr_icon_neutral.png",  OVRGUIContent.Source.GenericIcons);
        _iconWarning = OVREditorUtils.CreateContent("ovr_icon_warning.png",  OVRGUIContent.Source.GenericIcons);
        _iconError = OVREditorUtils.CreateContent("ovr_icon_error.png",  OVRGUIContent.Source.GenericIcons);
        _currentIcon = _iconSuccess;

        OVRProjectSetup.ProcessorQueue.OnProcessorCompleted += RefreshData;
        EditorApplication.update += Update;
    }

    private static void Update()
    {
        if (_appStatusBar == null)
        {
            Refresh();
        }
    }

    private static void Refresh()
    {
        var toolbars = Resources.FindObjectsOfTypeAll(_toolbarType);
        if (toolbars == null || toolbars.Length == 0)
        {
            return;
        }

        _appStatusBar = toolbars[0];

        var backend = _guiBackend?.GetValue(_appStatusBar);
        if (backend == null)
        {
            return;
        }

        var elements = _visualTree?.GetValue(backend, null) as VisualElement;
        _container = elements?[0];
        if (_container == null)
        {
            return;
        }

        var handler = _onGuiHandler?.GetValue(_container) as Action;
        if (handler == null)
        {
            return;
        }

        handler -= RefreshGUI;
        handler += RefreshGUI;
        _onGuiHandler.SetValue(_container, handler);
    }

    private static void RefreshStyles()
    {
        if (_iconStyle != null)
        {
            return;
        }

        _iconStyle = new GUIStyle("StatusBarIcon");
    }

    public static OVRGUIContent ComputeIcon(OVRConfigurationTaskUpdaterSummary summary)
    {
        if (summary == null)
        {
            return _iconSuccess;
        }

        var icon = summary.HighestFixLevel switch
        {
            OVRProjectSetup.TaskLevel.Optional => _iconNeutral,
            OVRProjectSetup.TaskLevel.Recommended => _iconWarning,
            OVRProjectSetup.TaskLevel.Required => _iconError,
            _ => _iconSuccess
        };

        icon.Tooltip = $"{summary.ComputeNoticeMessage()}\n{OpenOculusSettings}";

        return icon;
    }

    private static void RefreshData(OVRConfigurationTaskProcessor processor)
    {
        var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        if (processor.Type == OVRConfigurationTaskProcessor.ProcessorType.Updater
            && processor.BuildTargetGroup == activeBuildTargetGroup)
        {
            var updater = processor as OVRConfigurationTaskUpdater;
            _currentIcon = ComputeIcon(updater?.Summary);
        }
    }

    private static void RefreshGUI()
    {
        if (!OVRProjectSetup.ShowStatusIcon.Value)
        {
            return;
        }

        RefreshStyles();

        var screenWidth = _container.layout.width;
        // Hardcoded position
        // Currently overlaps with progress bar, and works with 2020 status bar icons
        // TODO: Better hook to dynamically position the button
        var currentRect = new Rect(screenWidth - 130, 0, 26, 30); // Hardcoded position
        GUILayout.BeginArea(currentRect);
        if (GUILayout.Button(_currentIcon, _iconStyle))
        {
            OVRStatusMenu.ShowDropdown(GUIUtility.GUIToScreenPoint(Vector2.zero));
        }

        var buttonRect = GUILayoutUtility.GetLastRect();
        EditorGUIUtility.AddCursorRect(buttonRect, MouseCursor.Link);
        GUILayout.EndArea();
    }
}
