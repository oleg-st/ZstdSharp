namespace ZstdSharp.Unsafe
{
    public unsafe struct ZSTD_customMem
    {
        public void* customAlloc;
        public void* customFree;
        public void* opaque;
    }
}