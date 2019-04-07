using System;
using System.IO;
using System.Text;

namespace Skaillz.Ubernet
{
    public static class SerializationHelper
    {
        private static readonly float[] SerializeFloatBuffer = new float[1];
        private static readonly long[] SerializeLongBuffer = new long[1];
        private static readonly double[] SerializeDoubleBuffer = new double[1];

        private static readonly byte[] ShortByteBuffer = new byte[2];
        private static readonly byte[] IntByteBuffer = new byte[4];
        private static readonly byte[] LongByteBuffer = new byte[8];
        private static readonly byte[] FloatByteBuffer = new byte[4];
        private static readonly byte[] DoubleByteBuffer = new byte[8];
        private static byte[] _stringByteBuffer = new byte[50];
        
        public static void SerializeBool(bool value, Stream stream)
        {
            stream.WriteByte(value ? (byte) 1 : (byte) 0);
        }
        
        public static void SerializeByte(byte value, Stream stream)
        {
            stream.WriteByte(value);
        }

        public static void SerializeShort(short value, Stream stream)
        {
            stream.WriteByte((byte) ((uint) value >> 8));
            stream.WriteByte((byte) value);
        }

        public static void SerializeInt(int value, Stream stream)
        {
            stream.WriteByte((byte) (value >> 24));
            stream.WriteByte((byte) (value >> 16));
            stream.WriteByte((byte) (value >> 8));
            stream.WriteByte((byte) value);
        }

        public static void SerializeLong(long value, Stream stream)
        {
            SerializeLongBuffer[0] = value;
            Buffer.BlockCopy(SerializeLongBuffer, 0, LongByteBuffer, 0, 8);
            byte[] buffer = LongByteBuffer;
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

        public static void SerializeString(string value, Stream stream)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > short.MaxValue)
                throw new NotSupportedException(
                    "Strings that exceed a UTF8-encoded byte-length of 32767 (short.MaxValue) are not supported. Yours is: " +
                    bytes.Length);

            SerializeShort((short) bytes.Length, stream);
            stream.Write(bytes, 0, bytes.Length);
        }

        public static void SerializeFloat(float value, Stream stream)
        {
            SerializeFloatBuffer[0] = value;
            Buffer.BlockCopy(SerializeFloatBuffer, 0, FloatByteBuffer, 0, 4);

            if (BitConverter.IsLittleEndian)
            {
                byte t1 = FloatByteBuffer[0];
                byte t2 = FloatByteBuffer[1];
                FloatByteBuffer[0] = FloatByteBuffer[3];
                FloatByteBuffer[1] = FloatByteBuffer[2];
                FloatByteBuffer[2] = t2;
                FloatByteBuffer[3] = t1;
            }

            stream.Write(FloatByteBuffer, 0, 4);
        }

        public static void SerializeDouble(double value, Stream stream)
        {
            SerializeDoubleBuffer[0] = value;
            Buffer.BlockCopy(SerializeDoubleBuffer, 0, DoubleByteBuffer, 0, 8);
            byte[] buffer = DoubleByteBuffer;
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
        
        public static void SerializeByteArray(byte[] array, Stream stream)
        {
            SerializeInt(array.Length, stream);
            stream.Write(array, 0, array.Length);
        }
        
        public static void SerializeByteArrayWithByteLength(byte[] array, Stream stream)
        {
            SerializeByte((byte) array.Length, stream);
            stream.Write(array, 0, array.Length);
        }

        public static bool DeserializeBool(Stream stream)
        {
            return (byte) stream.ReadByte() > 0;
        }

        public static byte DeserializeByte(Stream stream)
        {
            return (byte) stream.ReadByte();
        }

        public static short DeserializeShort(Stream stream)
        {
            byte[] buffer = ShortByteBuffer;
            stream.Read(buffer, 0, 2);
            return (short) (buffer[0] << 8 | buffer[1]);
        }

        public static int DeserializeInt(Stream stream)
        {
            byte[] buffer = IntByteBuffer;
            stream.Read(buffer, 0, 4);
            return buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];
        }
        
        public static long DeserializeLong(Stream stream)
        {
            byte[] buffer = LongByteBuffer;
            stream.Read(buffer, 0, 8);
            if (BitConverter.IsLittleEndian)
                return (long) LongByteBuffer[0] << 56 | (long) LongByteBuffer[1] << 48 | (long) LongByteBuffer[2] << 40 
                       | (long) LongByteBuffer[3] << 32 | (long) LongByteBuffer[4] << 24 | (long) LongByteBuffer[5] << 16
                       | (long) LongByteBuffer[6] << 8 | (long) LongByteBuffer[7];
            return BitConverter.ToInt64(LongByteBuffer, 0);
        }

        public static string DeserializeString(Stream stream)
        {
            short length = DeserializeShort(stream);

            if (_stringByteBuffer.Length < length)
                _stringByteBuffer = new byte[length];

            stream.Read(_stringByteBuffer, 0, length);
            return Encoding.UTF8.GetString(_stringByteBuffer, 0, length);
        }

        public static float DeserializeFloat(Stream stream)
        {
            byte[] buffer = FloatByteBuffer;
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
        
        public static double DeserializeDouble(Stream stream)
        {
            byte[] buffer = DoubleByteBuffer;
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
        
        public static byte[] DeserializeByteArray(Stream stream)
        {
            int length = DeserializeInt(stream);
            byte[] array = new byte[length];
            stream.Read(array, 0, length);

            return array;
        }
        
        public static byte[] DeserializeByteArrayWithByteLength(Stream stream)
        {
            byte length = DeserializeByte(stream);
            byte[] array = new byte[length];
            stream.Read(array, 0, length);

            return array;
        }
    }
}