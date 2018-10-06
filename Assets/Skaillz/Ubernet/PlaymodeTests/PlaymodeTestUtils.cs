using System;
using Skaillz.Ubernet.Providers.Photon;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Skaillz.Ubernet.Tests.IT
{
    public static class PlaymodeTestUtils
    {
        public const string AppIdPrefKey = "Ubernet/Tests/AppId";
        public static string RoomName { get; set; } = Guid.NewGuid().ToString();

        public static string GetPhotonAppId()
        {
            #if UNITY_EDITOR
            string id = EditorPrefs.GetString(AppIdPrefKey, null);
            #else
            string id = null;
            #endif
            
            if (string.IsNullOrEmpty(id))
            {
                throw new Exception($"Invalid App ID in EditorPrefs key {AppIdPrefKey}. Set it in Ubernet Developer options first");
            }
            return id;
        }
        
        public static PhotonSettings GetPhotonSettings()
        {
            return new PhotonSettings
            {
                AppId = GetPhotonAppId(),
                AppVersion = "0.1",
                Region = "eu"
            };
        }
    }
    
#if UNITY_EDITOR
    public class SetPhotonAppIdWindow : EditorWindow
    {
        private string _appId;
            
        [MenuItem("Window/Ubernet/Developer/Set Photon App ID for Playmode Tests")]
        private static void SetPhotonAppId()
        {
            var window = CreateInstance<SetPhotonAppIdWindow>();
            window.ShowUtility();
        }
            
        private void OnEnable()
        {
            _appId = EditorPrefs.GetString(PlaymodeTestUtils.AppIdPrefKey, "");
        }

        private void OnGUI()
        {
            _appId = EditorGUILayout.TextField("App ID", _appId);

            if (GUILayout.Button("Save"))
            {
                EditorPrefs.SetString(PlaymodeTestUtils.AppIdPrefKey, _appId);
                Close();
            }
        }
    }
#endif
}