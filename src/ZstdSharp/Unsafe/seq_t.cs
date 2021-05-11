using System;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct seq_t
    {
        public nuint litLength;

        public nuint matchLength;

        public nuint offset;

        public byte* match;
    }
}
