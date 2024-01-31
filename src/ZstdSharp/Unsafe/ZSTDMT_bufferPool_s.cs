namespace ZstdSharp.Unsafe
{
    public unsafe struct ZSTDMT_bufferPool_s
    {
        public void* poolMutex;
        public nuint bufferSize;
        public uint totalBuffers;
        public uint nbBuffers;
        public ZSTD_customMem cMem;
        /* variable size */
        public _bTable_e__FixedBuffer bTable;
        public unsafe struct _bTable_e__FixedBuffer
        {
            public buffer_s e0;
        }
    }
}