using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MsgTool
{
    public static class Helper
    {
        public static readonly List<int> KEY = new List<int> { 0xCF, 0xCE, 0xFB, 0xF8, 0xEC, 0x0A, 0x33, 0x66, 0x93, 0xA9, 0x1D, 0x93, 0x50, 0x39, 0x5F, 0x09 };
        public static string SeekString(int offset, Dictionary<int, string> stringDict)
        {
            if (stringDict.Count == 0)
                throw new ArgumentException("No string pool but seeking string");
            if (!stringDict.ContainsKey(offset))
                throw new ArgumentException($"Seeking target not at string pool {offset}");
            return stringDict[offset];
        }

        public static byte[] Decrypt(byte[] rawBytes)
        {
            byte[] rawData = new byte[rawBytes.Length];
            Array.Copy(rawBytes, rawData, rawBytes.Length);
            byte prev = 0;
            for (int i = 0; i < rawData.Length; i++)
            {
                byte cur = rawData[i];
                rawData[i] = (byte)(cur ^ prev ^ KEY[i & 0xF]);
                prev = cur;
            }
            return rawData;
        }

        public static byte[] Encrypt(byte[] rawBytes)
        {
            byte[] rawData = new byte[rawBytes.Length];
            Array.Copy(rawBytes, rawData, rawBytes.Length);
            byte prev = 0;
            for (int i = 0; i < rawData.Length; i++)
            {
                byte cur = rawData[i];
                rawData[i] = (byte)(cur ^ prev ^ KEY[i & 0xF]);
                prev = rawData[i];
            }
            return rawData;
        }

        public static Dictionary<int, string> WcharPoolToStrDict(byte[] wcharPool)
        {
            if (wcharPool.Length == 0)
                return new Dictionary<int, string>();

            string stringPool = WcharPoolToStrPool(wcharPool);

            var stringDict = new Dictionary<int, string>();
            int startPointer = 0;
            for (int i = 0; i < stringPool.Length; i++)
            {
                if (stringPool[i] == '\x00')
                {
                    stringDict[startPointer * 2] = stringPool.Substring(startPointer, i - startPointer);
                    startPointer = i + 1;
                }
            }
            return stringDict;
        }

        public static string WcharPoolToStrPool(byte[] wcharPool)
        {
            if (wcharPool.Length % 2 != 0)
                throw new ArgumentException("Wchar pool should have even size");
            string stringPool = Encoding.Unicode.GetString(wcharPool);
            if (stringPool[^1] != '\x00')
                throw new ArgumentException("Ending wchar not null");
            return stringPool;
        }

        public static string ForceWindowsLineBreak(string input)
        {
            return input.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n");
        }

        public static Dictionary<string, int> CalcStrPoolOffsets(HashSet<string> stringPoolSet)
        {
            var dict = new Dictionary<string, int>();
            var offset = 0;
            foreach (var str in stringPoolSet)
            {
                dict[str] = offset;
                offset += str.Length * 2 + 2;
            }
            return dict;
        }

        public static byte[] ToWcharBytes(string input)
        {
            return Encoding.Unicode.GetBytes(input + '\x00');
        }
    }
}
