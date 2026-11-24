namespace ZstdSharp.Unsafe
{
    /* ========================================================
     * ===  Private interface, for use by ZSTD_compress.c   ===
     * ===  Not exposed in libzstd. Never invoke directly   ===
     * ======================================================== */
    public unsafe struct Range
    {
        public void* start;
        public nuint size;
        public Range(void* start, nuint size)
        {
            this.start = start;
            this.size = size;
        }
    }
}