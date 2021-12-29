using InlineIL;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static InlineIL.IL.Emit;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct HUF_CStream_t
    {
        public _bitContainer_e__FixedBuffer bitContainer;

        public _bitPos_e__FixedBuffer bitPos;

        public byte* startPtr;

        public byte* ptr;

        public byte* endPtr;

        public unsafe partial struct _bitContainer_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;

            public ref nuint this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + (uint)index);
            }

            public ref nuint this[uint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + index);
            }

            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + (uint)index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _bitContainer_e__FixedBuffer t)
            {
                Ldarg_0();
                Ldflda(new FieldRef(typeof(_bitContainer_e__FixedBuffer), nameof(e0)));
                return IL.ReturnPointer<nuint>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _bitContainer_e__FixedBuffer t, uint index)
            {
                Ldarg_0();
                Ldflda(new FieldRef(typeof(_bitContainer_e__FixedBuffer), nameof(e0)));
                Ldarg_1();
                Conv_I();
                Sizeof<nuint>();
                Conv_I();
                Mul();
                Add();
                return IL.ReturnPointer<nuint>();
            }
        }

        public unsafe partial struct _bitPos_e__FixedBuffer
        {
            public nuint e0;
            public nuint e1;

            public ref nuint this[int index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + (uint)index);
            }

            public ref nuint this[uint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + index);
            }

            public ref nuint this[nuint index]
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                [InlineMethod.Inline]
                get => ref *(this + (uint)index);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static implicit operator nuint*(in _bitPos_e__FixedBuffer t)
            {
                Ldarg_0();
                Ldflda(new FieldRef(typeof(_bitPos_e__FixedBuffer), nameof(e0)));
                return IL.ReturnPointer<nuint>();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            [InlineMethod.Inline]
            public static nuint* operator +(in _bitPos_e__FixedBuffer t, uint index)
            {
                Ldarg_0();
                Ldflda(new FieldRef(typeof(_bitPos_e__FixedBuffer), nameof(e0)));
                Ldarg_1();
                Conv_I();
                Sizeof<nuint>();
                Conv_I();
                Mul();
                Add();
                return IL.ReturnPointer<nuint>();
            }
        }
    }
}
