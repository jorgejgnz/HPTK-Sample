/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.WitAi.Data.Configuration.Tabs;
using Lib.Wit.Runtime.Requests;
using Meta.Conduit.Editor;
using Meta.WitAi.Configuration;
using Meta.WitAi.Data.Configuration;
using Meta.Conduit;
using Meta.Voice.TelemetryUtilities;
using Meta.WitAi.Lib;
using UnityEditor;
using UnityEngine;
using Meta.WitAi.Windows.Components;

namespace Meta.WitAi.Windows
{
    [InitializeOnLoadAttribute]
    public class WitConfigurationEditor : Editor
    {
        private ConduitManifestGenerationManager _conduitManifestGenerationManager;

        public WitConfiguration Configuration {
            get => _configuration;
            private set
            {
                if (_configuration == value)
                {
                    return;
                }

                _configuration = value;
                _conduitManifestGenerationManager = ConduitManifestGenerationManager.GetInstance(_configuration);
            }
        }
        private WitConfiguration _configuration;
        private string _serverToken;
        private string _appName;
        private string _appID;
        private bool _initialized = false;
        public bool drawHeader = true;
        private bool _foldout = true;
        private int _requestTab = 0;
        private bool _syncInProgress = false;
        private bool _didCheckAutoTrainAvailability = false;
        private bool _isAutoTrainAvailable = false;

        /// <summary>
        /// Whether or not server specific functionality like sync
        /// should be disabled for this configuration
        /// </summary>
        protected virtual bool _disableServerPost => false;
        private static readonly ManifestLoader ManifestLoader = new ManifestLoader();
        private static readonly IWitVRequestFactory VRequestFactory = new WitVRequestFactory();

        private EnumSynchronizer _enumSynchronizer;

        private static Type[] _tabTypes;
        private WitConfigurationEditorTab[] _tabs;

        private const string ENTITY_SYNC_CONSENT_KEY = "Conduit.EntitySync.Consent";

        protected virtual Texture2D HeaderIcon => WitTexts.HeaderIcon;
        public virtual string HeaderUrl => WitTexts.GetAppURL(Configuration.GetApplicationId(), WitTexts.WitAppEndpointType.Settings);
        protected virtual string DocsUrl => WitTexts.Texts.WitDocsUrl;
        protected virtual string OpenButtonLabel => WitTexts.Texts.WitOpenButtonLabel;

        public void Initialize()
        {
            // Shared between all WitConfigurationEditors
            if (_tabTypes == null)
            {
                _tabTypes = typeof(WitConfigurationEditorTab).GetSubclassTypes().ToArray();
            }
            // Generate tab instances
            if (_tabs == null)
            {
                _tabs = _tabTypes.Select(type => (WitConfigurationEditorTab)Activator.CreateInstance(type))
                    .OrderBy(tab =>tab.TabOrder)
                    .ToArray();
            }

            // Refresh configuration & auth tokens
            Configuration = target as WitConfiguration;

            // Get app server token
            _serverToken = WitAuthUtility.GetAppServerToken(Configuration);
            if (CanConfigurationRefresh(Configuration) && WitConfigurationUtility.IsServerTokenValid(_serverToken))
            {
                // Get client token if needed
                _appID = Configuration.GetApplicationId();
                if (string.IsNullOrEmpty(_appID))
                {
                    Configuration.SetServerToken(_serverToken);
                }
                // Refresh additional data
                else
                {
                    SafeRefresh();
                }
            }
        }

        public void OnDisable()
        {
            ConduitManifestGenerationManager.PersistStatistics();
        }

        public override void OnInspectorGUI()
        {
            // Init if needed
            if (!_initialized || Configuration != target)
            {
                Initialize();
                _initialized = true;
            }

            // Draw header
            WitEditorUI.LayoutHeaderText(target.name, HeaderUrl, DocsUrl);


            // Layout content
            LayoutContent();
        }

        private void LayoutConduitContent()
        {
            if (_conduitManifestGenerationManager == null)
            {
                _conduitManifestGenerationManager = ConduitManifestGenerationManager.GetInstance(Configuration);
            }

            var isServerTokenValid = WitConfigurationUtility.IsServerTokenValid(_serverToken);
            if (!isServerTokenValid && !_disableServerPost)
            {
                GUILayout.TextArea(WitTexts.Texts.ConfigurationConduitMissingTokenLabel, WitStyles.LabelError);
            }

            EditorGUI.indentLevel++;

            // Set conduit
            var updated = false;
            WitEditorUI.LayoutToggle(new GUIContent(WitTexts.Texts.ConfigurationConduitUseConduitLabel), ref Configuration.useConduit, ref updated);
            if (updated)
            {
                EditorUtility.SetDirty(Configuration);
            }

            // Configuration buttons
            GUILayout.Space(EditorGUI.indentLevel * WitStyles.ButtonMargin);
            {
                GUI.enabled = Configuration.useConduit;
                updated = false;
                WitEditorUI.LayoutToggle(
                    new GUIContent(WitTexts.Texts.ConfigurationConduitRelaxedResolutionsLabel,
                        WitTexts.Texts.ConfigurationConduitRelaxedResolutionsTooltip),
                    ref Configuration.relaxedResolution, ref updated);
                if (updated)
                {
                    EditorUtility.SetDirty(Configuration);
                }

                GUILayout.BeginHorizontal();
                {
                    if (_conduitManifestGenerationManager != null && WitEditorUI.LayoutTextButton(_conduitManifestGenerationManager.ManifestAvailable ? WitTexts.Texts.ConfigurationConduitUpdateManifestLabel : WitTexts.Texts.ConfigurationConduitGenerateManifestLabel))
                    {
                        _conduitManifestGenerationManager.GenerateManifest(Configuration, true);
                    }

                    GUI.enabled = Configuration.useConduit && _conduitManifestGenerationManager.ManifestAvailable ;
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationConduitSelectManifestLabel) && _conduitManifestGenerationManager.ManifestAvailable )
                    {
                        Selection.activeObject =
                            AssetDatabase.LoadAssetAtPath<TextAsset>(Configuration.GetManifestEditorPath());
                    }

                    GUI.enabled = Configuration.useConduit;
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationConduitSpecifyAssembliesLabel))
                    {
                        PresentAssemblySelectionDialog();
                    }

                    if (isServerTokenValid && !_disableServerPost)
                    {
                        GUI.enabled = Configuration.useConduit && _conduitManifestGenerationManager.ManifestAvailable  && !_syncInProgress;
                        if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationConduitSyncEntitiesLabel))
                        {
                            SyncEntities();
                            GUIUtility.ExitGUI();
                            return;
                        }
                        if (_isAutoTrainAvailable)
                        {
                            if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationConduitAutoTrainLabel) && _conduitManifestGenerationManager.ManifestAvailable )
                            {
                                SyncEntities(() => { AutoTrainOnWitAi(Configuration); });
                            }
                        }
                    }
                    GUI.enabled = true;
                }
                GUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }

        protected virtual void LayoutContent()
        {
            // Begin vertical box
            GUILayout.BeginVertical(EditorStyles.helpBox);

            // Check for app name/id update
            ReloadAppData();

            // Title Foldout
            GUILayout.BeginHorizontal();
            string foldoutText = WitTexts.Texts.ConfigurationHeaderLabel;
            if (!string.IsNullOrEmpty(_appName))
            {
                foldoutText = foldoutText + " - " + _appName;
            }

            _foldout = WitEditorUI.LayoutFoldout(new GUIContent(foldoutText), _foldout);
            // Refresh button
            if (CanConfigurationRefresh(Configuration))
            {
                if (string.IsNullOrEmpty(_appName))
                {
                    bool isValid =  WitConfigurationUtility.IsServerTokenValid(_serverToken);
                    GUI.enabled = isValid;
                    if (WitEditorUI.LayoutTextButton(WitTexts.Texts.ConfigurationRefreshButtonLabel))
                    {
                        ApplyServerToken(_serverToken);
                    }
                }
                else
                {
                    bool isRefreshing = Configuration.IsUpdatingData();
                    GUI.enabled = !isRefreshing;
                    if (WitEditorUI.LayoutTextButton(isRefreshing ? WitTexts.Texts.ConfigurationRefreshingButtonLabel : WitTexts.Texts.ConfigurationRefreshButtonLabel))
                    {
                        SafeRefresh();
                    }
                }
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
            GUILayout.Space(WitStyles.ButtonMargin);

            // Show configuration app data
            if (_foldout)
            {
                // Indent
                EditorGUI.indentLevel++;

                // Server access token
                bool updated = false;
                WitEditorUI.LayoutPasswordField(WitTexts.ConfigurationServerTokenContent, ref _serverToken, ref updated);

                if (updated && WitConfigurationUtility.IsServerTokenValid(_serverToken))
                {
                    ApplyServerToken(_serverToken);
                }

                // Additional data
                if (Configuration)
                {
                    LayoutConfigurationData();
                }

                // Undent
                EditorGUI.indentLevel--;
            }

            // End vertical box layout
            GUILayout.EndVertical();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            LayoutConduitContent();
            GUILayout.EndVertical();


            // Layout configuration request tabs
            LayoutConfigurationRequestTabs();

            // Additional open wit button
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(OpenButtonLabel, WitStyles.TextButton))
            {
                Application.OpenURL(HeaderUrl);
            }
        }
        // Reload app data if needed
        private void ReloadAppData()
        {
            // Check for changes
            string checkID = "";
            string checkName = "";
            if (Configuration != null)
            {
                checkID = Configuration.GetApplicationId();
                if (!string.IsNullOrEmpty(checkID))
                {
                    checkName = Configuration.GetApplicationInfo().name;
                }
            }
            // Reset
            if (!string.Equals(_appName, checkName) || !string.Equals(_appID, checkID))
            {
                // Refresh app data
                _appName = checkName;
                _appID = checkID;

                // Do not clear token if failed to set
                string newToken = WitAuthUtility.GetAppServerToken(Configuration);
                if (!string.IsNullOrEmpty(newToken))
                {
                    _serverToken = newToken;
                }
            }
        }
        // Apply server token
        public void ApplyServerToken(string newToken)
        {
            if (newToken != _serverToken)
            {
                _serverToken = newToken;
                Configuration.ResetData();
            }

            WitAuthUtility.ServerToken = _serverToken;
            Configuration.SetServerToken(_serverToken);

            _conduitManifestGenerationManager.GenerateManifest(Configuration, false);
        }
        // Whether or not to allow a configuration to refresh
        protected virtual bool CanConfigurationRefresh(WitConfiguration configuration)
        {
            return configuration;
        }
        // Layout configuration data
        protected virtual void LayoutConfigurationData()
        {
            // Reset update
            bool updated = false;
            // Client access field
            string clientAccessToken = Configuration.GetClientAccessToken();
            WitEditorUI.LayoutPasswordField(WitTexts.ConfigurationClientTokenContent, ref clientAccessToken, ref updated);
            if (updated && string.IsNullOrEmpty(clientAccessToken))
            {
                VLog.E("Client access token is not defined. Cannot perform requests with '" + Configuration.name + "'.");
            }
            // Timeout field
            WitEditorUI.LayoutIntField(WitTexts.ConfigurationRequestTimeoutContent, ref Configuration.timeoutMS, ref updated);
            // Updated
            if (updated)
            {
                Configuration.SetClientAccessToken(clientAccessToken);
            }

            // Show configuration app data
            LayoutConfigurationEndpoint();
        }
        // Layout endpoint data
        protected virtual void LayoutConfigurationEndpoint()
        {
            // Generate if needed
            if (Configuration.endpointConfiguration == null)
            {
                Configuration.endpointConfiguration = new WitEndpointConfig();
                EditorUtility.SetDirty(Configuration);
            }

            // Handle via serialized object
            var serializedObj = new SerializedObject(Configuration);
            var serializedProp = serializedObj.FindProperty("endpointConfiguration");
            EditorGUILayout.PropertyField(serializedProp);
            serializedObj.ApplyModifiedProperties();
        }
        // Tabs
        protected virtual void LayoutConfigurationRequestTabs()
        {
            // Application info
            Data.Info.WitAppInfo appInfo = Configuration.GetApplicationInfo();
            // Indent
            EditorGUI.indentLevel++;

            // Iterate tabs
            if (_tabs != null)
            {
                GUILayout.BeginHorizontal();
                for (int i = 0; i < _tabs.Length; i++)
                {
                    // Enable if not selected
                    GUI.enabled = _requestTab != i;
                    // If valid and clicked, begin selecting
                    if (null != appInfo.id &&
                        (_tabs[i].ShouldTabShow(appInfo) || _tabs[i].ShouldTabShow(Configuration)))
                    {
                        if (WitEditorUI.LayoutTabButton(_tabs[i].GetTabText(true)))
                        {
                            _requestTab = i;
                        }
                    }
                    // If invalid, stop selecting
                    else if (_requestTab == i)
                    {
                        _requestTab = -1;
                    }
                }

                GUI.enabled = true;
                GUILayout.EndHorizontal();

                // Layout selected tab using property id
                string propertyID = _requestTab >= 0 && _requestTab < _tabs.Length
                    ? _tabs[_requestTab].TabID
                    : string.Empty;
                if (!string.IsNullOrEmpty(propertyID) && Configuration != null)
                {
                    var newConfigData = Array.Find(Configuration.GetConfigData(), d => d.GetType() == _tabs[_requestTab].DataType);

                    SerializedObject serializedObj;
                    if (newConfigData == null)
                    {
                        serializedObj = new SerializedObject(Configuration);
                    }
                    else
                    {
                        serializedObj = new SerializedObject(newConfigData);
                    }

                    SerializedProperty serializedProp = serializedObj.FindProperty(_tabs[_requestTab].GetPropertyName(propertyID));
                    if (serializedProp == null)
                    {
                        WitEditorUI.LayoutErrorLabel(_tabs[_requestTab].GetTabText(false));
                    }
                    else if (!serializedProp.isArray)
                    {
                        EditorGUILayout.PropertyField(serializedProp);
                    }
                    else if (serializedProp.arraySize == 0)
                    {
                        WitEditorUI.LayoutErrorLabel(_tabs[_requestTab].GetTabText(false));
                    }
                    else
                    {
                        for (int i = 0; i < serializedProp.arraySize; i++)
                        {
                            SerializedProperty serializedPropChild = serializedProp.GetArrayElementAtIndex(i);
                            EditorGUILayout.PropertyField(serializedPropChild);
                        }
                    }
                    serializedObj.ApplyModifiedProperties();
                }
            }

            // Undent
            EditorGUI.indentLevel--;
        }

        // Safe refresh
        protected virtual void SafeRefresh()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;

            if (WitConfigurationUtility.IsServerTokenValid(_serverToken))
            {
                Configuration.SetServerToken(_serverToken);
                Configuration.UpdateDataAssets();
            }
            else if (WitConfigurationUtility.IsClientTokenValid(Configuration.GetClientAccessToken()))
            {
                Configuration.RefreshAppInfo();
                Configuration.UpdateDataAssets();
            }
            if (Configuration.useConduit)
            {
                CheckAutoTrainAvailabilityIfNeeded();
            }
        }

        private void CheckAutoTrainAvailabilityIfNeeded()
        {
            if (_didCheckAutoTrainAvailability || !WitConfigurationUtility.IsServerTokenValid(_serverToken)) {
                return;
            }

            _didCheckAutoTrainAvailability = true;
            CheckAutoTrainIsAvailable(Configuration, (isAvailable) => {
                _isAutoTrainAvailable = isAvailable;
                Telemetry.LogInstantEvent(Telemetry.TelemetryEventId.CheckAutoTrain, new Dictionary<Telemetry.AnnotationKey, string>
                {
                    { Telemetry.AnnotationKey.IsAvailable, isAvailable.ToString() }
                });
            });
        }

        // Show dialog to disable/enable assemblies
        private void PresentAssemblySelectionDialog()
        {
            var assemblyWalker = _conduitManifestGenerationManager.AssemblyWalker;
            var assemblyNames = assemblyWalker.GetAllAssemblies().Select(a => a.FullName).ToList();
            assemblyWalker.AssembliesToIgnore = new HashSet<string>(Configuration.excludedAssemblies);
            WitMultiSelectionPopup.Show(assemblyNames, assemblyWalker.AssembliesToIgnore, (disabledAssemblies) => {
                assemblyWalker.AssembliesToIgnore = new HashSet<string>(disabledAssemblies);
                Configuration.excludedAssemblies = new List<string>(assemblyWalker.AssembliesToIgnore);
                _conduitManifestGenerationManager.GenerateManifest(Configuration, false);
            });
        }

        // Sync entities
        private void SyncEntities(Action successCallback = null)
        {
            var instanceKey = Telemetry.StartEvent(Telemetry.TelemetryEventId.SyncEntities);

            if (!EditorUtility.DisplayDialog("Synchronizing with Wit.Ai entities", "This will synchronize local enums with Wit.Ai entities. Part of this process involves generating code locally and may result in overwriting existing code. Please make sure to backup your work before proceeding.", "Proceed", "Cancel", DialogOptOutDecisionType.ForThisSession, ENTITY_SYNC_CONSENT_KEY))
            {
                Telemetry.EndEvent(instanceKey, Telemetry.ResultType.Cancel);
                VLog.D("Entity Sync cancelled");
                return;
            }

            // Fail without server token
            var validServerToken = WitConfigurationUtility.IsServerTokenValid(_serverToken);
            if (!validServerToken)
            {
                Telemetry.EndEventWithFailure(instanceKey, "Invalid server token");
                VLog.E($"Conduit Sync Failed\nError: Invalid server token");
                return;
            }

            // Generate
            if (_enumSynchronizer == null)
            {
                var assemblyWalker = _conduitManifestGenerationManager.AssemblyWalker;
                _enumSynchronizer = new EnumSynchronizer(Configuration, assemblyWalker, new FileIo(), VRequestFactory);
            }

            // Sync
            _syncInProgress = true;
            EditorUtility.DisplayProgressBar("Conduit Entity Sync", "Generating Manifest.", 0f );
            _conduitManifestGenerationManager.GenerateManifest(Configuration, false);

            var manifest = LoadManifest(Configuration.ManifestLocalPath);

            const float initializationProgress = 0.1f;
            EditorUtility.DisplayProgressBar("Conduit Entity Sync", "Synchronizing entities. Please wait...", initializationProgress);
            VLog.D("Synchronizing enums with Wit.Ai entities");
            CoroutineUtility.StartCoroutine(_enumSynchronizer.SyncWitEntities(manifest, (success, data) =>
                {
                    _syncInProgress = false;
                    EditorUtility.ClearProgressBar();
                    if (!success)
                    {
                        Telemetry.EndEventWithFailure(instanceKey, data);
                        VLog.E($"Conduit failed to synchronize entities\nError: {data}");
                    }
                    else
                    {
                        Telemetry.EndEvent(instanceKey, Telemetry.ResultType.Success);
                        VLog.D("Conduit successfully synchronized entities");
                        successCallback?.Invoke();
                    }
                },
                (status, progress) =>
                {
                    EditorUtility.DisplayProgressBar("Conduit Entity Sync", status,
                        initializationProgress + (1f - initializationProgress) * progress);
                }));
        }

        private void AutoTrainOnWitAi(WitConfiguration configuration)
        {
            var instanceKey = Telemetry.StartEvent(Telemetry.TelemetryEventId.AutoTrain);
            var manifest = LoadManifest(configuration.ManifestLocalPath);

            var intents = _conduitManifestGenerationManager.ExtractManifestData();
            VLog.D($"Auto training on WIT.ai: {intents.Count} intents.");

            configuration.ImportData(manifest, (isSuccess, error) =>
            {
                if (isSuccess)
                {
                    Telemetry.EndEvent(instanceKey, Telemetry.ResultType.Success);
                    EditorUtility.DisplayDialog("Auto Train", "Successfully started auto train process on WIT.ai.",
                        "OK");
                }
                else
                {
                    var failureMessage =
                        $"Failed to import generated manifest JSON into WIT.ai: {error}. Manifest:\n{manifest}";
                    Telemetry.EndEventWithFailure(instanceKey, failureMessage);
                    VLog.E(failureMessage);
                    EditorUtility.DisplayDialog("Auto Train", "Failed to start auto train process on WIT.ai.", "OK");
                }
            });
        }

        private void CheckAutoTrainIsAvailable(WitConfiguration configuration, Action<bool> onComplete)
        {
            var appInfo = configuration.GetApplicationInfo();
            var manifestText = _conduitManifestGenerationManager.GenerateEmptyManifest(appInfo.name, appInfo.id);
            var manifest = ManifestLoader.LoadManifestFromString(manifestText);
            configuration.ImportData(manifest, (result, error) => onComplete(result), true);
        }

        private static Manifest LoadManifest(string manifestPath)
        {
            var instanceKey = Telemetry.StartEvent(Telemetry.TelemetryEventId.LoadManifest);

            var manifest = ManifestLoader.LoadManifest(manifestPath);

            if (manifest == null)
            {
                Telemetry.EndEventWithFailure(instanceKey);
            }
            Telemetry.EndEvent(instanceKey, Telemetry.ResultType.Success);

            return manifest;
        }
    }
}
