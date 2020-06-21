using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace TP1
{
    public static class Utils
    {
        public static string CurrentDirectory => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);


        /// <summary>
        ///     Convert to binary string
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        public static string ToBinaryString(byte[] target)
        {
            if (null == target)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return String.Join(" ", target.Select(a => Convert.ToString(a, 2).PadLeft(8, '0')));
        }

        public static int Log2(int value)
        {
            int i;
            for (i = -1; value != 0; i++)
                value >>= 1;

            return (i == -1) ? 0 : i;
        }

        // https://www.geeksforgeeks.org/highest-power-2-less-equal-given-number/
        public static int HighestPowerOf2(int n)
        {
            return Log2(n & (~(n - 1)));
        }

        public static bool IsPowerOfTwo(int x)
        {
            // x will check if x == 0 and !(x & (x - 1)) will check if x is a power of 2 or not
            return (
                x != 0 &&
                (x & (x - 1)) == 0); // renmoves the lat bit set and checks
        }
    }

    public static class Extensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            //bool isEqual = Enumerable.SequenceEqual(data, result);
            return result;
        }

        public static string ToBitString(this byte[] target)
        {
            if (null == target)
            {
                throw new ArgumentNullException(nameof(target));
            }

            return String.Join(" ", target.Select(a => Convert.ToString(a, 2).PadLeft(8, '0')));
        }

        public static void Clear(this byte[] data)
        {
            Array.Clear(data, 0, data.Length);
        }

        public static void Fill(this byte[] data, byte val)
        {
            Array.Fill<byte>(data, val);
        }

        public static int Mod(this int x, int m)
        {
            return (x % m + m) % m;
        }
    }

    public enum ErrorChangeType
    {
        D_NoError,
        A_AllFrames,
        B_RandomFrames,
        C_SpecifiedFrames
    }
}
