using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class Vector3Serializer : CustomTypeSerializer<Vector3>
    {       
        public override void Serialize(Vector3 value, Stream stream)
        {
            SerializationHelper.SerializeFloat(value.x, stream);
            SerializationHelper.SerializeFloat(value.y, stream);
            SerializationHelper.SerializeFloat(value.z, stream);
        }

        public override Vector3 Deserialize(Stream stream)
        {
            float x = SerializationHelper.DeserializeFloat(stream);
            float y = SerializationHelper.DeserializeFloat(stream);
            float z = SerializationHelper.DeserializeFloat(stream);
            return new Vector3(x, y, z);
        }
    }
}