using System.Text;

namespace MsgTool
{
    public sealed class StringPool
    {
        private byte[]? _encrypted;
        private readonly byte[] _unencrypted;
        private readonly Dictionary<int, string> _pool;
        private Dictionary<string, int>? _reversePool;

        public StringPool(byte[] data, bool encrypted)
        {
            if (encrypted)
            {
                _encrypted = data;
                _unencrypted = Helper.Decrypt(data);
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
                _encrypted ??= Helper.Encrypt(_unencrypted);
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

        private static string WcharPoolToStrPool(byte[] wcharPool)
        {
            if (wcharPool.Length % 2 != 0)
                throw new ArgumentException("Wchar pool should have even size");
            string stringPool = Encoding.Unicode.GetString(wcharPool);
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
    }
}
