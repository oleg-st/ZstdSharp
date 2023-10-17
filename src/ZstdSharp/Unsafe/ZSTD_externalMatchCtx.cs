namespace ZstdSharp.Unsafe
{
    /* Context for block-level external matchfinder API */
    public unsafe struct ZSTD_externalMatchCtx
    {
        public void* mState;
        public void* mFinder;
        public ZSTD_Sequence* seqBuffer;
        public nuint seqBufferCapacity;
    }
}