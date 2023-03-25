/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi;
using Meta.WitAi.Json;
using UnityEngine;

namespace Meta.Conduit
{
    /// <summary>
    /// Loads the manifest and resolves its actions so they can be used during dispatching.
    /// </summary>
    class ManifestLoader : IManifestLoader
    {
        /// <inheritdoc/>
        public Manifest LoadManifest(string manifestLocalPath)
        {
            int extIndex = manifestLocalPath.LastIndexOf('.');
            string ignoreEnd = extIndex == -1 ? manifestLocalPath : manifestLocalPath.Substring(0, extIndex);
            TextAsset jsonFile = Resources.Load<TextAsset>(ignoreEnd);
            if (jsonFile == null)
            {
                VLog.E($"Conduit Error - No Manifest found at Resources/{manifestLocalPath}");
                return null;
            }

            string rawJson = jsonFile.text;
            return LoadManifestFromString(rawJson);
        }

        /// <inheritdoc/>
        public Manifest LoadManifestFromString(string manifestText)
        {
            var manifest = JsonConvert.DeserializeObject<Manifest>(manifestText);
            if (manifest.ResolveActions())
            {
                VLog.D($"Successfully Loaded Conduit manifest");
            }
            else
            {
                VLog.E($"Fail to resolve actions from Conduit manifest");
            }

            return manifest;
        }
    }
}
