using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class Vector2Serializer : CustomTypeSerializer<Vector2>
    {
        public override void Serialize(Vector2 value, Stream stream)
        {
            SerializationHelper.SerializeFloat(value.x, stream);
            SerializationHelper.SerializeFloat(value.y, stream);
        }

        public override Vector2 Deserialize(Stream stream)
        {
            float x = SerializationHelper.DeserializeFloat(stream);
            float y = SerializationHelper.DeserializeFloat(stream);
            return new Vector2(x, y);
        }
    }
}