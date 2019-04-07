using System;
using System.IO;

namespace Skaillz.Ubernet
{
    public interface ICustomTypeSerializer
    {
        void Serialize(object value, Stream stream);
        object Deserialize(Stream stream);
        
        Type Type { get; }
    }
}