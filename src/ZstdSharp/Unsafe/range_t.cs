namespace ZstdSharp.Unsafe
{
    /* ====   Serial State   ==== */
    public unsafe struct range_t
    {
        public void* start;
        public nuint size;
        public range_t(void* start, nuint size)
        {
            this.start = start;
            this.size = size;
        }
    }
}