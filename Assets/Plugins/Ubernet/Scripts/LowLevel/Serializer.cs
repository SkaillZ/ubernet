using System;
using System.Collections.Generic;
using System.IO;

namespace Skaillz.Ubernet
{
    public class Serializer : ISerializer
    {
        public static class TypeId
        {
            public const byte Null = 0;
            public const byte Byte = 1;
            public const byte Bool = 2;
            public const byte Short = 3;
            public const byte Int = 4;
            public const byte Long = 5;
            public const byte Float = 6;
            public const byte Double = 7;
            public const byte String = 8;

            public const byte TypedArray = 10;
            public const byte ByteArray = 11;
            public const byte ObjectArray = 15;
        }

        private class CustomType
        {
            public CustomType(Type type, byte id, ICustomTypeSerializer serializer)
            {
                Type = type;
                Id = id;
                Serializer = serializer;
            }

            public Type Type { get; }
            public byte Id { get; }
            public ICustomTypeSerializer Serializer { get; }
        }

        private const byte MinCustomCode = 50;

        private readonly Dictionary<Type, CustomType> _typeMappings = new Dictionary<Type, CustomType>();
        private readonly Dictionary<byte, CustomType> _codeMappings = new Dictionary<byte, CustomType>();

        private readonly SerializationHelper _helper = new SerializationHelper();
        private readonly MemoryStream _serializeStream = new MemoryStream(128);
        private byte _nextCustomTypeCode = MinCustomCode;

        public static int MaxCustomTypes => byte.MaxValue - MinCustomCode - 1;

        public void Serialize(object value, Stream stream)
        {
            var type = value?.GetType();
            if (ReferenceEquals(value, null))
            {
                _helper.SerializeByte(TypeId.Null, stream);
            }
            else if (type == typeof(byte))
            {
                _helper.SerializeByte(TypeId.Byte, stream);
                _helper.SerializeByte((byte) value, stream);
            }
            else if (type == typeof(bool))
            {
                _helper.SerializeByte(TypeId.Bool, stream);
                _helper.SerializeBool((bool) value, stream);
            }
            else if (type == typeof(short))
            {
                _helper.SerializeByte(TypeId.Short, stream);
                _helper.SerializeShort((short) value, stream);
            }
            else if (type == typeof(int))
            {
                _helper.SerializeByte(TypeId.Int, stream);
                _helper.SerializeInt((int) value, stream);
            }
            else if (type == typeof(long))
            {
                _helper.SerializeByte(TypeId.Long, stream);
                _helper.SerializeLong((long) value, stream);
            }
            else if (type == typeof(float))
            {
                _helper.SerializeByte(TypeId.Float, stream);
                _helper.SerializeFloat((float) value, stream);
            }
            else if (type == typeof(double))
            {
                _helper.SerializeByte(TypeId.Double, stream);
                _helper.SerializeDouble((double) value, stream);
            }
            else if (type == typeof(string))
            {
                _helper.SerializeByte(TypeId.String, stream);
                _helper.SerializeString((string) value, stream);
            }
            else if (type.IsArray)
            {
                if (type == typeof(byte[]))
                {
                    _helper.SerializeByte(TypeId.ByteArray, stream);
                    _helper.SerializeByteArray((byte[]) value, stream);
                }
                else if (type == typeof(object[]))
                {
                    _helper.SerializeByte(TypeId.ObjectArray, stream);
                    SerializeObjectArray((object[]) value, stream);
                }
                else
                {
                    _helper.SerializeByte(TypeId.TypedArray, stream);

                    var elementType = type.GetElementType();
                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (!_typeMappings.ContainsKey(elementType))
                    {
                        throw new UnknownTypeException(
                            $"The type '{type.FullName}' is not known to the serializer. Please add it as a custom type.");
                    }

                    var customType = _typeMappings[elementType];
                    _helper.SerializeByte(customType.Id, stream);
                    SerializeCustomTypedArray(value, customType, stream);
                }
            }
            else
            {
                if (!_typeMappings.ContainsKey(type))
                {
                    throw new UnknownTypeException(
                        $"The type '{type.FullName}' is not known to the serializer. Please add it as a custom type.");
                }

                var customType = _typeMappings[type];
                _helper.SerializeByte(customType.Id, stream);
                customType.Serializer.Serialize(value, stream);
            }
        }

        public object Deserialize(Stream stream)
        {
            byte typeId = _helper.DeserializeByte(stream);
            switch (typeId)
            {
                case TypeId.Null:
                    return null;
                case TypeId.Byte:
                    return _helper.DeserializeByte(stream);
                case TypeId.Bool:
                    return _helper.DeserializeBool(stream);
                case TypeId.Short:
                    return _helper.DeserializeShort(stream);
                case TypeId.Int:
                    return _helper.DeserializeInt(stream);
                case TypeId.Long:
                    return _helper.DeserializeLong(stream);
                case TypeId.Float:
                    return _helper.DeserializeFloat(stream);
                case TypeId.Double:
                    return _helper.DeserializeDouble(stream);
                case TypeId.String:
                    return _helper.DeserializeString(stream);
                case TypeId.ByteArray:
                    return _helper.DeserializeByteArray(stream);
                case TypeId.ObjectArray:
                    return DeserializeObjectArray(stream);
                case TypeId.TypedArray:
                    byte elementTypeId = _helper.DeserializeByte(stream);
                    if (!_codeMappings.ContainsKey(elementTypeId))
                    {
                        throw new UnknownTypeException(
                            $"Type ID '{elementTypeId}' is not known to the serializer. Please add it as a custom type.");
                    }

                    return DeserializeCustomTypedArray(_codeMappings[elementTypeId], stream);
                default:
                    if (!_codeMappings.ContainsKey(typeId))
                    {
                        throw new UnknownTypeException(
                            $"Type ID '{typeId}' is not known to the serializer. Please add it as a custom type.");
                    }
                    return _codeMappings[typeId].Serializer.Deserialize(stream);
            }
        }

        public byte[] Serialize(NetworkEvent evt)
        {
            ClearBuffer();
            _helper.SerializeInt(evt.SenderId, _serializeStream);
            _helper.SerializeByte(evt.Code, _serializeStream);
            
            Serialize(evt.Data, _serializeStream);

            return _serializeStream.ToArray();
        }

        public NetworkEvent Deserialize(byte[] bytes)
        {
            ClearBuffer();
            _serializeStream.Write(bytes, 0, bytes.Length);

            // After writing, the stream is at the end of the data. Reset it.
            _serializeStream.Seek(0, SeekOrigin.Begin);

            var evt = new NetworkEvent
            {
                SenderId = _helper.DeserializeInt(_serializeStream),
                Code = _helper.DeserializeByte(_serializeStream),
                Data = Deserialize(_serializeStream)
            };

            return evt;
        }

        public void RegisterCustomType(Type type, ICustomTypeSerializer serializer)
        {
            byte nextCode = _nextCustomTypeCode;
            while (_codeMappings.ContainsKey(nextCode))
            {
                if (++nextCode == byte.MaxValue)
                {
                    throw new TypeRegistrationException(
                        $"Type registration failed: You registered too many custom types. You can register up to {MaxCustomTypes} types");
                }
            }

            if (nextCode == byte.MaxValue)
            {
                throw new TypeRegistrationException(
                    $"Type registration failed: You registered too many custom types. You can register up to {MaxCustomTypes} types");
            }

            RegisterCustomType(type, nextCode, serializer);

            _nextCustomTypeCode = ++nextCode;
        }

        public void RegisterCustomType(Type type, byte code, ICustomTypeSerializer serializer)
        {
            if (_typeMappings.ContainsKey(type))
            {
                throw new TypeRegistrationException($"The type '{type.FullName}' is already registered.");
            }

            if (_codeMappings.ContainsKey(code))
            {
                throw new TypeRegistrationException($"The code '{code}' is already registered.");
            }

            var customType = new CustomType(type, code, serializer);
            _typeMappings.Add(type, customType);
            _codeMappings.Add(code, customType);
        }

        public bool IsTypeRegistered(Type type)
        {
            return _typeMappings.ContainsKey(type);
        }

        private byte TypeToID(Type type)
        {
            if (type == null)
            {
                return TypeId.Null;
            }
            
            if (type == typeof(byte))
            {
                return TypeId.Byte;
            }
            
            if (type == typeof(bool))
            {
                return TypeId.Bool;
            }

            if (type == typeof(short))
            {
                return TypeId.Short;
            }

            if (type == typeof(int))
            {
                return TypeId.Int;
            }
            
            if (type == typeof(long))
            {
                return TypeId.Long;
            }

            if (type == typeof(float))
            {
                return TypeId.Float;
            }
            
            if (type == typeof(double))
            {
                return TypeId.Double;
            }

            if (type == typeof(string))
            {
                return TypeId.String;
            }
            
            if (type == typeof(byte[]))
            {
                return TypeId.ByteArray;
            }

            if (type == typeof(object[]))
            {
                return TypeId.ObjectArray;
            }

            if (type == typeof(Array))
            {
                return TypeId.TypedArray;
            }
            
            if (_typeMappings.ContainsKey(type))
            {
                return _typeMappings[type].Id;
            }

            throw new UnknownTypeException(
                $"The type {type.FullName} is not known to the serializer. Please add it as a custom type.");
        }
        
        private void SerializeCustomTypedArray(object value, CustomType type, Stream stream)
        {
            var array = (Array) value;
            
            // Write length
            _helper.SerializeInt(array.Length, stream);

            var serializer = type.Serializer;
            foreach (var elem in array)
            {
                serializer.Serialize(elem, stream);
            }
        }
        
        private Array DeserializeCustomTypedArray(CustomType type, Stream stream)
        {
            // Read length
            int length = _helper.DeserializeInt(stream);
            
            var array = Array.CreateInstance(type.Type, length);
            var serializer = type.Serializer;
            for (int i = 0; i < length; i++)
            {
                array.SetValue(serializer.Deserialize(stream), i);
            }

            return array;
        }
        
        private void SerializeObjectArray(object[] array, Stream stream)
        {
            // Write length
            _helper.SerializeInt(array.Length, stream);

            foreach (var elem in array)
            {
                Serialize(elem, stream);
            }
        }
        
        private object[] DeserializeObjectArray(Stream stream)
        {
            // Read length
            int length = _helper.DeserializeInt(stream);
            var array = new object[length];

            for (var i = 0; i < length; i++)
            {
                array[i] = Deserialize(stream);
            }

            return array;
        }

        private void ClearBuffer()
        {
            _serializeStream.Seek(0, SeekOrigin.Begin);
            _serializeStream.SetLength(0);
        }
        
        public class TypeRegistrationException : UbernetException
        {
            public TypeRegistrationException(string message) : base(message)
            {
            }
        }
       
    }
}