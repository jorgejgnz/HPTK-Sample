/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.Voice.Hub.Attributes;
using Meta.Voice.Hub.Interfaces;
using Meta.Voice.Hub.UIComponents;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Meta.Voice.Hub
{
    [MetaHubPageScriptableObject]
    public class ImagePage : ScriptableObject, IPageInfo
    {
        [SerializeField] private string _displayName;
        [SerializeField] private string _prefix;
        [SerializeField] private MetaHubContext _context;
        [SerializeField] private int _priority = 0;
        [SerializeField] [FormerlySerializedAs("image")]
        private Texture2D _image;

        public string Name => _displayName ?? name;
        public string Context => _context?.Name;
        public int Priority => _priority;
        public string Prefix => _prefix;
        internal Texture2D Image => _image;
    }

    [CustomEditor(typeof(ImagePage))]
    public class ImageDisplayScriptableObjectEditor : Editor
    {
        private ImagePage _imageDisplay;
        private ImageView _imageView;

        private void OnEnable()
        {
            _imageDisplay = (ImagePage)target;
            _imageView = new ImageView(this);
        }

        public override void OnInspectorGUI()
        {
            if (_imageDisplay.Image)
            {
                _imageView.Draw(_imageDisplay.Image);
            }
            else
            {
                // Draw the default properties
                base.OnInspectorGUI();
            }
        }
    }
}
