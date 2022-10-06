using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public unsafe struct seqState_t
    {
        public BIT_DStream_t DStream;
        public ZSTD_fseState stateLL;
        public ZSTD_fseState stateOffb;
        public ZSTD_fseState stateML;
        public _prevOffset_e__FixedBuffer prevOffset;
        public unsafe struct _prevOffset_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;
            public nuint e2;
            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_prevOffset_e__FixedBuffer, nuint>(this) + index);
            }

            public ref nuint this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_prevOffset_e__FixedBuffer, nuint>(this) + index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _prevOffset_e__FixedBuffer t) => RefToPointer<_prevOffset_e__FixedBuffer, nuint>(t);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _prevOffset_e__FixedBuffer t, nuint index) => RefToPointer<_prevOffset_e__FixedBuffer, nuint>(t) + index;
        }
    }
}