/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using UnityEngine;

namespace Meta.Voice.Hub.Content
{
    [MetaHubPageScriptableObject]
    public class SamplesPage : ScriptableObject, IPageInfo
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _prefix;
        [SerializeField] private MetaHubContext _context;
        [SerializeField] private int _priority = 0;
        [SerializeField] private string _sampleSetId;

        public string SampleSetId => _sampleSetId;
        public string Name => _displayName ?? name;
        public string Context => _context.Name;
        public int Priority => _priority;
        public string Prefix => _prefix;
    }
}
