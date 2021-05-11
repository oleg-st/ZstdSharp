using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct seqState_t
    {
        public BIT_DStream_t DStream;

        public ZSTD_fseState stateLL;

        public ZSTD_fseState stateOffb;

        public ZSTD_fseState stateML;

        public _prevOffset_e__FixedBuffer prevOffset;

        public byte* prefixStart;

        public byte* dictEnd;

        public nuint pos;

        public unsafe partial struct _prevOffset_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;
            public nuint e2;

            public ref nuint this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get
                {
                    return ref AsSpan()[index];
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public Span<nuint> AsSpan() => MemoryMarshal.CreateSpan(ref e0, 3);

            public ref nuint this[uint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref AsSpan()[(int) index];
            }

            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref AsSpan()[(int) index];
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _prevOffset_e__FixedBuffer t)
            {
                fixed (nuint *pThis = &t.e0)
                {
                    return pThis;
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _prevOffset_e__FixedBuffer t, uint index)
            {
                fixed (nuint *pThis = &t.e0)
                {
                    return pThis + index;
                }
            }
        }
    }
}
