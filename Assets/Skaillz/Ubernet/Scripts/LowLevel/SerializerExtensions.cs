using Skaillz.Ubernet.DefaultSerializers;

namespace Skaillz.Ubernet
{
    public static class SerializerExtensions
    {
        public static void RegisterProtobufType<T>(this ISerializer _serializer)
        {
            _serializer.RegisterCustomType(typeof(T), new ProtobufSerializer<T>());
        }
        
        public static void RegisterProtobufType<T>(this ISerializer _serializer, byte code)
        {
            _serializer.RegisterCustomType(typeof(T), code, new ProtobufSerializer<T>());
        }
    }
}