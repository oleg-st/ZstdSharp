namespace ZstdSharp.Unsafe
{
    /* ------------------------------------------ */
    /* =====   Multi-threaded compression   ===== */
    /* ------------------------------------------ */
    public struct inBuff_t
    {
        /* read-only non-owned prefix buffer */
        public range_t prefix;
        public buffer_s buffer;
        public nuint filled;
    }
}