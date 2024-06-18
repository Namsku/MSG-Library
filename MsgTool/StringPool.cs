using System.Text;

namespace MsgTool
{
    public sealed class StringPool
    {
        private static readonly byte[] g_key = [0xCF, 0xCE, 0xFB, 0xF8, 0xEC, 0x0A, 0x33, 0x66, 0x93, 0xA9, 0x1D, 0x93, 0x50, 0x39, 0x5F, 0x09];

        private byte[]? _encrypted;
        private readonly byte[] _unencrypted;
        private readonly Dictionary<int, string> _pool;
        private Dictionary<string, int>? _reversePool;

        public StringPool(byte[] data, bool encrypted)
        {
            if (encrypted)
            {
                _encrypted = data;
                _unencrypted = Decrypt(data);
            }
            else
            {
                _unencrypted = data;
            }
            _pool = WcharPoolToStrDict(_unencrypted);
        }

        public StringPool(IEnumerable<string> strings)
        {
            _unencrypted = Build(strings);
            _pool = WcharPoolToStrDict(_unencrypted);
        }

        public int Count => _pool.Count;

        public ReadOnlySpan<byte> Encryped
        {
            get
            {
                _encrypted ??= Encrypt(_unencrypted);
                return _encrypted;
            }
        }

        public ReadOnlySpan<byte> Unencryped => _unencrypted;

        public string Find(ulong offset) => Find((int)offset);
        public string Find(long offset) => Find((int)offset);
        public string Find(int offset) => _pool[offset];

        public int FindOffset(string s)
        {
            _reversePool ??= _pool.ToDictionary(x => x.Value, x => x.Key);
            return _reversePool[s];
        }

        private static Dictionary<int, string> WcharPoolToStrDict(byte[] wcharPool)
        {
            if (wcharPool.Length == 0)
                return new Dictionary<int, string>();

            var stringPool = WcharPoolToStrPool(wcharPool);
            var stringDict = new Dictionary<int, string>();
            var startPointer = 0;
            for (var i = 0; i < stringPool.Length; i++)
            {
                if (stringPool[i] == '\x00')
                {
                    stringDict[startPointer * 2] = stringPool.Substring(startPointer, i - startPointer);
                    startPointer = i + 1;
                }
            }
            return stringDict;
        }

        private static string WcharPoolToStrPool(byte[] wcharPool)
        {
            if (wcharPool.Length % 2 != 0)
                throw new ArgumentException("Wchar pool should have even size");
            var stringPool = Encoding.Unicode.GetString(wcharPool);
            if (stringPool[^1] != '\x00')
                throw new ArgumentException("Ending wchar not null");
            return stringPool;
        }

        private static byte[] Build(IEnumerable<string> strings)
        {
            var strOffsetDict = CalcStrPoolOffsets(strings);
            var result = new MemoryStream();
            foreach (var str in strOffsetDict.Keys)
            {
                result.Write(ToWcharBytes(str));
            }
            return result.ToArray();
        }

        private static Dictionary<string, int> CalcStrPoolOffsets(IEnumerable<string> strings)
        {
            var dict = new Dictionary<string, int>();
            var offset = 0;
            foreach (var s in strings)
            {
                dict[s] = offset;
                offset += s.Length * 2 + 2;
            }
            return dict;
        }

        private static byte[] ToWcharBytes(string input)
        {
            return Encoding.Unicode.GetBytes(input + '\x00');
        }

        private static byte[] Decrypt(byte[] rawBytes)
        {
            var rawData = new byte[rawBytes.Length];
            Array.Copy(rawBytes, rawData, rawBytes.Length);
            byte prev = 0;
            for (int i = 0; i < rawData.Length; i++)
            {
                var cur = rawData[i];
                rawData[i] = (byte)(cur ^ prev ^ g_key[i & 0xF]);
                prev = cur;
            }
            return rawData;
        }

        private static byte[] Encrypt(byte[] rawBytes)
        {
            var rawData = new byte[rawBytes.Length];
            Array.Copy(rawBytes, rawData, rawBytes.Length);
            byte prev = 0;
            for (int i = 0; i < rawData.Length; i++)
            {
                var cur = rawData[i];
                rawData[i] = (byte)(cur ^ prev ^ g_key[i & 0xF]);
                prev = rawData[i];
            }
            return rawData;
        }
    }
}
