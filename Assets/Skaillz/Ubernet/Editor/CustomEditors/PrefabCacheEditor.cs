using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Skaillz.Ubernet.Editor.CustomEditors
{
	[CustomEditor(typeof(PrefabCache))]
	public class PrefabCacheEditor : UnityEditor.Editor
	{
		private static readonly string HelpMessage = "Prefabs must be added to the cache before they can be instantiated " +
			$"over the network with '{nameof(NetworkEntityManager)}.{nameof(UnityUtils.InstantiateFromPrefab)}'.";
		
		private ReorderableList _list;
	
		private void OnEnable() {
			_list = new ReorderableList(serializedObject, 
				serializedObject.FindProperty("_prefabs"), 
				true, true, true, true);
			
			_list.drawElementCallback =  (rect, index, isActive, isFocused) => {
				var element = _list.serializedProperty.GetArrayElementAtIndex(index);
				rect.y += 2;
				EditorGUI.LabelField(
					new Rect(rect.x, rect.y, 60, EditorGUIUtility.singleLineHeight),
					$"Index {index}");
				EditorGUI.PropertyField(
					new Rect(rect.x + 60, rect.y, rect.width - 60, EditorGUIUtility.singleLineHeight),
					element, GUIContent.none);
			};
			
			_list.drawHeaderCallback = rect => {
				EditorGUI.LabelField(rect, "Prefab Cache");
			};
		}
	
		public override void OnInspectorGUI() {
			EditorGUILayout.HelpBox(HelpMessage, MessageType.Info);
			
			serializedObject.Update();
			_list.DoLayoutList();
			serializedObject.ApplyModifiedProperties();
		}
	}
}