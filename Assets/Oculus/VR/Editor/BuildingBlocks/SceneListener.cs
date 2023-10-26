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

using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    [InitializeOnLoad]
    public static class SceneListener
    {
        private const string InitializedKey = "OVRBBSceneListener_Initialized";

        static SceneListener()
        {
            if (!SessionState.GetBool(InitializedKey, false))
            {
                SessionState.SetBool(InitializedKey, true);

                if (SceneManager.GetActiveScene().isLoaded)
                {
                    RecordSceneBlocks();
                }
            }

            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
        {
            RecordSceneBlocks();
        }

        private static void RecordSceneBlocks()
        {
            EditorApplication.delayCall += () =>
            {
                var sceneBlocks = Object.FindObjectsOfType<BuildingBlock>();

                foreach (var block in sceneBlocks)
                {
                    OVRTelemetry.Start(OVRTelemetryConstants.BB.MarkerId.OpenSceneWithBlock)
                        .AddBlockInfo(block)
                        .Send();
                }
            };
        }
    }
}
