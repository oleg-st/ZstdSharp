namespace ZstdSharp.Unsafe
{
    public struct syncPoint_t
    {
        /* The number of bytes to load from the input. */
        public nuint toLoad;
        /* Boolean declaring if we must flush because we found a synchronization point. */
        public int flush;
    }
}