using System;

namespace ZstdSharp
{
    public enum ZSTD_compressionStage_e
    {
        ZSTDcs_created = 0,
        ZSTDcs_init,
        ZSTDcs_ongoing,
        ZSTDcs_ending,
    }
}
