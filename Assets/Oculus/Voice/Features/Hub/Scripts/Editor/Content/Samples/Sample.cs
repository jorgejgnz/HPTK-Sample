/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using UnityEditor;
using UnityEngine;

namespace Meta.Voice.Hub.Content
{
    public class Sample : ScriptableObject
    {
        [Header("Content")]
        public string title;
        [TextArea]
        public string description;
        public Texture2D tileImage;
        public Texture2D screenshot;

        [Header("Resource Paths")]
        public SceneAsset sceneReference;
        public string packageSampleName;
        public string sampleSetId;
        public float priority;
    }
}
