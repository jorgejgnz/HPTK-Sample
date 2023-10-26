/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * This source code is licensed under the license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Meta.WitAi.Attributes;
using Meta.WitAi.Windows;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Drawers
{
    [CustomPropertyDrawer(typeof(TooltipBoxAttribute))]
    public class TooltipBoxDrawer : DecoratorDrawer
    {
        private float _spaceAfterBox = 4;
        private float _iconSize = 32;
        private float _lastViewWidth;
        
        public override float GetHeight()
        {
            if (!WitWindow.ShowTooltips) return 0;
            
            TooltipBoxAttribute infoBoxAttribute = (TooltipBoxAttribute)attribute;
            var height = EditorStyles.helpBox.CalcHeight(new GUIContent(infoBoxAttribute.Text), _lastViewWidth - _iconSize);
            return Mathf.Max(_iconSize, height) + _spaceAfterBox;
        }
        
        public override void OnGUI(Rect position)
        {
            if (!WitWindow.ShowTooltips) return;
            _lastViewWidth = EditorGUIUtility.currentViewWidth;
            
            var iconRect = EditorGUI.IndentedRect(position);
            iconRect.width = _iconSize;
            iconRect.height = _iconSize;
            GUIContent infoIcon = EditorGUIUtility.IconContent("console.infoicon");
            infoIcon.tooltip = "You can turn off these tooltips in Voice SDK Settings.";
            EditorGUI.LabelField(iconRect, infoIcon);
            
            var tooltip = (TooltipBoxAttribute) attribute;
            var rect = EditorGUI.IndentedRect(position);
            rect.x += _iconSize;
            rect.width -= _iconSize;
            rect.height -= _spaceAfterBox;
            EditorGUI.TextArea(rect, tooltip.Text, EditorStyles.helpBox);
        }
    }
}
