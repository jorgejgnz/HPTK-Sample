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

using UnityEditor;
using UnityEngine.UIElements;

internal class OVRProjectSetupSettingsProvider : SettingsProvider
{
	public enum Origins
	{
		Settings,
		Menu,
		Icon,
		Console
	}

    [MenuItem("Oculus/Tools/Project Setup Tool", false, 1)]
    static void OpenProjectSetupTool()
    {
        OpenSettingsWindow(Origins.Menu);
    }

    public const string SettingsName = "Oculus";
    private static readonly string SettingsPath = $"Project/{SettingsName}";

    private OVRProjectSetupDrawer _ovrProjectSetupDrawer;
    private OVRProjectSetupDrawer OvrProjectSetupDrawer => _ovrProjectSetupDrawer ??= new OVRProjectSetupDrawer();
    private static Origins? _lastOrigin = null;
    private static bool _activated = false;

    [SettingsProvider]
    public static SettingsProvider CreateProjectValidationSettingsProvider()
    {
        return new OVRProjectSetupSettingsProvider(SettingsPath, SettingsScope.Project);
    }

    private OVRProjectSetupSettingsProvider(string path,
        SettingsScope scopes)
        : base(path, scopes) {}

    public override void OnActivate(string searchContext, VisualElement rootElement)
    {
	    if (!_activated)
	    {
		    _activated = true;
		    _lastOrigin = _lastOrigin ?? Origins.Settings;

	    }
    }

    public override void OnDeactivate()
    {
	    _lastOrigin = null;
	    _activated = false;
    }

    public override void OnTitleBarGUI()
    {
        base.OnTitleBarGUI();
        OvrProjectSetupDrawer.OnTitleBarGUI();
    }

    public override void OnGUI(string searchContext)
    {
        base.OnGUI(searchContext);
        OvrProjectSetupDrawer.OnGUI();
    }

    public static void OpenSettingsWindow(Origins origin)
    {
	    _lastOrigin = origin;
        var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        EditorUserBuildSettings.selectedBuildTargetGroup = buildTargetGroup;
        SettingsService.OpenProjectSettings(SettingsPath);
    }
}
