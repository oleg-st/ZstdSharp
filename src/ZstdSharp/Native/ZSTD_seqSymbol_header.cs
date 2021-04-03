using System;

namespace ZstdSharp
{
    /*-*******************************************************
     *  Decompression types
     *********************************************************/
    public partial struct ZSTD_seqSymbol_header
    {
        public uint fastMode;

        public uint tableLog;
    }
}
