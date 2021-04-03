using System;

namespace ZstdSharp
{
    public partial struct ZSTD_entropyCTables_t
    {
        public ZSTD_hufCTables_t huf;

        public ZSTD_fseCTables_t fse;
    }
}
