using System;

namespace ZstdSharp.Unsafe
{
    public enum ZSTD_litLocation_e
    {
        ZSTD_not_in_dst = 0,
        ZSTD_in_dst = 1,
        ZSTD_split = 2,
    }
}
