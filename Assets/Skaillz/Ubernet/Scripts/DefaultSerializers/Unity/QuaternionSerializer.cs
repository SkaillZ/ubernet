using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class QuaternionSerializer : ICustomTypeSerializer
    {
        private readonly SerializationHelper _helper = new SerializationHelper();
        
        public void Serialize(object value, Stream stream)
        {
            var quaternion = (Quaternion) value;
            _helper.SerializeFloat(quaternion.x, stream);
            _helper.SerializeFloat(quaternion.y, stream);
            _helper.SerializeFloat(quaternion.z, stream);
            _helper.SerializeFloat(quaternion.w, stream);
        }

        public object Deserialize(Stream stream)
        {
            float x = _helper.DeserializeFloat(stream);
            float y = _helper.DeserializeFloat(stream);
            float z = _helper.DeserializeFloat(stream);
            float w = _helper.DeserializeFloat(stream);
            return new Quaternion(x, y, z, w);
        }
    }
}