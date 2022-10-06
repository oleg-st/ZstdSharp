namespace ZstdSharp.Unsafe
{
    public unsafe struct FSE_DecompressWksp
    {
        public fixed short ncount[256];
        /* Dynamically sized */
        public fixed uint dtable[1];
    }
}