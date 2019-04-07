using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class QuaternionSerializer : CustomTypeSerializer<Quaternion>
    {
        public override void Serialize(Quaternion value, Stream stream)
        {
            SerializationHelper.SerializeFloat(value.x, stream);
            SerializationHelper.SerializeFloat(value.y, stream);
            SerializationHelper.SerializeFloat(value.z, stream);
            SerializationHelper.SerializeFloat(value.w, stream);
        }

        public override Quaternion Deserialize(Stream stream)
        {
            float x = SerializationHelper.DeserializeFloat(stream);
            float y = SerializationHelper.DeserializeFloat(stream);
            float z = SerializationHelper.DeserializeFloat(stream);
            float w = SerializationHelper.DeserializeFloat(stream);
            return new Quaternion(x, y, z, w);
        }
    }
}