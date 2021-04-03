using System;

namespace ZstdSharp
{
    public unsafe partial struct ldmMatchCandidate_t
    {
        public byte* split;

        public uint hash;

        public uint checksum;

        public ldmEntry_t* bucket;
    }
}
