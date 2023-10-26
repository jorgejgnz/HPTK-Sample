/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.IO;
using Meta.Conduit.Editor;
using Meta.Voice.TelemetryUtilities;
using Meta.WitAi.Data.Configuration;
using Meta.WitAi.Utilities;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Meta.WitAi.Windows
{
    /// <summary>
    /// Manages the Conduit manifest generation.
    /// </summary>
    public class ConduitManifestGenerationManager: IPreprocessBuildWithReport
    {
        /// <summary>
        /// The priority for preprocess build callback.
        /// </summary>
        public int callbackOrder => 0;
        
        /// <summary>
        /// The assembly miner.
        /// </summary>
        private static readonly AssemblyMiner AssemblyMiner = new AssemblyMiner(new WitParameterValidator());
        
        /// <summary>
        /// Maps individual configurations to their associated managers. This is needed to allow static resolution
        /// on global events like building or running.
        /// </summary>
        private static readonly Dictionary<string, ConduitManifestGenerationManager> ConfigurationToManagerMap =
            new Dictionary<string, ConduitManifestGenerationManager>();
        
        /// <summary>
        /// Locally collected Conduit statistics.
        /// </summary>
        private static ConduitStatistics _statistics;
        
        /// <summary>
        /// Set to true if code had changed since last manifest generation.
        /// We start with this set to true to handle changes when the editor is not running.
        /// </summary>
        private static bool _codeChanged = true;
        
        /// <summary>
        /// The manifest generator used for this configuration.
        /// </summary>
        private readonly ManifestGenerator _manifestGenerator;
        
        /// <summary>
        /// True when a manifest exists locally for this configuration.
        /// </summary>
        public bool ManifestAvailable { get; private set; }= false;

        /// <summary>
        /// The assembly walker associated with this configuration.
        /// </summary>
        internal AssemblyWalker AssemblyWalker { get; private set; } = new AssemblyWalker();
        
        /// <summary>
        /// Locally collected Conduit statistics.
        /// </summary>
        private static ConduitStatistics Statistics
        {
            get
            {
                if (_statistics == null)
                {
                    _statistics = new ConduitStatistics(new PersistenceLayer());
                }
                return _statistics;
            }
        }

        /// <summary>
        /// Factory method that creates a manager for the configuration if none exists. Otherwise, creates a new one.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <returns>An instance of this class.</returns>
        public static ConduitManifestGenerationManager GetInstance(WitConfiguration configuration)
        {
            // This key has to match what we set in the constructor. 
            var configurationKey = configuration.name;
            if (!ConfigurationToManagerMap.ContainsKey(configurationKey))
            {
                ConfigurationToManagerMap[configurationKey] = new ConduitManifestGenerationManager(configuration);
            }
            
            ConfigurationToManagerMap[configurationKey].GenerateManifestIfNeeded(configuration);
            
            return ConfigurationToManagerMap[configurationKey];
        }

        static ConduitManifestGenerationManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
        }

        /// <summary>
        /// This default constructor is intended to be used by Unity only. Do not call it directly.
        /// </summary>
        public ConduitManifestGenerationManager()
        {
        }

        private ConduitManifestGenerationManager(WitConfiguration configuration)
        {
            _manifestGenerator = new ManifestGenerator(AssemblyWalker, AssemblyMiner);
            ConfigurationToManagerMap[configuration.name] = this;
        }
        
        public void OnPreprocessBuild(BuildReport report)
        {
            if (_codeChanged)
            {
                GenerateAllManifests();
            }
            else
            {
                GenerateMissingManifests();
            }
        }
        
        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            _codeChanged = true;
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode)
            {
                return;
            }
            if (_codeChanged)
            {
                GenerateAllManifests();
            }
            else
            {
                GenerateMissingManifests();
            }
        }

        public static void PersistStatistics()
        {
            Statistics.Persist();
        }

        private static void GenerateMissingManifests()
        {
            foreach (var configuration in WitConfigurationUtility.GetLoadedConfigurations())
            {
                if (configuration == null || !configuration.useConduit)
                {
                    continue;
                }
                
                var manager = GetInstance(configuration);
                manager.GenerateManifest(configuration, false);
            }
        }
        
        private static void GenerateAllManifests()
        {
            foreach (var configuration in WitConfigurationUtility.GetLoadedConfigurations())
            {
                if (configuration != null && configuration.useConduit)
                {
                    var manager = GetInstance(configuration);
                    manager.GenerateManifest(configuration, false);
                }
            }

            _codeChanged = false;
        }

        public List<string> ExtractManifestData()
        {
            return _manifestGenerator.ExtractManifestData();
        }

        /// <summary>
        /// Generate a manifest with empty entities and actions lists.
        /// </summary>
        /// <param name="domain">A friendly name to use for this app.</param>
        /// <param name="id">The App ID.</param>
        /// <returns>A JSON representation of the empty manifest.</returns>
        public string GenerateEmptyManifest(string domain, string id)
        {
            return _manifestGenerator.GenerateEmptyManifest(domain, id);
        }
        
        private void GenerateManifestIfNeeded(WitConfiguration configuration)
        {
            if (!configuration.useConduit || configuration == null)
            {
                return;
            }

            // Get full manifest path & ensure it exists
            var manifestPath = configuration.GetManifestEditorPath();
            ManifestAvailable = File.Exists(manifestPath);

            // Auto-generate manifest
            if (!ManifestAvailable)
            {
                GenerateManifest(configuration, false);
            }
        }

        private static string GetManifestFullPath(WitConfiguration configuration, bool shouldCreateDirectoryIfNotExist = false)
        {
            string directory = Application.dataPath + "/Oculus/Voice/Resources";
            if (shouldCreateDirectoryIfNotExist)
            {
                IOUtility.CreateDirectory(directory, true);
            }
            return directory + "/" + configuration.ManifestLocalPath;
        }

        
        /// <summary>
        /// Generates a manifest and optionally opens it in the editor.
        /// </summary>
        /// <param name="configuration">The configuration that we are generating the manifest for.</param>
        /// <param name="openManifest">If true, will open the manifest file in the code editor.</param>
        public void GenerateManifest(WitConfiguration configuration, bool openManifest)
        {
            var instanceKey = Telemetry.StartEvent(Telemetry.TelemetryEventId.GenerateManifest);
            AssemblyWalker.AssembliesToIgnore = new HashSet<string>(configuration.excludedAssemblies);

            // Generate
            var startGenerationTime = DateTime.UtcNow;
            var appInfo = configuration.GetApplicationInfo();
            var manifest = _manifestGenerator.GenerateManifest(appInfo.name, appInfo.id);
            var endGenerationTime = DateTime.UtcNow;

            // Get file path
            var fullPath = configuration.GetManifestEditorPath();
            if (string.IsNullOrEmpty(fullPath) || !File.Exists(fullPath))
            {
                fullPath = GetManifestFullPath(configuration, true);
            }

            // Write to file
            try
            {
                var writer = new StreamWriter(fullPath);
                writer.NewLine = "\n";
                writer.WriteLine(manifest);
                writer.Close();
            }
            catch (Exception e)
            {
                VLog.E($"Conduit manifest failed to generate\nPath: {fullPath}\n{e}");
                Telemetry.EndEventWithFailure(instanceKey, e.Message);
                return;
            }
            try
            {
                var incompatibleSignatures = string.Join(" ", AssemblyMiner.IncompatibleSignatureFrequency.Keys);
                Telemetry.AnnotateEvent(instanceKey, Telemetry.AnnotationKey.IncompatibleSignatures, incompatibleSignatures);

                var compatibleSignatures = string.Join(" ", AssemblyMiner.SignatureFrequency.Keys);
                Telemetry.AnnotateEvent(instanceKey, Telemetry.AnnotationKey.CompatibleSignatures, compatibleSignatures);
            }
            catch (Exception e)
            {
                VLog.W($"Failed to collect signature telemetry. Exception: {e}");
            }


            Telemetry.EndEvent(instanceKey, Telemetry.ResultType.Success);
            Statistics.SuccessfulGenerations++;
            Statistics.AddFrequencies(AssemblyMiner.SignatureFrequency);
            Statistics.AddIncompatibleFrequencies(AssemblyMiner.IncompatibleSignatureFrequency);
            var generationTime = endGenerationTime - startGenerationTime;
            var unityPath = fullPath.Replace(Application.dataPath, "Assets");
            AssetDatabase.ImportAsset(unityPath);
            ManifestAvailable = true;

            var configName = configuration.name;
            var manifestName = Path.GetFileNameWithoutExtension(unityPath);
            #if UNITY_2021_2_OR_NEWER
            var configPath = AssetDatabase.GetAssetPath(configuration);
            configName = $"<a href=\"{configPath}\">{configName}</a>";
            manifestName = $"<a href=\"{unityPath}\">{manifestName}</a>";
            #endif
            VLog.D($"Conduit manifest generated\nConfiguration: {configName}\nManifest: {manifestName}\nGeneration Time: {generationTime.TotalMilliseconds} ms");

            if (openManifest)
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(fullPath, 1);
            }
        }
    }
}
