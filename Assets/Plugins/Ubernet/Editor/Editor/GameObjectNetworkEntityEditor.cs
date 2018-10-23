using System.Linq;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;
using UnityEditor;
using UnityEngine;

namespace Skaillz.Ubernet.Editor.CustomEditors
{
    [CustomEditor(typeof(GameObjectNetworkEntityBase), true)]
    public class GameObjectNetworkEntityEditor : UnityEditor.Editor
    {
        private bool _showComponents = true;
        
        private SerializedProperty _idProp;
        private SerializedProperty _reliableProp;

        private SerializedObject _cacheObj;
        private SerializedProperty _cacheArray;
        
        private static Texture _warningIcon;

        private static readonly string IdTooltip = $"This ID should be unique per scene. It is automatically set when a" +
            $" {nameof(GameObjectNetworkEntityBase)} is created or reset.";

        private readonly GUIContent _idContent = new GUIContent("Network Entity ID", IdTooltip);
        
        private GUIContent _duplicateIdContent;
        
        private readonly GUIContent _reliableContent = new GUIContent("Reliability",
            $"Specifies if the entity's serialization events should be sent reliably or unreliably. " +
            $"This setting does not affect RPCs, which are sent reliably by default.");
        
        private readonly GUIContent _ownerIdContent = new GUIContent("Owner");
        private readonly GUIContent _componentsContent = new GUIContent("Controlled components:");
        private readonly GUIContent _noComponentsContent = new GUIContent("Controlled components: none");

        private readonly GUIContent _setAtRuntimeContent = new GUIContent("Set at runtime");

        private void OnEnable()
        {
            _idProp = serializedObject.FindProperty("_id");
            _reliableProp = serializedObject.FindProperty("_reliable");

            var cache = Resources.Load<PrefabCache>(PrefabCache.CacheFileName);
            if (cache != null)
            {
                _cacheObj = new SerializedObject(cache);
                _cacheArray = _cacheObj.FindProperty("_prefabs");
            }
            
            _warningIcon = EditorGUIUtility.Load("console.erroricon") as Texture;
            _duplicateIdContent = new GUIContent("Network Entity ID (duplicate)", _warningIcon, IdTooltip);
        }

        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                GUILayout.Label("(target destroyed)");
                return;
            }
           
            var entity = (GameObjectNetworkEntityBase) target;

            if (IsPrefab(entity.gameObject) && _cacheObj != null)
            {
                DrawPrefabCacheInfo(entity);
                EditorGUILayout.LabelField(_idContent, _setAtRuntimeContent);
            }
            else
            {
                DrawIdInfo(entity);
            }

            DrawOwnerInfo(entity);
            
            DrawReliabilitySelection();
            
            if (Application.isPlaying && !IsPrefab(entity.gameObject))
            {
                DrawComponentList(entity);
            }
        }

        private void DrawPrefabCacheInfo(GameObjectNetworkEntityBase entity)
        {
            if (!IsInCache(entity.gameObject))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox(
                    "This entity is not registered in the prefab cache, so it cannot be instantiated over the network.",
                    MessageType.Warning);

                EditorGUILayout.BeginVertical();
                GUILayout.Space(16);
                if (GUILayout.Button("Add to Cache", EditorStyles.miniButton))
                {
                    // Try to find an empty space
                    bool insertedInFreeSpace = false;
                    
                    for (int i = 0; i < _cacheArray.arraySize && !insertedInFreeSpace; i++)
                    {
                        var elem = _cacheArray.GetArrayElementAtIndex(i);
                        if (elem.objectReferenceInstanceIDValue == 0)
                        {
                            elem.objectReferenceInstanceIDValue = entity.gameObject.GetInstanceID();
                            insertedInFreeSpace = true;
                        }
                    }

                    if (!insertedInFreeSpace)
                    {
                        // If no empty space was found, insert at the end
                        _cacheArray.arraySize++;
                        var lastElem = _cacheArray.GetArrayElementAtIndex(_cacheArray.arraySize - 1);
                        lastElem.objectReferenceInstanceIDValue = entity.gameObject.GetInstanceID();
                    }

                    _cacheObj.ApplyModifiedProperties();
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("(Registered in Prefab Cache)", EditorStyles.miniBoldLabel);
                if (GUILayout.Button("View Cache", EditorStyles.miniButton, GUILayout.Width(80f)))
                {
                    Selection.activeObject = _cacheObj.targetObject;
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(2);
            }
        }

        private void DrawIdInfo(GameObjectNetworkEntityBase entity)
        {
            var duplicateIdEntities = Resources.FindObjectsOfTypeAll<GameObjectNetworkEntityBase>()
                .Where(e => e != entity && !IsPrefab(e.gameObject))
                .ToArray();

            bool duplicate = duplicateIdEntities.Any(e => e.Id == entity.Id);
            
            EditorGUI.BeginChangeCheck();

            if (duplicate)
            {
                EditorGUILayout.BeginHorizontal();
                
                EditorGUILayout.HelpBox("Duplicate Entity ID", MessageType.Warning);

                EditorGUILayout.BeginVertical(GUILayout.Width(100f));
                GUILayout.Space(6f);
                
                if (GUILayout.Button("Fix"))
                {
                    GUI.FocusControl(null);
                    entity.Reset();
                }
                if (GUILayout.Button("Select other entities", EditorStyles.miniButton))
                {
                    // ReSharper disable once CoVariantArrayConversion
                    Selection.objects = duplicateIdEntities.Select(e => e.gameObject).ToArray();
                }

                EditorGUILayout.EndVertical();
                
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                
                EditorGUILayout.PropertyField(_idProp, _duplicateIdContent, EditorStyles.boldFont);
            }
            else
            {
                EditorGUILayout.PropertyField(_idProp, _idContent);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawComponentList(GameObjectNetworkEntityBase entity)
        {
            if (entity.Components != null && entity.Components.Count > 0)
            {
                _showComponents = EditorGUILayout.Foldout(_showComponents, _componentsContent);

                if (_showComponents)
                {
                    EditorGUI.indentLevel++;
                    foreach (var component in entity.Components)
                    {
                        if (component is MonoNetworkComponent)
                        {
                            GUI.enabled = false;
                            EditorGUILayout.ObjectField((MonoNetworkComponent) component, typeof(MonoNetworkComponent), true);
                            GUI.enabled = true;
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"{component.GetType().FullName}: {component}");
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
            else
            {
                EditorGUILayout.LabelField(_noComponentsContent);
            }
        }

        private void DrawOwnerInfo(GameObjectNetworkEntityBase entity)
        {
            if (!IsPrefab(entity.gameObject))
            {
                if (Application.isPlaying)
                {
                    if (entity.OwnerId == -1)
                    {
                        var serverPlayer = entity.Manager?.GetServerPlayer();
                        EditorGUILayout.LabelField(_ownerIdContent, new GUIContent($"Scene - controlled by server: " +
                                                                                   $"{FormatPlayer(serverPlayer)}"));
                    }
                    else
                    {
                        var player = entity.Manager?.GetPlayer(entity.OwnerId);
                        EditorGUILayout.LabelField(_ownerIdContent, new GUIContent(FormatPlayer(player)));
                    }
                }
                else
                {
                    EditorGUILayout.LabelField(_ownerIdContent,
                        new GUIContent("Scene (controlled by server at runtime)"));
                }
            }
            else
            {
                EditorGUILayout.LabelField(_ownerIdContent, _setAtRuntimeContent);
            }
        }

        private void DrawReliabilitySelection()
        {
            EditorGUI.BeginChangeCheck();
            int reliableValue = EditorGUILayout.Popup(_reliableContent, _reliableProp.boolValue ? 1 : 0, new[]
            {
                "Unreliable",
                "Reliable"
            });
            _reliableProp.boolValue = reliableValue == 1;

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private bool IsInCache(GameObject go)
        {
            for (int i = 0; i < _cacheArray.arraySize; i++)
            {
                if (_cacheArray.GetArrayElementAtIndex(i).objectReferenceInstanceIDValue == go.GetInstanceID())
                {
                    return true;
                }
            }

            return false;
        }
        
        private static string FormatPlayer(IPlayer player)
        {
            if (player == null)
            {
                return "<null player>";
            }

            if (player.IsLocalPlayer())
            {
                return "Local client";
            }
            
            return $"{player} (Client #{player.ClientId})";
        }

        private static bool IsPrefab(GameObject go)
        {
            return go.scene.name == null;
        }
    }
}