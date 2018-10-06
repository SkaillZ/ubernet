using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    [CreateAssetMenu(menuName = "Ubernet Prefab Cache", fileName = "UbernetPrefabCache", order = 100000)]
    public class PrefabCache : ScriptableObject
    {
        public const string CacheFileName = "UbernetPrefabCache";
        private static PrefabCache _defaultCache;
        
        public static PrefabCache GetPrefabCache()
        {
            if (_defaultCache == null)
            {
                LoadPrefabCache();
            }

            return _defaultCache;
        }

        private static void LoadPrefabCache()
        {
            _defaultCache = Resources.Load<PrefabCache>(CacheFileName);
            if (_defaultCache == null)
            {
                throw new UbernetException($"Prefab cache could not be loaded. Please create one in 'Resources/{CacheFileName}'");
            }
        }
        
        [SerializeField] private GameObject[] _prefabs;

        public int GetPrefabIndex(GameObject prefab)
        {
            for (int i = 0; i < _prefabs.Length; i++)
            {
                if (_prefabs[i] == prefab)
                {
                    return i;
                }
            }

            return -1;
        }
        
        public GameObject GetPrefab(int index)
        {
            if (index < 0 || index > _prefabs.Length - 1)
            {
                return null;
            }
            return _prefabs[index];
        }
    }
}