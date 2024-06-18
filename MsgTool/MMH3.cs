using System.Text;

namespace MsgTool
{
    public static class MMH3
    {
        public static uint Hash32(byte[] data, int seed = 0)
        {
            const uint c1 = 0xcc9e2d51;
            const uint c2 = 0x1b873593;

            int length = data.Length;
            int nblocks = length / 4;

            uint h1 = (uint)seed;

            // Body
            for (int i = 0; i < nblocks; i++)
            {
                int i4 = i * 4;
                uint k1 = (uint)(data[i4] | (data[i4 + 1] << 8) | (data[i4 + 2] << 16) | (data[i4 + 3] << 24));

                k1 *= c1;
                k1 = Rotl32(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = Rotl32(h1, 13);
                h1 = h1 * 5 + 0xe6546b64;
            }

            // Tail
            uint k1_tail = 0;

            int tailIndex = nblocks * 4;
            switch (length & 3)
            {
                case 3:
                    k1_tail ^= (uint)data[tailIndex + 2] << 16;
                    goto case 2;
                case 2:
                    k1_tail ^= (uint)data[tailIndex + 1] << 8;
                    goto case 1;
                case 1:
                    k1_tail ^= data[tailIndex];
                    k1_tail *= c1;
                    k1_tail = Rotl32(k1_tail, 15);
                    k1_tail *= c2;
                    h1 ^= k1_tail;
                    break;
            }

            // Finalization
            h1 ^= (uint)length;
            h1 = FMix(h1);

            return h1;
        }

        private static uint Rotl32(uint x, byte r)
        {
            return (x << r) | (x >> (32 - r));
        }

        private static uint FMix(uint h)
        {
            h ^= h >> 16;
            h *= 0x85ebca6b;
            h ^= h >> 13;
            h *= 0xc2b2ae35;
            h ^= h >> 16;

            return h;
        }

        public static uint Hash32(string data, int seed = 0)
        {
            return Hash32(Encoding.UTF8.GetBytes(data), seed);
        }
    }
}
