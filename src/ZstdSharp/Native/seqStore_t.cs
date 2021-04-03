using System;

namespace ZstdSharp
{
    public unsafe partial struct seqStore_t
    {
        public seqDef_s* sequencesStart;

        /* ptr to end of sequences */
        public seqDef_s* sequences;

        public byte* litStart;

        /* ptr to end of literals */
        public byte* lit;

        public byte* llCode;

        public byte* mlCode;

        public byte* ofCode;

        public nuint maxNbSeq;

        public nuint maxNbLit;

        /* 0 == no longLength; 1 == Represent the long literal; 2 == Represent the long match; */
        public uint longLengthID;

        /* Index of the sequence to apply long length modification to */
        public uint longLengthPos;
    }
}
