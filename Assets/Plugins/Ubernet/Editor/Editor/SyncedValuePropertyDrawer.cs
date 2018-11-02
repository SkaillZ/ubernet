using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;
using UnityEditor;
using UnityEngine;

namespace Skaillz.Ubernet.Editor.CustomEditors
{
    [CustomPropertyDrawer(typeof(SyncedBool))]
    [CustomPropertyDrawer(typeof(SyncedInt))]
    [CustomPropertyDrawer(typeof(SyncedByte))]
    [CustomPropertyDrawer(typeof(SyncedShort))]
    [CustomPropertyDrawer(typeof(SyncedFloat))]
    [CustomPropertyDrawer(typeof(SyncedString))]
    [CustomPropertyDrawer(typeof(SyncedVector2))]
    [CustomPropertyDrawer(typeof(SyncedVector3))]
    [CustomPropertyDrawer(typeof(SyncedQuaternion))]
    [CustomPropertyDrawer(typeof(SyncedColor))]
    public class SyncedValuePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property.FindPropertyRelative("_value"), label);
        }
    }
}
