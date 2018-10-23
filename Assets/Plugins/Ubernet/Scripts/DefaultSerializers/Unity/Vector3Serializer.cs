using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class Vector3Serializer : ICustomTypeSerializer
    {
        private readonly SerializationHelper _helper = new SerializationHelper();
        
        public void Serialize(object value, Stream stream)
        {
            var vector = (Vector3) value;
            _helper.SerializeFloat(vector.x, stream);
            _helper.SerializeFloat(vector.y, stream);
            _helper.SerializeFloat(vector.z, stream);
        }

        public object Deserialize(Stream stream)
        {
            float x = _helper.DeserializeFloat(stream);
            float y = _helper.DeserializeFloat(stream);
            float z = _helper.DeserializeFloat(stream);
            return new Vector3(x, y, z);
        }
    }
}