using System;
using UnityEditor;
using UnityEngine;

namespace Meta.WitAi.Attributes
{
    /// <summary>
    /// Hides a property when a value is defined. This is meant for internal fields on prebuilt prefabs to reduce UI
    /// clutter. If you need to change the value of one of these fields you can access them through the inspector
    /// debug view.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class HideWhenSetAttribute : PropertyAttribute
    {
    }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(HideWhenSetAttribute))]
    public class HideWhenSetDrawer : PropertyDrawer
    {
        private bool IsVisible(SerializedProperty property) => property.objectReferenceValue == null;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (IsVisible(property))
            {
                EditorGUI.PropertyField(position, property, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (IsVisible(property)) return EditorGUI.GetPropertyHeight(property, label);

            return 0;
        }
    }
#endif
}
