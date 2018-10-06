using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;
using Skaillz.Ubernet.Providers.Photon;
using UnityEditor;
using UnityEngine;

namespace Skaillz.Ubernet.Editor.CustomEditors
{
    [CustomEditor(typeof(MatchmakerUI))]
    public class MatchmakerUIEditor : UnityEditor.Editor
    {
        private SerializedProperty _lobbySceneProp;
        private SerializedProperty _gameSceneProp;
        private SerializedProperty _persistBetweenScenesProp;
        
        private SerializedProperty _automaticallyCreateEntityManagerProp;        
        private SerializedProperty _playerTypeProp;

        private SerializedProperty _photonProtocolProp;
        private SerializedProperty _photonRegionProp;
        private SerializedProperty _photonAppIdProp;
        private SerializedProperty _photonAppVersionProp;
        private SerializedProperty _photonTickRateProp;

        private readonly GUIContent _lobbySceneContent = new GUIContent("Lobby Scene", "This scene is loaded after " +
            "disconnecting from a game.");
        private readonly GUIContent _gameSceneContent = new GUIContent("Game Scene", "The scene where gameplay takes place in. " +
            "It can be entered from the lobby.");
        private readonly GUIContent _persistBetweenScenesContent = new GUIContent("Persist Between Scenes",
            "If enabled, 'DontDestroyOnLoad' will be set on the GameObject, allowing the GUI to be shown on the game scene");
        
        private readonly GUIContent _automaticallyCreateEntityManagerContent = new GUIContent("Auto Create Entity Manager",
            $"If enabled, a {nameof(NetworkEntityManager)} will automatically created after connecting to a game." +
            "If this option is turned off, you will still be able to create an entity manager through the UI after joining.");
        private readonly GUIContent _playerTypeContent = new GUIContent("Player Type",
            $"{nameof(NetworkEntityManager)} requires you to provide player objects for each player where each player's " +
            "properties are saved. If you choose a type and a default constructor is present, it can be created automatically. " +
            $"For development purposes, you can use the '{nameof(DefaultPlayer)}' type, which only provides a name for the player. " +
            $"All custom types that implement {nameof(IPlayer)} are also shown in the list.");

        private readonly GUIContent _photonProtocolContent = new GUIContent("Protocol", "The transport protocol used " +
            "by Photon. The Websocket options are only available if building for WebGL.");
        private readonly GUIContent _photonAppIdContent = new GUIContent("App ID");
        private readonly GUIContent _photonAppVersionContent = new GUIContent("App Version", "The version of your client. " +
           "A new version also creates a new \"virtual app\" to separate players from older client versions.");
        private readonly GUIContent _photonRegionContent = new GUIContent("Region", "The region to connect to. That region's " +
            "Master Server is used to connect to Photon.");

        private readonly GUIContent _photonTickRateContent = new GUIContent("Tick Rate",
            "The number of times per second any network events should be sent and handled.");
        
        private readonly GUIContent _entitiesHeaderContent = new GUIContent("Network Entities");
        private readonly GUIContent _providerSettingsContent = new GUIContent("Provider Settings");
        private readonly GUIContent _photonSettingsContent = new GUIContent("Photon", "Photon-specific settings. " +
            "You can ignore these settings if you are not connecting to Photon.");

        private string[] _playerTypeDisplayNames;
        private string[] _playerTypeFullNames;
        private bool _photonFoldout;

        private void OnEnable()
        {
            _lobbySceneProp = serializedObject.FindProperty("_lobbyScene");
            _gameSceneProp = serializedObject.FindProperty("_gameScene");
            _persistBetweenScenesProp = serializedObject.FindProperty("_persistBetweenScenes");

            _automaticallyCreateEntityManagerProp = serializedObject.FindProperty("_automaticallyCreateEntityManager");
            _playerTypeProp = serializedObject.FindProperty("_playerType");

            _photonProtocolProp = serializedObject.FindProperty("_photonProtocol");
            _photonRegionProp = serializedObject.FindProperty("_photonRegion");
            _photonAppIdProp = serializedObject.FindProperty("_photonAppId");
            _photonAppVersionProp = serializedObject.FindProperty("_photonAppVersion");
            _photonTickRateProp = serializedObject.FindProperty("_photonTickRate");

            var types = GetPlayerTypes();
            _playerTypeDisplayNames = types.Select(t => t == typeof(DefaultPlayer) ? t.Name : t.FullName).ToArray();
            _playerTypeFullNames = types.Select(t => t.AssemblyQualifiedName).ToArray();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            DrawScenePicker(_lobbySceneProp, _lobbySceneContent);
            DrawScenePicker(_gameSceneProp, _gameSceneContent);
            
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_persistBetweenScenesProp, _persistBetweenScenesContent);
            
            DrawBoldHeader(_entitiesHeaderContent);
            EditorGUILayout.PropertyField(_automaticallyCreateEntityManagerProp, _automaticallyCreateEntityManagerContent);
            DrawPlayerTypeSelection();

            DrawBoldHeader(_providerSettingsContent);
            _photonFoldout = EditorGUILayout.Foldout(_photonFoldout, _photonSettingsContent);
            if (_photonFoldout)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.PropertyField(_photonProtocolProp, _photonProtocolContent);
                DrawRegionSelection();
                EditorGUILayout.PropertyField(_photonAppIdProp, _photonAppIdContent);
                EditorGUILayout.PropertyField(_photonAppVersionProp, _photonAppVersionContent);
                EditorGUILayout.PropertyField(_photonTickRateProp, _photonTickRateContent);
                
                EditorGUI.indentLevel--;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawBoldHeader(GUIContent content)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        private void DrawScenePicker(SerializedProperty property, GUIContent label)
        {
            var sceneObject = GetSceneObject(property.stringValue);
            var scene = EditorGUILayout.ObjectField(label, sceneObject, typeof(SceneAsset), false);
            if (scene != null)
            {
                var newSceneObject = GetSceneObject(scene.name);
            
                if (newSceneObject == null)
                {
                    EditorUtility.DisplayDialog("Unloadable scene", "The selected scene was not added to the build settings. " +
                                                                    "Please add it to the build settings and try again.", "Ok");
                    property.stringValue = "";
                } 
                else
                {
                    property.stringValue = scene.name;
                }
            }
        }

        private void DrawPlayerTypeSelection()
        {
            var typeStr = _playerTypeProp.stringValue;
            if (typeStr == typeof(DefaultPlayer).AssemblyQualifiedName)
            {
                EditorGUILayout.HelpBox($"Derive from {nameof(PlayerBase)} to create your own player type. " +
                                         "It will be included in the list automatically.", MessageType.Info);
            }

            if (!string.IsNullOrEmpty(typeStr))
            {
                var currentType = Type.GetType(typeStr);
                if (currentType != null && currentType != typeof(DefaultPlayer))
                {
                    if (currentType.GetConstructor(Type.EmptyTypes) == null)
                    {
                        EditorGUILayout.HelpBox("No constructor without arguments is available for the selected class. " +
                                                "Please provide an empty constructor and try again.", MessageType.Error);
                    }
                }
            }

            int lastSelection = Array.IndexOf(_playerTypeFullNames, typeStr);
            if (lastSelection == -1)
            {
                lastSelection = 0;
                _playerTypeProp.stringValue = typeof(DefaultPlayer).AssemblyQualifiedName;
            }
            
            int index = EditorGUILayout.Popup(_playerTypeContent, lastSelection, _playerTypeDisplayNames);
            _playerTypeProp.stringValue = _playerTypeFullNames[index];
        }

        private void DrawRegionSelection()
        {
            var regionIndex = Array.IndexOf(PhotonRegions.AllRegions, _photonRegionProp.stringValue);
            if (regionIndex == -1)
            {
                regionIndex = 0;
                _photonRegionProp.stringValue = PhotonRegions.AllRegions[0];
            }
            var newIndex = EditorGUILayout.Popup(_photonRegionContent, regionIndex, PhotonRegions.DisplayNames);
            _photonRegionProp.stringValue = PhotonRegions.AllRegions[newIndex];
        }
        
        private static List<Assembly> GetListOfEntryAssemblyWithReferences()
        {            
            var assemblies = new List<Assembly>();
            var mainAssembly = Assembly.Load("Assembly-CSharp");
            assemblies.Add(mainAssembly);

            foreach (var referenceName in mainAssembly.GetReferencedAssemblies())
            {
                assemblies.Add(Assembly.Load(referenceName));
            }
            return assemblies;
        }

        private static Type[] GetPlayerTypes()
        {
            var assemblies = GetListOfEntryAssemblyWithReferences();
            return assemblies
                .SelectMany(a => a.ExportedTypes)
                .Where(t => typeof(IPlayer).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface)
                .ToArray();
        }
        
        private static SceneAsset GetSceneObject(string sceneObjectName)
        {
            if (string.IsNullOrEmpty(sceneObjectName))
            {
                return null;
            }

            foreach (var editorScene in EditorBuildSettings.scenes)
            {
                if (editorScene.path.EndsWith(sceneObjectName + ".unity"))
                {
                    return AssetDatabase.LoadAssetAtPath<SceneAsset>(editorScene.path);
                }
            }
            return null;
        }
    }
}