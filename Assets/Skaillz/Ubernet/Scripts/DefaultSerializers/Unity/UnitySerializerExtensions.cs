using JetBrains.Annotations;
using Skaillz.Ubernet.NetworkEntities;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public static class UnitySerializerExtensions
    {
        public static ISerializer WithUnityDefaultTypes([NotNull] this ISerializer serializer)
        {
            serializer.RegisterCustomType(typeof(Vector2), UnityDefaultTypes.Vector2, new Vector2Serializer());
            serializer.RegisterCustomType(typeof(Vector3), UnityDefaultTypes.Vector3, new Vector3Serializer());
            serializer.RegisterCustomType(typeof(Quaternion), UnityDefaultTypes.Quaternion, new QuaternionSerializer());

            return serializer;
        }

        public static IConnection WithUnityDefaultTypes([NotNull] this IConnection connection)
        {
            WithUnityDefaultTypes(connection.Serializer);

            return connection;
        }
        
        public static NetworkEntityManager WithUnityDefaultTypes([NotNull] this NetworkEntityManager entityManager)
        {
            WithUnityDefaultTypes(entityManager.Connection.Serializer);

            return entityManager;
        }
    }
}