using System;
using System.IO;

namespace Skaillz.Ubernet
{
    public abstract class CustomTypeSerializer<T>: ICustomTypeSerializer
    {
        public abstract void Serialize(T value, Stream stream);
        public abstract T Deserialize(Stream stream);

        public Type Type => typeof(T);

        public void Serialize(object value, Stream stream)
        {
            Serialize((T) value, stream);
        }

        object ICustomTypeSerializer.Deserialize(Stream stream)
        {
            return Deserialize(stream);
        }
    }
}