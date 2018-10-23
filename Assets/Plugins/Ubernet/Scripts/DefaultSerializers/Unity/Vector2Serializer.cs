using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class Vector2Serializer : ICustomTypeSerializer
    {
        private readonly SerializationHelper _helper = new SerializationHelper();
        
        public void Serialize(object value, Stream stream)
        {
            var vector = (Vector2) value;
            _helper.SerializeFloat(vector.x, stream);
            _helper.SerializeFloat(vector.y, stream);
        }

        public object Deserialize(Stream stream)
        {
            float x = _helper.DeserializeFloat(stream);
            float y = _helper.DeserializeFloat(stream);
            return new Vector2(x, y);
        }
    }
}