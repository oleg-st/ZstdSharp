using System;

namespace ZstdSharp
{
    /**
     * Struct used for the dictionary selection function.
     */
    public unsafe partial struct COVER_dictSelection
    {
        public byte* dictContent;

        public nuint dictSize;

        public nuint totalCompressedSize;
    }
}
