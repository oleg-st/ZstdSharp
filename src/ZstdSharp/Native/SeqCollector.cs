using System;

namespace ZstdSharp
{
    public unsafe partial struct SeqCollector
    {
        public int collectSequences;

        public ZSTD_Sequence* seqStart;

        public nuint seqIndex;

        public nuint maxSequences;
    }
}
