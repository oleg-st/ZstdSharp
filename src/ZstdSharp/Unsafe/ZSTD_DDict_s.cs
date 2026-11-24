namespace ZstdSharp.Unsafe
{
    /*-*******************************************************
     *  Types
     *********************************************************/
    public unsafe struct ZSTD_DDict_s
    {
        public void* dictBuffer;
        /* complete dictionary, including any header */
        public void* dictContent;
        public nuint dictSize;
        /* excluded from matchable history */
        public nuint dictHeaderSize;
        public ZSTD_entropyDTables_t entropy;
        public uint dictID;
        public uint entropyPresent;
        public ZSTD_customMem cMem;
    }
}