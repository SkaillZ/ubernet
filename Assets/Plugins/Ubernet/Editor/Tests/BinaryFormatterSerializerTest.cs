using System;
using NUnit.Framework;
using Skaillz.Ubernet.DefaultSerializers;

namespace Skaillz.Ubernet.Tests
{
    public class BinaryFormatterSerializerTest
    {
        private ISerializer _serializer;
        
        [SetUp]
        public void BeforeEach()
        {
            _serializer = new Serializer();
            _serializer.RegisterCustomType(typeof(TestSerializable), new BinaryFormatterSerializer());
        }

        [Test]
        public void SerializesAndDeserializesObject()
        {
            var p = new TestSerializable { Name = "Hans", Age = 22 };
            Assert.AreEqual(p, _serializer.Deserialize(
                _serializer.Serialize(TestUtils.CreateNetworkEvent(p))
            ).Data);
        }
        
        [Serializable]
        internal class TestSerializable
        {
            public string Name { get; set; }
            public int Age { get; set; }

            protected bool Equals(TestSerializable other)
            {
                return string.Equals(Name, other.Name) && Age == other.Age;
            }

#pragma warning disable 659
            public override bool Equals(object obj)
#pragma warning restore 659
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                return Equals((TestSerializable) obj);
            }
        }
    }

}