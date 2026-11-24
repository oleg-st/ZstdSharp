namespace ZstdSharp.Unsafe
{
    public struct ZSTD_SequencePosition
    {
        /* Index in array of ZSTD_Sequence */
        public uint idx;
        /* Position within sequence at idx. The sum of its lengths may exceed U32. */
        public ulong posInSequence;
        /* Number of bytes given by sequences provided so far */
        public nuint posInSrc;
    }
}