using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Skaillz.Ubernet.DefaultSerializers
{
    public class BinaryFormatterSerializer : ICustomTypeSerializer
    {
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();
        
        public void Serialize(object value, Stream stream)
        {
            _binaryFormatter.Serialize(stream, value);
        }

        public object Deserialize(Stream stream)
        {
            return _binaryFormatter.Deserialize(stream);
        }
    }
}