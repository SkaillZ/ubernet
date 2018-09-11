using System.IO;
using NUnit.Framework;
using Skaillz.Ubernet.NetworkEntities;

namespace Skaillz.Ubernet.Tests
{
    public class SyncedValueSerializerTest
    {
        [Test]
        public void Serializes_And_Deserializes()
        {
            var syncedValue = new SyncedValue<int>(1);
            
            var synchronizer = new SyncedValueSerializer(new TestClass { MyValue = syncedValue }, new Serializer());

            var stream = new MemoryStream();
            synchronizer.Serialize(stream);
            syncedValue.Value = 2;
            
            Assert.AreEqual(2, syncedValue.Value);

            stream.Seek(0, SeekOrigin.Begin);
            synchronizer.Deserialize(stream);
            
            Assert.AreEqual(1, syncedValue.Value);
        }

        public class TestClass
        {
            public SyncedValue<int> MyValue { get; set; }
        }
    }
}