using System;

namespace ZstdSharp
{
    /*-***************************/
    /*  generic DTableDesc       */
    /*-***************************/
    public partial struct DTableDesc
    {
        public byte maxTableLog;

        public byte tableType;

        public byte tableLog;

        public byte reserved;
    }
}
