namespace ZstdSharp.Unsafe
{
    public unsafe struct ZSTD_customMem
    {
        public delegate* managed<void*, nuint, void*> customAlloc;
        public delegate* managed<void*, void*, void> customFree;
        public void* opaque;
    }
}