using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public unsafe struct HUF_CStream_t
    {
        public _bitContainer_e__FixedBuffer bitContainer;
        public _bitPos_e__FixedBuffer bitPos;
        public byte* startPtr;
        public byte* ptr;
        public byte* endPtr;
        public unsafe struct _bitContainer_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;
            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_bitContainer_e__FixedBuffer, nuint>(this) + index);
            }

            public ref nuint this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_bitContainer_e__FixedBuffer, nuint>(this) + index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _bitContainer_e__FixedBuffer t) => RefToPointer<_bitContainer_e__FixedBuffer, nuint>(t);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _bitContainer_e__FixedBuffer t, nuint index) => RefToPointer<_bitContainer_e__FixedBuffer, nuint>(t) + index;
        }

        public unsafe struct _bitPos_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;
            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_bitPos_e__FixedBuffer, nuint>(this) + index);
            }

            public ref nuint this[nint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(RefToPointer<_bitPos_e__FixedBuffer, nuint>(this) + index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _bitPos_e__FixedBuffer t) => RefToPointer<_bitPos_e__FixedBuffer, nuint>(t);
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _bitPos_e__FixedBuffer t, nuint index) => RefToPointer<_bitPos_e__FixedBuffer, nuint>(t) + index;
        }
    }
}