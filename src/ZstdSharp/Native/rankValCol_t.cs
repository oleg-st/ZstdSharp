using System;
using System.Runtime.CompilerServices;

namespace ZstdSharp
{
    public unsafe partial struct rankValCol_t
    {
        public fixed uint Body[13];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static implicit operator uint*(in rankValCol_t t)
        {
            fixed (uint* pThis = &t.Body[0])
            {
                return pThis;
            }
        }
    }
}
