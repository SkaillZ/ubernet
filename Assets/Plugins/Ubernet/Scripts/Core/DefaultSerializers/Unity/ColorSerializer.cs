using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class ColorSerializer : CustomTypeSerializer<Color>
    {
        public override void Serialize(Color value, Stream stream)
        {
            SerializationHelper.SerializeFloat(value.r, stream);
            SerializationHelper.SerializeFloat(value.g, stream);
            SerializationHelper.SerializeFloat(value.b, stream);
            SerializationHelper.SerializeFloat(value.a, stream);
        }

        public override Color Deserialize(Stream stream)
        {
            float r = SerializationHelper.DeserializeFloat(stream);
            float g = SerializationHelper.DeserializeFloat(stream);
            float b = SerializationHelper.DeserializeFloat(stream);
            float a = SerializationHelper.DeserializeFloat(stream);
            return new Color(r, g, b, a);
        }
    }
}