using System;

namespace ZstdSharp
{
    /*-*******************************************
    *  Private declarations
    *********************************************/
    public partial struct seqDef_s
    {
        /* Offset code of the sequence */
        public uint offset;

        public ushort litLength;

        public ushort matchLength;
    }
}
