using System.Diagnostics;

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

        public static double GetCurrentTime()
        {
            return Stopwatch.ElapsedMilliseconds;
        }
    }
}