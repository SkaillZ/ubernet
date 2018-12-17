using System.IO;

namespace Skaillz.Ubernet
{
    public interface ICustomSerializable
    {
        void Serialize(Stream stream);
        void Deserialize(Stream stream);
    }
}