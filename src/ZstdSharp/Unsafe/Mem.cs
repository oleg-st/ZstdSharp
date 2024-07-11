using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using BclUnsafe = System.Runtime.CompilerServices.Unsafe;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        /*-**************************************************************
        *  Memory I/O API
         * Can be rewritten with System.Runtime.CompilerServices.Unsafe
         * ReadUnaligned / WriteUnaligned
         * but unfortunately reduces inlining in .NET 5 or below
        *****************************************************************/
        /*=== Static platform detection ===*/
        public static bool MEM_32bits
        {
            [InlineMethod.Inline]
            get => sizeof(nint) == 4;
        }

        public static bool MEM_64bits
        {
            [InlineMethod.Inline]
            get => sizeof(nint) == 8;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /* default method, safe and standard.
           can sometimes prove slower */
        private static ushort MEM_read16(void* memPtr)
        {
            return BclUnsafe.ReadUnaligned<ushort>(memPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MEM_read32(void* memPtr)
        {
            return BclUnsafe.ReadUnaligned<uint>(memPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MEM_read64(void* memPtr)
        {
            return BclUnsafe.ReadUnaligned<ulong>(memPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint MEM_readST(void* memPtr)
        {
            return BclUnsafe.ReadUnaligned<nuint>(memPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_write16(void* memPtr, ushort value)
        {
            BclUnsafe.WriteUnaligned(memPtr, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_write64(void* memPtr, ulong value)
        {
            BclUnsafe.WriteUnaligned(memPtr, value);
        }

        /*=== Little endian r/w ===*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort MEM_readLE16(void* memPtr)
        {
            var num = BclUnsafe.ReadUnaligned<ushort>(memPtr);

            if (!BitConverter.IsLittleEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            return num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_writeLE16(void* memPtr, ushort val)
        {
            if (!BitConverter.IsLittleEndian)
            {
                val = BinaryPrimitives.ReverseEndianness(val);
            }

            BclUnsafe.WriteUnaligned(memPtr, val);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MEM_readLE24(void* memPtr) =>
            (uint)(MEM_readLE16(memPtr) + (((byte*)memPtr)[2] << 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_writeLE24(void* memPtr, uint val)
        {
            MEM_writeLE16(memPtr, (ushort)val);
            ((byte*)memPtr)[2] = (byte)(val >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MEM_readLE32(void* memPtr)
        {
            var num = BclUnsafe.ReadUnaligned<uint>(memPtr);

            if (!BitConverter.IsLittleEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            return num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_writeLE32(void* memPtr, uint val32)
        {
            if (!BitConverter.IsLittleEndian)
            {
                val32 = BinaryPrimitives.ReverseEndianness(val32);
            }

            BclUnsafe.WriteUnaligned(memPtr, val32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong MEM_readLE64(void* memPtr)
        {
            var num = BclUnsafe.ReadUnaligned<ulong>(memPtr);

            if (!BitConverter.IsLittleEndian)
            {
                num = BinaryPrimitives.ReverseEndianness(num);
            }

            return num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_writeLE64(void* memPtr, ulong val64)
        {
            if (!BitConverter.IsLittleEndian)
            {
                val64 = BinaryPrimitives.ReverseEndianness(val64);
            }

            BclUnsafe.WriteUnaligned(memPtr, val64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint MEM_readLEST(void* memPtr) => MEM_32bits ? MEM_readLE32(memPtr) : (nuint)MEM_readLE64(memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void MEM_writeLEST(void* memPtr, nuint val)
        {
            if (MEM_32bits)
            {
                MEM_writeLE32(memPtr, (uint)val);
            }
            else
            {
                MEM_writeLE64(memPtr, val);
            }
        }
    }
}
