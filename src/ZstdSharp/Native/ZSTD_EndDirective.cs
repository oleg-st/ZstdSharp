using System;

namespace ZstdSharp
{
    public enum ZSTD_EndDirective
    {
        ZSTD_e_continue = 0,
        ZSTD_e_flush = 1,
        ZSTD_e_end = 2,
    }
}
