using System.IO;
using NUnit.Framework;

namespace Skaillz.Ubernet.Tests
{
    public class SerializerTest
    {
        private Serializer _serializer;
        private Stream _stream;
        
        [SetUp]
        public void BeforeEach()
        {
            _serializer = new Serializer();
            _stream = new MemoryStream();
        }
        
        [Test]
        public void SerializesAndDeserializes_NetworkEvent()
        {
            var original = TestUtils.CreateNetworkEvent("foo", 2, 1);
            var serialized = _serializer.Deserialize(_serializer.Serialize(original));
            
            Assert.AreEqual(original.SenderId, serialized.SenderId);
            Assert.AreEqual(original.Code, serialized.Code);
            Assert.AreEqual(original.Data, serialized.Data);
        }
        
        [Test]
        public void SerializesAndDeserializes_CustomTypedArray()
        {
            _serializer.RegisterCustomType(new TestClassSerializer());
            
            var arr = new[] { new TestClass {Name = "foo"}, new TestClass {Name = "bar"} };

            _serializer.Serialize(arr, _stream);
            _stream.Seek(0, SeekOrigin.Begin);
            var deserializedArr = (TestClass[]) _serializer.Deserialize(_stream);

            Assert.AreEqual(arr.Length, deserializedArr.Length, "Length matches");
            for (var i = 0; i < deserializedArr.Length; i++)
            {
                Assert.AreEqual(arr[i].Name, deserializedArr[i].Name);
            }
        }
        
        [Test]
        public void SerializesAndDeserializes_ObjectArray()
        {
            _serializer.RegisterCustomType(new TestClassSerializer());
            
            var arr = new object[] { 1, 2, "foo" };

            _serializer.Serialize(arr, _stream);
            _stream.Seek(0, SeekOrigin.Begin);
            var deserializedArr = (object[]) _serializer.Deserialize(_stream);

            Assert.AreEqual(arr, deserializedArr);
        }

        class TestClassSerializer : CustomTypeSerializer<TestClass>
        {
            public override void Serialize(TestClass value, Stream stream)
            {
                SerializationHelper.SerializeString(value.Name, stream);
            }

            public override TestClass Deserialize(Stream stream)
            {
                return new TestClass
                {
                    Name = SerializationHelper.DeserializeString(stream)
                };
            }
        }

        class TestClass
        {
            public string Name { get; set; }

            protected bool Equals(TestClass other)
            {
                return string.Equals(Name, other.Name);
            }

#pragma warning disable 659
            public override bool Equals(object obj)
#pragma warning restore 659
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((TestClass) obj);
            }
        }
    }
}