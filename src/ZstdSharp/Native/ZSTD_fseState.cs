using System;

namespace ZstdSharp
{
    public unsafe partial struct ZSTD_fseState
    {
        public nuint state;

        public ZSTD_seqSymbol* table;
    }
}
