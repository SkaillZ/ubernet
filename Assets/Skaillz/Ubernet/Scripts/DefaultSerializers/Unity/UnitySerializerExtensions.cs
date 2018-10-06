using JetBrains.Annotations;
using Skaillz.Ubernet.NetworkEntities;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public static class UnitySerializerExtensions
    {
        public static ISerializer RegisterUnityDefaultTypes([NotNull] this ISerializer serializer)
        {
            serializer.RegisterCustomType(typeof(Vector2), UnityDefaultTypes.Vector2, new Vector2Serializer());
            serializer.RegisterCustomType(typeof(Vector3), UnityDefaultTypes.Vector3, new Vector3Serializer());
            serializer.RegisterCustomType(typeof(Quaternion), UnityDefaultTypes.Quaternion, new QuaternionSerializer());
            serializer.RegisterCustomType(typeof(Color), UnityDefaultTypes.Color, new ColorSerializer());

            return serializer;
        }

        public static IConnection RegisterUnityDefaultTypes([NotNull] this IConnection connection)
        {
            RegisterUnityDefaultTypes(connection.Serializer);

            return connection;
        }
        
        public static NetworkEntityManager RegisterUnityDefaultTypes([NotNull] this NetworkEntityManager entityManager)
        {
            RegisterUnityDefaultTypes(entityManager.Connection.Serializer);

            return entityManager;
        }
    }
}