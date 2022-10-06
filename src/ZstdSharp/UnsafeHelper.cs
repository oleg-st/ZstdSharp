using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using InlineIL;
using static InlineIL.IL.Emit;
// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

[module: SkipLocalsInit]

namespace ZstdSharp
{
    public static unsafe class UnsafeHelper
    {
        public static void* PoisonMemory(void* destination, ulong size)
        {
            memset(destination, 0xCC, (uint) size);
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* malloc(uint size)
        {
#if DEBUG
            return PoisonMemory((void*)Marshal.AllocHGlobal((int)size), size);
#else
            return (void*) Marshal.AllocHGlobal((int) size);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* malloc(ulong size)
        {
#if DEBUG
            return PoisonMemory((void*) Marshal.AllocHGlobal((nint) size), size);
#else
            return (void*) Marshal.AllocHGlobal((nint) size);
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* calloc(ulong num, ulong size)
        {
            var total = num * size;
            assert(total <= uint.MaxValue);
            var destination = (void*) Marshal.AllocHGlobal((nint) total);
            memset(destination, 0, (uint) total);
            return destination;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static void memcpy(void* destination, void* source, uint size)
            => System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(destination, source, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static void memset(void* memPtr, byte val, uint size)
            => System.Runtime.CompilerServices.Unsafe.InitBlockUnaligned(memPtr, val, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void free(void* ptr)
        {
            Marshal.FreeHGlobal((IntPtr) ptr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T* GetArrayPointer<T>(T[] array) where T : unmanaged
        {
            var size = (uint) (sizeof(T) * array.Length);
            var destination = (T*) malloc(size);
            fixed (void* source = &array[0])
                System.Runtime.CompilerServices.Unsafe.CopyBlockUnaligned(destination, source, size);

            return destination;
        }

        [Conditional("DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void assert(bool condition, string message = null)
        {
            if (!condition)
                throw new ArgumentException(message ?? "assert failed");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void memmove(void* destination, void* source, ulong size)
            => Buffer.MemoryCopy(source, destination, size, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static void Prefetch0(void* p)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (System.Runtime.Intrinsics.X86.Sse.IsSupported)
            {
                System.Runtime.Intrinsics.X86.Sse.Prefetch0(p);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static void Prefetch1(void* p)
        {
#if NETCOREAPP3_0_OR_GREATER
            if (System.Runtime.Intrinsics.X86.Sse.IsSupported)
            {
                System.Runtime.Intrinsics.X86.Sse.Prefetch1(p);
            }
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int memcmp(void* buf1, void* buf2, ulong size)
        {
            var p1 = (byte*) buf1;
            var p2 = (byte*) buf2;

            while (size > 0)
            {
                var diff = *p1++ - *p2++;
                if (diff != 0)
                {
                    return diff;
                }

                size--;
            }

            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static void SkipInit<T>(out T value)
        {
            /* 
             * Can be rewritten with
             * System.Runtime.CompilerServices.Unsafe.SkipInit(out value);
             * in .NET 5+
             */
            Ret();
            throw IL.Unreachable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        public static TTo* RefToPointer<TFrom, TTo>(in TFrom t) where TTo : unmanaged
        {
            /*
             * Can be rewritten with
             * (TTo*)System.Runtime.CompilerServices.Unsafe.AsPointer(ref System.Runtime.CompilerServices.Unsafe.AsRef(t));
             * but unfortunately reduces inlining
             */
            Ldarg_0();
            return IL.ReturnPointer<TTo>();
        }
    }
}
