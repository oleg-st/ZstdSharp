namespace ZstdSharp.Unsafe
{
    public unsafe struct ZSTD_customMem
    {
        public void* customAlloc;
        public void* customFree;
        public void* opaque;
        public ZSTD_customMem(void* customAlloc = default, void* customFree = default, void* opaque = default)
        {
            this.customAlloc = customAlloc;
            this.customFree = customFree;
            this.opaque = opaque;
        }
    }
}