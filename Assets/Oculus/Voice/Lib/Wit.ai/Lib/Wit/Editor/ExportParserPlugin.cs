/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */


using System.IO.Compression;
using Meta.WitAi.Data.Info;

namespace Meta.WitAi.Lib
{
    /// <summary>
    /// Parses the Wit.ai Export zip file
    /// </summary>
    public interface IExportParserPlugin
    {
        /// <summary>
        /// Extracts the data specific to this plugin from the zip file
        /// and adds it to the config.
        /// </summary>
        /// <param name="config">the configuration to modify</param>
        /// <param name="zipArchive">the archive containing the data to extract</param>
        void Process(IWitRequestConfiguration config, ZipArchive zipArchive);
    }
}
