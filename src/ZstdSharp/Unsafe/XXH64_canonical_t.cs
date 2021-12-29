using System;

namespace ZstdSharp.Unsafe
{
    /*******   Canonical representation   *******/
    public unsafe partial struct XXH64_canonical_t
    {
        public fixed byte digest[8];
    }
}
