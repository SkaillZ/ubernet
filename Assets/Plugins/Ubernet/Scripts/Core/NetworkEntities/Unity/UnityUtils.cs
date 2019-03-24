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
    }
}
