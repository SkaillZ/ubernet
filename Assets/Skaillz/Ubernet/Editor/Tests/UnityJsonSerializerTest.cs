using System;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Skaillz.Ubernet.DefaultSerializers.Unity;
using UnityEngine;

namespace Skaillz.Ubernet.Tests
{
    public class UnityJsonSerializerTest
    {
        private ISerializer _serializer;
        
        [SetUp]
        public void BeforeEach()
        {
            _serializer = new Serializer();
            _serializer.RegisterCustomType(typeof(TestSerializable), new UnityJsonSerializer<TestSerializable>());
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
        private class TestSerializable
        {
            [SerializeField] private string _name;
            [SerializeField] private int _age;

            public string Name
            {
                get { return _name; }
                set { _name = value; }
            }

            public int Age
            {
                get { return _age; }
                set { _age = value; }
            }

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