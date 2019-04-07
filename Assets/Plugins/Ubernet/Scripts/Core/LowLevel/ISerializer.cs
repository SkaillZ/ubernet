using System;
using System.IO;

namespace Skaillz.Ubernet
{
    public interface ISerializer
    {
        void Serialize(object value, Stream stream);
        object Deserialize(Stream stream);
        byte[] Serialize(NetworkEvent evt);
        NetworkEvent Deserialize(byte[] bytes);
        
        void RegisterCustomType<T>(CustomTypeSerializer<T> serializer);
        
        /// <summary>
        /// Registers a <see cref="CustomTypeSerializer{T}"/> for the given type.
        /// </summary>
        /// The code should be 50 or greater for custom types.
        /// 
        /// <param name="type"></param>
        /// <param name="code">The type code, which should be 50 or greater for custom types</param>
        /// <param name="serializer">The custom type serializer to register</param>
        void RegisterCustomType<T>(byte code, CustomTypeSerializer<T> serializer);
        bool IsTypeRegistered(Type type);
    }
}