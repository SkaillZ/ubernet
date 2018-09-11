using Skaillz.Ubernet.NetworkEntities.Unity;
using UnityEditor;
using UnityEngine;

namespace Skaillz.Ubernet.Editor.CustomEditors
{
    [CustomEditor(typeof(MonoNetworkComponent), true)]
    public class MonoNetworkComponentEditor : UnityEditor.Editor
    {
        private SerializedProperty _idProp;
        
        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            if (MonoNetworkComponent.ShowDebugInfo)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextField("Debug Info", EditorStyles.centeredGreyMiniLabel);
                if (GUILayout.Button("Hide", EditorStyles.miniButton))
                {
                    ((MonoNetworkComponent)target).ToggleDebugInfo();
                }
                EditorGUILayout.EndHorizontal();
                
                GUI.enabled = false;
                EditorGUILayout.PropertyField(_idProp, new GUIContent("Network Component ID"));
                
                EditorGUILayout.Toggle("Destroy automatically", ((MonoNetworkComponent)target).DestroyAutomatically);
                GUI.enabled = true;
            }
        }
    }
}