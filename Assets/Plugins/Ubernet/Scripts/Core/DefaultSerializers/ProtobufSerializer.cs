using System.IO;
using ProtoBuf;

namespace Skaillz.Ubernet.DefaultSerializers
{
    public class ProtobufSerializer<T> : ICustomTypeSerializer
    {
        public void Serialize(object value, Stream stream)
        {
            ProtoBuf.Serializer.SerializeWithLengthPrefix(stream, (T) value, PrefixStyle.Base128);
        }

        public object Deserialize(Stream stream)
        {
            return ProtoBuf.Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128);
        }
    }
}