using System;

namespace ZstdSharp.Unsafe
{
    /*********************************
    *  Compression internals structs *
    *********************************/
    public partial struct ZSTD_match_t
    {
        /* Offset sumtype code for the match, using ZSTD_storeSeq() format */
        public uint off;

        /* Raw length of match */
        public uint len;
    }
}
