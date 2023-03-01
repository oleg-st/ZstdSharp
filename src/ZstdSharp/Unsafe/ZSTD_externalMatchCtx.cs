namespace ZstdSharp.Unsafe
{
    /* Context for block-level external matchfinder API */
    public unsafe struct ZSTD_externalMatchCtx
    {
        public void* mState;
        public delegate* managed<void*, ZSTD_Sequence*, nuint, void*, nuint, void*, nuint, int, nuint, nuint> mFinder;
        public ZSTD_Sequence* seqBuffer;
        public nuint seqBufferCapacity;
    }
}