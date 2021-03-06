﻿using System;
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
            public CustomType(byte id, ICustomTypeSerializer serializer)
            {
                Type = serializer.Type;
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

        private readonly MemoryStream _serializeStream = new MemoryStream(128);
        private byte _nextCustomTypeCode = MinCustomCode;

        public static int MaxCustomTypes => byte.MaxValue - MinCustomCode - 1;

        public void Serialize(object value, Stream stream)
        {
            var type = value?.GetType();
            if (ReferenceEquals(value, null))
            {
                SerializationHelper.SerializeByte(TypeId.Null, stream);
            }
            else if (type == typeof(byte))
            {
                SerializationHelper.SerializeByte(TypeId.Byte, stream);
                SerializationHelper.SerializeByte((byte) value, stream);
            }
            else if (type == typeof(bool))
            {
                SerializationHelper.SerializeByte(TypeId.Bool, stream);
                SerializationHelper.SerializeBool((bool) value, stream);
            }
            else if (type == typeof(short))
            {
                SerializationHelper.SerializeByte(TypeId.Short, stream);
                SerializationHelper.SerializeShort((short) value, stream);
            }
            else if (type == typeof(int))
            {
                SerializationHelper.SerializeByte(TypeId.Int, stream);
                SerializationHelper.SerializeInt((int) value, stream);
            }
            else if (type == typeof(long))
            {
                SerializationHelper.SerializeByte(TypeId.Long, stream);
                SerializationHelper.SerializeLong((long) value, stream);
            }
            else if (type == typeof(float))
            {
                SerializationHelper.SerializeByte(TypeId.Float, stream);
                SerializationHelper.SerializeFloat((float) value, stream);
            }
            else if (type == typeof(double))
            {
                SerializationHelper.SerializeByte(TypeId.Double, stream);
                SerializationHelper.SerializeDouble((double) value, stream);
            }
            else if (type == typeof(string))
            {
                SerializationHelper.SerializeByte(TypeId.String, stream);
                SerializationHelper.SerializeString((string) value, stream);
            }
            else if (type.IsArray)
            {
                if (type == typeof(byte[]))
                {
                    SerializationHelper.SerializeByte(TypeId.ByteArray, stream);
                    SerializationHelper.SerializeByteArray((byte[]) value, stream);
                }
                else if (type == typeof(object[]))
                {
                    SerializationHelper.SerializeByte(TypeId.ObjectArray, stream);
                    SerializeObjectArray((object[]) value, stream);
                }
                else
                {
                    SerializationHelper.SerializeByte(TypeId.TypedArray, stream);

                    var elementType = type.GetElementType();
                    // ReSharper disable once AssignNullToNotNullAttribute
                    if (!_typeMappings.ContainsKey(elementType))
                    {
                        throw new UnknownTypeException(
                            $"The type '{type.FullName}' is not known to the serializer. Please add it as a custom type.");
                    }

                    var customType = _typeMappings[elementType];
                    SerializationHelper.SerializeByte(customType.Id, stream);
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
                SerializationHelper.SerializeByte(customType.Id, stream);
                customType.Serializer.Serialize(value, stream);
            }
        }

        public object Deserialize(Stream stream)
        {
            byte typeId = SerializationHelper.DeserializeByte(stream);
            switch (typeId)
            {
                case TypeId.Null:
                    return null;
                case TypeId.Byte:
                    return SerializationHelper.DeserializeByte(stream);
                case TypeId.Bool:
                    return SerializationHelper.DeserializeBool(stream);
                case TypeId.Short:
                    return SerializationHelper.DeserializeShort(stream);
                case TypeId.Int:
                    return SerializationHelper.DeserializeInt(stream);
                case TypeId.Long:
                    return SerializationHelper.DeserializeLong(stream);
                case TypeId.Float:
                    return SerializationHelper.DeserializeFloat(stream);
                case TypeId.Double:
                    return SerializationHelper.DeserializeDouble(stream);
                case TypeId.String:
                    return SerializationHelper.DeserializeString(stream);
                case TypeId.ByteArray:
                    return SerializationHelper.DeserializeByteArray(stream);
                case TypeId.ObjectArray:
                    return DeserializeObjectArray(stream);
                case TypeId.TypedArray:
                    byte elementTypeId = SerializationHelper.DeserializeByte(stream);
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

        public byte[] Serialize(NetworkEvent evt, out int length)
        {
            _serializeStream.Clear();
            SerializationHelper.SerializeInt(evt.SenderId, _serializeStream);
            SerializationHelper.SerializeByte(evt.Code, _serializeStream);
            
            Serialize(evt.Data, _serializeStream);

            length = (int) _serializeStream.Length;
            return _serializeStream.GetBuffer();
        }

        public NetworkEvent Deserialize(byte[] bytes, int length)
        {
            _serializeStream.From(bytes, length);

            return new NetworkEvent
            {
                SenderId = SerializationHelper.DeserializeInt(_serializeStream),
                Code = SerializationHelper.DeserializeByte(_serializeStream),
                Data = Deserialize(_serializeStream)
            };
        }

        public void RegisterCustomType<T>(CustomTypeSerializer<T> serializer)
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

            RegisterCustomType(nextCode, serializer);

            _nextCustomTypeCode = ++nextCode;
        }

        public void RegisterCustomType<T>(byte code, CustomTypeSerializer<T> serializer)
        {
            var type = typeof(T);
            if (_typeMappings.ContainsKey(type))
            {
                throw new TypeRegistrationException($"The type '{type.FullName}' is already registered.");
            }

            if (_codeMappings.ContainsKey(code))
            {
                throw new TypeRegistrationException($"The code '{code}' is already registered.");
            }

            var customType = new CustomType(code, serializer);
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
            SerializationHelper.SerializeInt(array.Length, stream);

            var serializer = type.Serializer;
            foreach (var elem in array)
            {
                serializer.Serialize(elem, stream);
            }
        }
        
        private Array DeserializeCustomTypedArray(CustomType type, Stream stream)
        {
            // Read length
            int length = SerializationHelper.DeserializeInt(stream);
            
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
            SerializationHelper.SerializeInt(array.Length, stream);

            foreach (var elem in array)
            {
                Serialize(elem, stream);
            }
        }
        
        private object[] DeserializeObjectArray(Stream stream)
        {
            // Read length
            int length = SerializationHelper.DeserializeInt(stream);
            var array = new object[length];

            for (var i = 0; i < length; i++)
            {
                array[i] = Deserialize(stream);
            }

            return array;
        }
        
        public class TypeRegistrationException : UbernetException
        {
            public TypeRegistrationException(string message) : base(message)
            {
            }
        }
       
    }
}