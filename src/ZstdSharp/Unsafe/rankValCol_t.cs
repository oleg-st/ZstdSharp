using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public unsafe struct rankValCol_t
    {
        public fixed uint Body[13];
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static implicit operator uint*(in rankValCol_t t) => RefToPointer<rankValCol_t, uint>(t);
    }
}