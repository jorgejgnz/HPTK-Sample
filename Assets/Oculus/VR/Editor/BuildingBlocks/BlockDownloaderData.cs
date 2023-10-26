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
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_2021_1_OR_NEWER
using System.IO.Compression;
#endif

namespace Meta.XR.BuildingBlocks.Editor
{
    internal class BlockDownloaderData : BlockBaseData
    {
        [SerializeField] private string zipFileUrl;
        public string ZipFileUrl => zipFileUrl;

        private string InstallPath => $"Assets/Plugins/{name}";

        private UnityWebRequest _www;
        private Action _onInstall;

#if UNITY_2021_1_OR_NEWER
        public override bool DisplayOnContentTab => !_isInstalled && base.DisplayOnContentTab;
#else
    public override bool DisplayOnContentTab => false;
#endif

#if UNITY_2021_1_OR_NEWER
        internal override bool CanBeAdded => !_isInstalled;
#else
    internal override bool CanBeAdded => false;
#endif

        [ContextMenu("Validate")]
        internal override void Validate()
        {
            base.Validate();

            Assert.IsFalse(string.IsNullOrEmpty(ZipFileUrl), $"{nameof(ZipFileUrl)} cannot be null or empty");
            Assert.IsTrue(ZipFileUrl.EndsWith(".zip"), $"{nameof(ZipFileUrl)} must be a zip file");
        }

        [ContextMenu("Check if is installed")]
        public void PrintIsInstalled()
        {
            Debug.Log($"{BlockName}'s SDK is installed: {_isInstalled}");
        }

        private bool _isInstalled;

        private void UpdateInstalledState()
        {
            _isInstalled = Directory.Exists(InstallPath) && (Directory.GetFiles(InstallPath).Length > 0 ||
                                                             Directory.GetDirectories(InstallPath).Length > 0);
        }

        private void OnEnable()
        {
            UpdateInstalledState();
        }

        internal override bool RequireListRefreshAfterInstall => true;

        internal override void AddToProject(Action onInstall = null)
        {
            _onInstall = onInstall;
            Install();
        }

        [ContextMenu("Install")]
        private void Install()
        {
            if (_isInstalled)
            {
                throw new InvalidOperationException($"{BlockName}'s SDK is already installed");
            }

#if UNITY_2021_1_OR_NEWER
            _www = UnityWebRequest.Get(ZipFileUrl);
            _www.SendWebRequest();
            EditorApplication.update += MonitorDownload;

            OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.InstallSDK)
                .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.BlockId, Id)
                .Send();
#else
        throw new InvalidOperationException("Remote blocks installation is only available from Unity 2021");
#endif
        }

        [ContextMenu("Remove")]
        internal void Remove()
        {
            if (!_isInstalled)
            {
                throw new InvalidOperationException($"{BlockName}'s SDK is not installed");
            }

            FileUtil.DeleteFileOrDirectory(InstallPath);
            FileUtil.DeleteFileOrDirectory($"{InstallPath}.meta");

            AssetDatabase.Refresh();

            OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.RemoveSDK)
                .AddAnnotation(OVRTelemetryConstants.BB.AnnotationType.BlockId, Id)
                .Send();

            UpdateInstalledState();
        }

#if UNITY_2021_1_OR_NEWER
        private void MonitorDownload()
        {
            if (_www == null || !_www.isDone)
            {
                return;
            }

            if (_www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(_www.error);
            }
            else
            {
                var zipFilePath = Path.Combine(Application.temporaryCachePath, "downloaded.zip");

                File.WriteAllBytes(zipFilePath, _www.downloadHandler.data);

                ZipFile.ExtractToDirectory(zipFilePath, InstallPath);

                File.Delete(zipFilePath);

                AssetDatabase.Refresh();
            }

            _www = null;
            EditorApplication.update -= MonitorDownload;

            UpdateInstalledState();
            _onInstall?.Invoke();
            _onInstall = null;
        }
#endif

    }
}
