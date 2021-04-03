using System;

namespace ZstdSharp
{
    /* *****************************************
    *  FSE symbol decompression API
    *******************************************/
    public unsafe partial struct FSE_DState_t
    {
        public nuint state;

        /* precise table may vary, depending on U16 */
        public void* table;
    }
}
