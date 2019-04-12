using System;
using System.Diagnostics;
using System.IO;

namespace Skaillz.Ubernet
{
    public static class UbernetUtils
    {
        private static readonly Stopwatch Stopwatch = new Stopwatch();

        internal static bool AreArraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length)
            {
                return false;
            }
            
            for (int i = 0; i < a1.Length; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
            }

            return true;
        }
        
        internal static bool AreArraysEqual(byte[] a1, byte[] a2, int length)
        {
            for (int i = 0; i < length; i++)
            {
                if (a1[i] != a2[i])
                {
                    return false;
                }
            }

            return true;
        }

        public static double GetCurrentTime()
        {
            return Stopwatch.ElapsedMilliseconds;
        }
        
        public static void Clear(this MemoryStream source)
        {
            byte[] buffer = source.GetBuffer();
            source.Position = 0;
            source.SetLength(0);
        }
        
        public static void From(this MemoryStream source, byte[] data)
        {
            From(source, data, data.Length);
        }
        
        public static void From(this MemoryStream source, byte[] data, int length)
        {
            source.Position = 0;
            source.SetLength(length);
            byte[] buffer = source.GetBuffer();
            Buffer.BlockCopy(data, 0, buffer, 0, length);
        }
    }
}