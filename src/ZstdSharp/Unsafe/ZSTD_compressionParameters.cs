namespace ZstdSharp.Unsafe
{
    public struct ZSTD_compressionParameters
    {
        /**< largest match distance : larger == more compression, more memory needed during decompression */
        public uint windowLog;
        /**< fully searched segment : larger == more compression, slower, more memory (useless for fast) */
        public uint chainLog;
        /**< dispatch table : larger == faster, more memory */
        public uint hashLog;
        /**< nb of searches : larger == more compression, slower */
        public uint searchLog;
        /**< match length searched : larger == faster decompression, sometimes less compression */
        public uint minMatch;
        /**< acceptable match size for optimal parser (only) : larger == more compression, slower */
        public uint targetLength;
        /**< see ZSTD_strategy definition above */
        public ZSTD_strategy strategy;
        public ZSTD_compressionParameters(uint windowLog = default, uint chainLog = default, uint hashLog = default, uint searchLog = default, uint minMatch = default, uint targetLength = default, ZSTD_strategy strategy = default)
        {
            this.windowLog = windowLog;
            this.chainLog = chainLog;
            this.hashLog = hashLog;
            this.searchLog = searchLog;
            this.minMatch = minMatch;
            this.targetLength = targetLength;
            this.strategy = strategy;
        }
    }
}