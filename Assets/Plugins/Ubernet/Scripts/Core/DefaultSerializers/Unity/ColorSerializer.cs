using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class ColorSerializer : ICustomTypeSerializer
    {
        private readonly SerializationHelper _helper = new SerializationHelper();
        
        public void Serialize(object value, Stream stream)
        {
            var col = (Color) value;
            _helper.SerializeFloat(col.r, stream);
            _helper.SerializeFloat(col.g, stream);
            _helper.SerializeFloat(col.b, stream);
            _helper.SerializeFloat(col.a, stream);
        }

        public object Deserialize(Stream stream)
        {
            float r = _helper.DeserializeFloat(stream);
            float g = _helper.DeserializeFloat(stream);
            float b = _helper.DeserializeFloat(stream);
            float a = _helper.DeserializeFloat(stream);
            return new Color(r, g, b, a);
        }
    }
}