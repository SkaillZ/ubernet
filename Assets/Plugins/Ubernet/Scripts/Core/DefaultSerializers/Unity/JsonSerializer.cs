using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class UnityJsonSerializer<T> : CustomTypeSerializer<T>
    {
        public bool PrettyPrint { get; set; }
        
        public override void Serialize(T value, Stream stream)
        {
            string json = JsonUtility.ToJson(value, PrettyPrint);
            SerializationHelper.SerializeString(json, stream);
        }

        public override T Deserialize(Stream stream)
        {
            string json = SerializationHelper.DeserializeString(stream);
            return JsonUtility.FromJson<T>(json);
        }
    }
}