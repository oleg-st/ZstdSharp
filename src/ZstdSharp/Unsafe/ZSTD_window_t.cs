using System;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct ZSTD_window_t
    {
        /* next block here to continue on current prefix */
        public byte* nextSrc;

        /* All regular indexes relative to this position */
        public byte* @base;

        /* extDict indexes relative to this position */
        public byte* dictBase;

        /* below that point, need extDict */
        public uint dictLimit;

        /* below that point, no more valid data */
        public uint lowLimit;
    }
}
