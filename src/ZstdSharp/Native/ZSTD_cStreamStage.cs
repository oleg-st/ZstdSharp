using System;

namespace ZstdSharp
{
    public enum ZSTD_cStreamStage
    {
        zcss_init = 0,
        zcss_load,
        zcss_flush,
    }
}
