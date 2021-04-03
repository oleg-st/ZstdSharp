using System;

namespace ZstdSharp
{
    /* ======    Decompression    ====== */
    public partial struct FSE_DTableHeader
    {
        public ushort tableLog;

        public ushort fastMode;
    }
}
