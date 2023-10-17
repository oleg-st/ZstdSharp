namespace ZstdSharp.Unsafe
{
    public struct ZSTD_seqSymbol
    {
        public ushort nextState;
        public byte nbAdditionalBits;
        public byte nbBits;
        public uint baseValue;
        public ZSTD_seqSymbol(ushort nextState = default, byte nbAdditionalBits = default, byte nbBits = default, uint baseValue = default)
        {
            this.nextState = nextState;
            this.nbAdditionalBits = nbAdditionalBits;
            this.nbBits = nbBits;
            this.baseValue = baseValue;
        }
    }
}