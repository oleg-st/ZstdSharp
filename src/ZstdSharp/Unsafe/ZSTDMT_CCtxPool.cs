namespace ZstdSharp.Unsafe
{
    /* =====   CCtx Pool   ===== */
    /* a single CCtx Pool can be invoked from multiple threads in parallel */
    public unsafe struct ZSTDMT_CCtxPool
    {
        public void* poolMutex;
        public int totalCCtx;
        public int availCCtx;
        public ZSTD_customMem cMem;
        /* variable size */
        public _cctx_e__FixedBuffer cctx;
        public unsafe struct _cctx_e__FixedBuffer
        {
            public ZSTD_CCtx_s* e0;
        }
    }
}