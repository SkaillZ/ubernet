using System.IO;
using UnityEngine;

namespace Skaillz.Ubernet.DefaultSerializers.Unity
{
    public class UnityJsonSerializer<T> : ICustomTypeSerializer
    {
        private readonly SerializationHelper _helper = new SerializationHelper();

        public bool PrettyPrint { get; set; }
        
        public void Serialize(object value, Stream stream)
        {
            string json = JsonUtility.ToJson(value, PrettyPrint);
            _helper.SerializeString(json, stream);
        }

        public object Deserialize(Stream stream)
        {
            string json = _helper.DeserializeString(stream);
            return JsonUtility.FromJson<T>(json);
        }
    }
}