using System;
using System.IO;
using Skaillz.Ubernet.DefaultSerializers.Unity;
using Skaillz.Ubernet.NetworkEntities;
using Skaillz.Ubernet.NetworkEntities.Unity;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    public static class UnityUtils
    {
        internal static readonly Vector3Serializer Vector3Serializer = new Vector3Serializer();
        internal static readonly QuaternionSerializer QuaternionSerializer = new QuaternionSerializer();
        
        /// <summary>
        /// The singleton <see cref="NetworkEntityManager"/> used by <see cref="GameObjectNetworkEntity"/> by default.
        /// </summary>
        public static NetworkEntityManager EntityManager { get; set; }

        public static NetworkEntityManager SetAsDefaultEntityManager(this NetworkEntityManager entityManager)
        {
            EntityManager = entityManager;
            return entityManager;
        }

        public static GameObjectNetworkEntityBase InstantiateFromResourcePrefab(this NetworkEntityManager manager,
            string path, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            GameObjectNetworkEntity.AutoRegister = false;
            
            try
            {
                var prefab = Resources.Load(path) as GameObject;
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Prefab could not be loaded from path: '{path}'");
                }
                
                if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
                {
                    throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                        $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
                }

                var go = Object.Instantiate(prefab, position, rotation);
                var entity = go.GetComponent<GameObjectNetworkEntityBase>();

                entity.Id = manager.CreateEntityId();
                entity.OwnerId = manager.LocalPlayer.ClientId;

                using (var stream = new MemoryStream())
                {
                    var helper = new SerializationHelper();
                    helper.SerializeInt(entity.Id, stream);
                    helper.SerializeInt(entity.OwnerId, stream);
                    helper.SerializeString(path, stream);
                    
                    Vector3Serializer.Serialize(position, stream);
                    QuaternionSerializer.Serialize(rotation, stream);
                    
                    manager.SendEvent(DefaultEvents.NetworkEntityCreateFromResource, stream.ToArray());
                }

                manager.RegisterEntity(entity, true);
                return entity;
            }
            finally
            {
                GameObjectNetworkEntity.AutoRegister = true;
            }
        }
        
        public static GameObjectNetworkEntityBase InstantiateFromPrefab(this NetworkEntityManager manager,
            GameObject prefab, Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion))
        {
            GameObjectNetworkEntity.AutoRegister = false;
            
            try
            {
                var prefabCache = PrefabCache.GetPrefabCache();
                int cacheIndex = prefabCache.GetPrefabIndex(prefab);
                if (cacheIndex == -1)
                {
                    throw new InvalidOperationException("The given prefab is not in the prefab cache. Please add it and try again.");
                }
                
                if (prefab.GetComponent<GameObjectNetworkEntityBase>() == null)
                {
                    throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                        $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
                }

                var go = Object.Instantiate(prefab, position, rotation);
                var entity = go.GetComponent<GameObjectNetworkEntityBase>();

                entity.Id = manager.CreateEntityId();
                entity.OwnerId = manager.LocalPlayer.ClientId;

                using (var stream = new MemoryStream())
                {
                    var helper = new SerializationHelper();
                    helper.SerializeInt(entity.Id, stream);
                    helper.SerializeInt(entity.OwnerId, stream);
                    helper.SerializeInt(cacheIndex, stream);
                    
                    Vector3Serializer.Serialize(position, stream);
                    QuaternionSerializer.Serialize(rotation, stream);
                    
                    manager.SendEvent(DefaultEvents.NetworkEntityCreateFromPrefabCache, stream.ToArray());
                }

                manager.RegisterEntity(entity, true);
                return entity;
            }
            finally
            {
                GameObjectNetworkEntity.AutoRegister = true;
            }
        }
    }
}
