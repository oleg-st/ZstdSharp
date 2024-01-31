namespace ZstdSharp.Unsafe
{
    public struct rsyncState_t
    {
        public ulong hash;
        public ulong hitMask;
        public ulong primePower;
    }
}