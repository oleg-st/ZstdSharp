using System;

namespace ZstdSharp
{
    public unsafe partial struct ZSTD_optimal_t
    {
        public int price;

        public uint off;

        public uint mlen;

        public uint litlen;

        public fixed uint rep[3];
    }
}
