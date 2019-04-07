using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Skaillz.Ubernet.DefaultSerializers
{
    public class BinaryFormatterSerializer<T> : CustomTypeSerializer<T> where T : class
    {
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();
        
        public override void Serialize(T value, Stream stream)
        {
            _binaryFormatter.Serialize(stream, value);
        }

        public override T Deserialize(Stream stream)
        {
            return (T) _binaryFormatter.Deserialize(stream);
        }
    }
}