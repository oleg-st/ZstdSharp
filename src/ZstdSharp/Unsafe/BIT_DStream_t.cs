namespace ZstdSharp.Unsafe
{
    public unsafe struct BIT_DStream_t
    {
        public nuint bitContainer;
        public uint bitsConsumed;
        public sbyte* ptr;
        public sbyte* start;
        public sbyte* limitPtr;
    }
}