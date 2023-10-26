/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Sample = Meta.Voice.Hub.Content.Sample;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using PackageSample = UnityEditor.PackageManager.UI.Sample;

namespace Meta.Voice.Hub.Inspectors
{
    [CustomEditor(typeof(Sample))]
    public class SampleEditor : Editor
    {
        #if VSDK_INTERNAL
        private bool edit = true;
        #else
        private bool edit = false;
        #endif
        private Sample _sample;

        // Layout
        private const float _sampleMargin = 5f;
        private const float _sampleButtonHeight = 30f;
        private const float _sampleIconMaxWidth = 540f;
        private static GUIStyle _sampleTitleStyle;
        private static GUIStyle _sampleWordwrapStyle;
        private static GUIStyle _sampleSizeStyle;

        // Text
        private const string _textOpenSample = "Open Sample";
        private const string _textImportSample = "Import Sample";
        private const string _textInsideSample = "Currently Open";
        private const string _textMissingSample = "Sample Not Found";

        private void OnEnable()
        {
            _sample = (Sample) target;
        }

        public override void OnInspectorGUI()
        {
            DrawSample(_sample, EditorGUIUtility.currentViewWidth - 40f, true);

            #if VSDK_INTERNAL
            GUILayout.Space(16);
            edit = EditorGUILayout.Foldout(edit, "Edit");
            #endif

            if (edit)
            {
                base.OnInspectorGUI();
            }
        }

        /// <summary>
        /// Draw method for a sample
        /// </summary>
        /// <param name="sample">Sample to be drawn</param>
        /// <param name="tileSize">Width of a tile</param>
        public static void DrawSample(Sample sample, float tileSize, bool showDescription = false)
        {
            // Generate all required styles
            InitStyles();

            // Begin
            GUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(tileSize));
            GUILayout.BeginHorizontal();
            GUILayout.Space(_sampleMargin);
            GUILayout.BeginVertical();

            // Image
            if (sample.tileImage != null)
            {
                float imageWidth = tileSize - _sampleMargin * 2f;
                imageWidth = Mathf.Min(imageWidth, sample.tileImage.width, _sampleIconMaxWidth);
                float imageHeight = imageWidth * sample.tileImage.height / sample.tileImage.width;
                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label(sample.tileImage, GUILayout.Width(imageWidth), GUILayout.Height(imageHeight));
                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();
            }

            // Title
            GUILayout.Label(sample.title, _sampleTitleStyle, GUILayout.Height(_sampleTitleStyle.lineHeight * 2));
            GUILayout.Space(_sampleMargin);

            // Description if applicable
            if (showDescription)
            {
                GUILayout.Label(sample.description, _sampleWordwrapStyle);
                GUILayout.Space(_sampleMargin);
            }

            // Open/Import button
            DrawOpenSampleButton(sample);
            GUILayout.Space(_sampleMargin);

            // Complete
            GUILayout.EndVertical();
            GUILayout.Space(_sampleMargin);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        // Draw button for open sample
        public static void DrawOpenSampleButton(Sample sample)
        {
            // Get sample package if possible
            PackageSample packagedSample = GetPackageSample(sample);

            // Already opened
            if (IsSampleOpened(sample))
            {
                bool defaultEnabled = GUI.enabled;
                GUI.enabled = false;
                GUILayout.Button(_textInsideSample, GUILayout.Height(_sampleButtonHeight));
                GUI.enabled = defaultEnabled;
            }
            // Open
            else if (CanOpenSample(sample, packagedSample))
            {
                // Show open button
                if (GUILayout.Button(_textOpenSample, GUILayout.Height(_sampleButtonHeight)))
                {
                    OpenSample(sample, packagedSample);
                }
            }
            // Import
            else if (IsPackageSampleValid(packagedSample) && !packagedSample.isImported)
            {
                // Show import button
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(_textImportSample, GUILayout.Height(_sampleButtonHeight)))
                {
                    ImportSample(packagedSample);
                }
                // Show import size
                GUIContent sizeContent = new GUIContent(GetSampleSize(packagedSample));
                float sizeWidth = _sampleSizeStyle.CalcSize(sizeContent).x;
                GUILayout.Label(sizeContent, _sampleSizeStyle, GUILayout.Width(sizeWidth), GUILayout.Height(_sampleButtonHeight));
                GUILayout.EndHorizontal();
            }
            // Error log
            else
            {
                Color defaultColor = GUI.color;
                bool defaultEnabled = GUI.enabled;
                GUI.color = Color.red;
                GUI.enabled = false;
                GUILayout.Button(_textMissingSample, GUILayout.Height(_sampleButtonHeight));
                GUI.color = defaultColor;
                GUI.enabled = defaultEnabled;
            }
        }

        private static void InitStyles()
        {
            if (null == _sampleTitleStyle)
            {
                _sampleTitleStyle = new GUIStyle(EditorStyles.boldLabel);
                _sampleTitleStyle.wordWrap = true;
                _sampleTitleStyle.fontSize = 16;
                _sampleTitleStyle.alignment = TextAnchor.UpperLeft;
            }

            if (null == _sampleWordwrapStyle)
            {
                _sampleWordwrapStyle = new GUIStyle(EditorStyles.label);
                _sampleWordwrapStyle.wordWrap = true;
            }

            if (null == _sampleSizeStyle)
            {
                _sampleSizeStyle = new GUIStyle(EditorStyles.label);
                _sampleSizeStyle.alignment = TextAnchor.MiddleRight;
                _sampleSizeStyle.fontSize = 12;
            }
        }

        /// <summary>
        /// Checks if a sample is currently opened
        /// </summary>
        /// <param name="sample">The sample asset data referencing the sample scene.</param>
        public static bool IsSampleOpened(Sample sample)
        {
            if (sample.sceneReference != null)
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (scene.isLoaded && string.Equals(sample.sceneReference.name, scene.name))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if a sample can be opened, if false the sample must be imported.
        /// </summary>
        /// <param name="sample">The sample data to be checked</param>
        /// <returns>True if sample is imported & can be opened</returns>
        public static bool CanOpenSample(Sample sample) => CanOpenSample(sample, GetPackageSample(sample));
        // Checks if sample scene reference can be opened or if a packaged sample is imported
        private static bool CanOpenSample(Sample sample, PackageSample packagedSample)
        {
            return sample.sceneReference != null || (IsPackageSampleValid(packagedSample) && packagedSample.isImported);
        }

        /// <summary>
        /// Opens a sample if possible
        /// </summary>
        /// <param name="sample">The sample data asset to be opened</param>
        public static void OpenSample(Sample sample) => OpenSample(sample, GetPackageSample(sample));
        // Opens a sample scene if found, otherwise attempts to find and open sample scene in package
        private static void OpenSample(Sample sample, PackageSample packagedSample)
        {
            // Open via sample reference
            if (sample.sceneReference != null)
            {
                string scenePath = AssetDatabase.GetAssetPath(sample.sceneReference);
                EditorSceneManager.OpenScene(scenePath);
                return;
            }
            // Failure
            Debug.LogError($"HUB Sample - Cannot Open Sample\nSample: {sample.title}");
        }

        /// <summary>
        /// Imports a sample into the current scene
        /// </summary>
        public static void ImportSample(PackageSample sample)
        {
            if (!sample.isImported)
            {
                sample.Import(PackageSample.ImportOptions.OverridePreviousImports);
            }
        }

        // Returns package sample data struct if specified sample name matches the asset's package name & version
        private static PackageSample GetPackageSample(Sample sample)
        {
            string sampleAssetPath = AssetDatabase.GetAssetPath(sample);
            PackageInfo info = PackageInfo.FindForAssetPath(sampleAssetPath);
            if (info != null && !string.IsNullOrEmpty(info.packageId))
            {
                foreach (var packageSample in PackageSample.FindByPackage(info.name, info.version))
                {
                    if (string.Equals(packageSample.displayName, sample.packageSampleName))
                    {
                        return packageSample;
                    }
                }
            }
            return new PackageSample();
        }
        // Returns true if the package sample struct is not empty
        private static bool IsPackageSampleValid(PackageSample packagedSample)
        {
            return !string.IsNullOrEmpty(packagedSample.displayName);
        }

        /// <summary>
        /// Determine size of a sample
        /// </summary>
        /// <param name="sample">The sample data asset to be opened</param>
        public static void GetSampleSize(Sample sample) => GetSampleSize(GetPackageSample(sample));
        // Gets sample size from from packaged sample
        private static string GetSampleSize(PackageSample sample)
        {
            if (string.IsNullOrEmpty(sample.resolvedPath) || !Directory.Exists(sample.resolvedPath))
                return "0 KB";
            return ConvertToString(GetDirectorySize(sample.resolvedPath));
        }
        private static ulong GetDirectorySize(string directory)
        {
            ulong sizeInBytes = 0;
            foreach (string fileName in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
            {
                FileInfo fileInfo = new FileInfo(fileName);
                sizeInBytes += (ulong) fileInfo.Length;
            }
            return sizeInBytes;
        }
        private static string ConvertToString(ulong sizeInBytes)
        {
            double size = sizeInBytes / _sizeUnitMax;
            int index = 0;
            while (size >= _sizeUnitMax && index < _sizeUnits.Length - 1)
            {
                ++index;
                size /= _sizeUnitMax;
            }
            return $"{size:0.##} {_sizeUnits[index]}";
        }
        private const double _sizeUnitMax = 1024.0;
        private static readonly string[] _sizeUnits = new string[4]
        {
            "KB",
            "MB",
            "GB",
            "TB"
        };
    }
}
