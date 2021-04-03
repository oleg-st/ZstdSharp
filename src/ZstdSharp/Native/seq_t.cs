using System;

namespace ZstdSharp
{
    public unsafe partial struct seq_t
    {
        public nuint litLength;

        public nuint matchLength;

        public nuint offset;

        public byte* match;
    }
}
