using System;
using System.IO;
using NUnit.Framework;

namespace Skaillz.Ubernet.Tests
{
    public class SerializationHelperTest
    {
        private SerializationHelper _helper;
        private Stream _stream;
        
        [SetUp]
        public void BeforeEach()
        {
            _helper = new SerializationHelper();
            _stream = new MemoryStream();
        }
        
        [Test]
        public void Byte()
        {
            SerializesAndDeserializes("Byte", (byte) 50);
        }
        
        [Test]
        public void Bool_False()
        {
            SerializesAndDeserializes("Bool", false);
        }
        
        [Test]
        public void Bool_True()
        {
            SerializesAndDeserializes("Bool", true);
        }
        
        [Test]
        public void Short()
        {
            SerializesAndDeserializes("Short", (short) 50);
        }
        
        [Test]
        public void NegativeShort()
        {
            SerializesAndDeserializes("Short", (short) -50);
        }
        
        [Test]
        public void Int()
        {
            SerializesAndDeserializes("Int", 50);
        }
        
        [Test]
        public void Long()
        {
            SerializesAndDeserializes("Long", long.MaxValue);
        }
        
        [Test]
        public void NegativeInt()
        {
            SerializesAndDeserializes("Int", -50);
        }
        
        [Test]
        public void String()
        {
            SerializesAndDeserializes("String", "Hóla, World!");
        }
        
        [Test]
        public void Float()
        {
            SerializesAndDeserializes("Float", 3.41f);
        }
        
        [Test]
        public void NegativeFloat()
        {
            SerializesAndDeserializes("Float", -3.41f);
        }
        
        [Test]
        public void FloatPositiveInfinity()
        {
            SerializesAndDeserializes("Float", float.PositiveInfinity);
        }
        
        [Test]
        public void FloatNegativeInfinity()
        {
            SerializesAndDeserializes("Float", float.NegativeInfinity);
        }
        
        [Test]
        public void Double()
        {
            SerializesAndDeserializes("Double", 3.41d);
        }
        
        [Test]
        public void NegativeDouble()
        {
            SerializesAndDeserializes("Double", -3.41d);
        }
        
        [Test]
        public void DoublePositiveInfinity()
        {
            SerializesAndDeserializes("Double", double.PositiveInfinity);
        }
        
        [Test]
        public void DoubleNegativeInfinity()
        {
            SerializesAndDeserializes("Double", double.NegativeInfinity);
        }
        
        [Test]
        public void ByteArray()
        {
            SerializesAndDeserializes("ByteArray", new byte[] { 5, 100, 255 });
        }
        
        [Test]
        public void String_SerializeThrowsException_WhenTooLong()
        {
            Assert.Throws<NotSupportedException>(() =>
            {
                string str = new string(new char[short.MaxValue + 1]);
                _helper.SerializeString(str, _stream);
            });
        }
        
        private void SerializesAndDeserializes<T>(string typeName, T value)
        {
            // ReSharper disable PossibleNullReferenceException
            _helper.GetType().GetMethod($"Serialize{typeName}")
                .Invoke(_helper, new object[] { value, _stream });
            
            _stream.Seek(0, SeekOrigin.Begin);
            var deserializedValue = (T) _helper.GetType().GetMethod($"Deserialize{typeName}")
                .Invoke(_helper, new object[] { _stream });
            
            Assert.AreEqual(value, deserializedValue);
        }
    }
}