using System;
using System.IO;
using System.Text;

namespace Skaillz.Ubernet
{
    public class SerializationHelper
    {
        private readonly float[] _serializeFloatBuffer = new float[1];
        private readonly long[] _serializeLongBuffer = new long[1];
        private readonly double[] _serializeDoubleBuffer = new double[1];

        private readonly byte[] _shortByteBuffer = new byte[2];
        private readonly byte[] _intByteBuffer = new byte[4];
        private readonly byte[] _longByteBuffer = new byte[8];
        private readonly byte[] _floatByteBuffer = new byte[4];
        private readonly byte[] _doubleByteBuffer = new byte[8];
        private byte[] _stringByteBuffer = new byte[50];
        
        public void SerializeBool(bool value, Stream stream)
        {
            stream.WriteByte(value ? (byte) 1 : (byte) 0);
        }
        
        public void SerializeByte(byte value, Stream stream)
        {
            stream.WriteByte(value);
        }

        public void SerializeShort(short value, Stream stream)
        {
            stream.WriteByte((byte) ((uint) value >> 8));
            stream.WriteByte((byte) value);
        }

        public void SerializeInt(int value, Stream stream)
        {
            stream.WriteByte((byte) (value >> 24));
            stream.WriteByte((byte) (value >> 16));
            stream.WriteByte((byte) (value >> 8));
            stream.WriteByte((byte) value);
        }

        public void SerializeLong(long value, Stream stream)
        {
            _serializeLongBuffer[0] = value;
            Buffer.BlockCopy(_serializeLongBuffer, 0, _longByteBuffer, 0, 8);
            byte[] buffer = _longByteBuffer;
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = buffer[0];
                byte num2 = buffer[1];
                byte num3 = buffer[2];
                byte num4 = buffer[3];
                buffer[0] = buffer[7];
                buffer[1] = buffer[6];
                buffer[2] = buffer[5];
                buffer[3] = buffer[4];
                buffer[4] = num4;
                buffer[5] = num3;
                buffer[6] = num2;
                buffer[7] = num1;
            }
            stream.Write(buffer, 0, 8);
        }

        public void SerializeString(string value, Stream stream)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > short.MaxValue)
                throw new NotSupportedException(
                    "Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " +
                    bytes.Length);

            SerializeShort((short) bytes.Length, stream);
            stream.Write(bytes, 0, bytes.Length);
        }

        public void SerializeFloat(float value, Stream stream)
        {
            _serializeFloatBuffer[0] = value;
            Buffer.BlockCopy(_serializeFloatBuffer, 0, _floatByteBuffer, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                byte t1 = _floatByteBuffer[0];
                byte t2 = _floatByteBuffer[1];
                _floatByteBuffer[0] = _floatByteBuffer[3];
                _floatByteBuffer[1] = _floatByteBuffer[2];
                _floatByteBuffer[2] = t2;
                _floatByteBuffer[3] = t1;
            }

            stream.Write(_floatByteBuffer, 0, 4);
        }

        public void SerializeDouble(double value, Stream stream)
        {
            _serializeDoubleBuffer[0] = value;
            Buffer.BlockCopy(_serializeDoubleBuffer, 0, _doubleByteBuffer, 0, 8);
            byte[] buffer = _doubleByteBuffer;
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = buffer[0];
                byte num2 = buffer[1];
                byte num3 = buffer[2];
                byte num4 = buffer[3];
                buffer[0] = buffer[7];
                buffer[1] = buffer[6];
                buffer[2] = buffer[5];
                buffer[3] = buffer[4];
                buffer[4] = num4;
                buffer[5] = num3;
                buffer[6] = num2;
                buffer[7] = num1;
            }
            
            stream.Write(buffer, 0, 8);
        }
        
        public void SerializeByteArray(byte[] array, Stream stream)
        {
            SerializeInt(array.Length, stream);
            foreach (byte b in array)
            {
                SerializeByte(b, stream);
            }
        }
        
        public void SerializeByteArrayWithByteLength(byte[] array, Stream stream)
        {
            SerializeByte((byte) array.Length, stream);
            foreach (byte b in array)
            {
                SerializeByte(b, stream);
            }
        }

        public bool DeserializeBool(Stream stream)
        {
            return (byte) stream.ReadByte() > 0;
        }

        public byte DeserializeByte(Stream stream)
        {
            return (byte) stream.ReadByte();
        }

        public short DeserializeShort(Stream stream)
        {
            byte[] buffer = _shortByteBuffer;
            stream.Read(buffer, 0, 2);
            return (short) (buffer[0] << 8 | buffer[1]);
        }

        public int DeserializeInt(Stream stream)
        {
            byte[] buffer = _intByteBuffer;
            stream.Read(buffer, 0, 4);
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }
        
        public long DeserializeLong(Stream stream)
        {
            byte[] buffer = _longByteBuffer;
            stream.Read(buffer, 0, 8);
            if (BitConverter.IsLittleEndian)
                return (long) _longByteBuffer[0] << 56 | (long) _longByteBuffer[1] << 48 | (long) _longByteBuffer[2] << 40 
                       | (long) _longByteBuffer[3] << 32 | (long) _longByteBuffer[4] << 24 | (long) _longByteBuffer[5] << 16
                       | (long) _longByteBuffer[6] << 8 | (long) _longByteBuffer[7];
            return BitConverter.ToInt64(_longByteBuffer, 0);
        }

        public string DeserializeString(Stream stream)
        {
            short length = DeserializeShort(stream);

            if (_stringByteBuffer.Length < length)
                _stringByteBuffer = new byte[length];

            stream.Read(_stringByteBuffer, 0, length);
            return Encoding.UTF8.GetString(_stringByteBuffer, 0, length);
        }

        public float DeserializeFloat(Stream stream)
        {
            byte[] buffer = _floatByteBuffer;
            stream.Read(buffer, 0, 4);
            if (BitConverter.IsLittleEndian)
            {
                byte t1 = buffer[0];
                byte t2 = buffer[1];
                buffer[0] = buffer[3];
                buffer[1] = buffer[2];
                buffer[2] = t2;
                buffer[3] = t1;
            }

            return BitConverter.ToSingle(buffer, 0);
        }
        
        public double DeserializeDouble(Stream stream)
        {
            byte[] buffer = _doubleByteBuffer;
            stream.Read(buffer, 0, 8);
            if (BitConverter.IsLittleEndian)
            {
                byte num1 = buffer[0];
                byte num2 = buffer[1];
                byte num3 = buffer[2];
                byte num4 = buffer[3];
                buffer[0] = buffer[7];
                buffer[1] = buffer[6];
                buffer[2] = buffer[5];
                buffer[3] = buffer[4];
                buffer[4] = num4;
                buffer[5] = num3;
                buffer[6] = num2;
                buffer[7] = num1;
            }

            return BitConverter.ToDouble(buffer, 0);
        }
        
        public byte[] DeserializeByteArray(Stream stream)
        {
            int length = DeserializeInt(stream);
            byte[] array = new byte[length];

            for (var i = 0; i < array.Length; i++)
            {
                array[i] = DeserializeByte(stream);
            }

            return array;
        }
        
        public byte[] DeserializeByteArrayWithByteLength(Stream stream)
        {
            byte length = DeserializeByte(stream);
            byte[] array = new byte[length];

            for (var i = 0; i < array.Length; i++)
            {
                array[i] = DeserializeByte(stream);
            }

            return array;
        }
    }
}