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
using System.IO;
using UnityEditor;
using UnityEngine;

internal class OVRGUIContent
{
    public enum Source
    {
        BuiltIn,
        GenericIcons,
        ProjectSetupToolIcons,
        BuildingBlocksIcons,
        BuildingBlocksThumbnails,
        BuildingBlocksAnimations
    }

    private static string _rootPath;
    private static readonly Dictionary<Source, string> RelativePaths = new Dictionary<Source, string>();

    public static void RegisterContentPath(Source source, string relativePath)
    {
        RelativePaths.Add(source, relativePath);
    }

    private static bool GetRootPath(out string rootPath)
    {
        if (_rootPath == null)
        {
            var g = AssetDatabase.FindAssets($"t:Script {nameof(OVRGUIContent)}");
            if (g.Length > 0)
            {
                _rootPath = AssetDatabase.GUIDToAssetPath(g[0]);
                _rootPath = Path.GetDirectoryName(_rootPath);
                _rootPath = Path.GetDirectoryName(_rootPath);
            }
        }

        rootPath = _rootPath;
        return rootPath != null;
    }

    public static bool BuildPath(string path, Source source, out string contentPath)
    {
        contentPath = null;
        if (GetRootPath(out var rootPath))
        {
            if (RelativePaths.TryGetValue(source, out var relativePath))
            {
                contentPath = Path.Combine(Path.Combine(rootPath, relativePath), path);
            }
        }
        return contentPath != null;
    }

    public static implicit operator GUIContent(OVRGUIContent ovrContent)
    {
        return ovrContent.Content;
    }

    private GUIContent _content;
    private readonly string _name;
    private readonly Source _source;
    private readonly bool _builtIn;
    private string _tooltip;

    public string Tooltip
    {
        set
        {
            _tooltip = value;
            _content.tooltip = value;
        }
    }

    public OVRGUIContent(string name, Source source, string tooltip = null)
    {
        _name = name;
        _tooltip = tooltip;
        _source = source;
        _content = new GUIContent
        {
            image = null,
            tooltip = _tooltip
        };
    }

    public GUIContent Content
    {
        get
        {
            if (_content.image == null)
            {
                LoadContent();
            }

            return _content;
        }
    }

    public bool Valid => Content.image != null;

    private void LoadContent()
    {
        if (!OVREditorUtils.IsMainEditor())
        {
            return;
        }

        if (_source == Source.BuiltIn)
        {
            _content = EditorGUIUtility.TrIconContent(_name, _tooltip);
        }
        else if (BuildPath(_name, _source, out var fullPath))
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(fullPath);
            if (texture)
            {
                _content.image = texture;
            }
        }
    }
}
