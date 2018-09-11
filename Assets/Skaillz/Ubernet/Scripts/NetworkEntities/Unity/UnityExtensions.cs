using System;
using UnityEngine;

namespace Skaillz.Ubernet.NetworkEntities.Unity
{
    public static class UnityExtensions
    {
        public static GameObjectNetworkEntityBase InstantiateFromPrefab(this NetworkEntityManager manager, GameObject prefab)
        {
            var go = UnityEngine.Object.Instantiate(prefab);
            var entity = go.GetComponent<GameObjectNetworkEntityBase>();
            if (entity == null)
            {
                throw new InvalidOperationException($"Prefabs instantiated on a {nameof(NetworkEntityManager)} " +
                                                    $"must have a {nameof(GameObjectNetworkEntityBase)} component attached to them.");
            }

            manager.InstantiateEntity(entity);
            return entity;
        }
    }
}