using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace ZstdSharp
{
    public static unsafe partial class Methods
    {
    	/*-**************************************************************
        *  Memory I/O API
        *****************************************************************/
        /*=== Static platform detection ===*/
        public static bool MEM_32bits => sizeof(nint) == 4;

        public static bool MEM_64bits => sizeof(nint) == 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        /* default method, safe and standard.
           can sometimes prove slower */
        [InlineMethod.Inline]
        private static ushort MEM_read16(void* memPtr) => *(ushort*) memPtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_read32(void* memPtr) => *(uint*) memPtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ulong MEM_read64(void* memPtr) => *(ulong*) memPtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static nuint MEM_readST(void* memPtr) => *(nuint*) memPtr;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_write64(void* memPtr, ulong value) => *(ulong*) memPtr = value;

        /*=== Little endian r/w ===*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ushort MEM_readLE16(void* memPtr) =>
            BitConverter.IsLittleEndian
                ? *(ushort*) memPtr
                : BinaryPrimitives.ReverseEndianness(*(ushort*) memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE16(void* memPtr, ushort val) => 
            (*(ushort*) memPtr) = BitConverter.IsLittleEndian ? val : BinaryPrimitives.ReverseEndianness(val);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_readLE24(void* memPtr) =>
            (uint) (MEM_readLE16(memPtr) + (((byte*) memPtr)[2] << 16));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE24(void* memPtr, uint val)
        {
            MEM_writeLE16(memPtr, (ushort) val);
            ((byte*) memPtr)[2] = (byte) (val >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_readLE32(void* memPtr) =>
            BitConverter.IsLittleEndian ? *(uint*) memPtr : BinaryPrimitives.ReverseEndianness(*(uint*) memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE32(void* memPtr, uint val32) =>
            *(uint*) memPtr = BitConverter.IsLittleEndian ? val32 : BinaryPrimitives.ReverseEndianness(val32);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ulong MEM_readLE64(void* memPtr) =>
            BitConverter.IsLittleEndian
                ? *(ulong*) memPtr
                : BinaryPrimitives.ReverseEndianness(*(ulong*) memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE64(void* memPtr, ulong val64) =>
            *(ulong*) memPtr = BitConverter.IsLittleEndian ? val64 : BinaryPrimitives.ReverseEndianness(val64);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static nuint MEM_readLEST(void* memPtr) =>
            BitConverter.IsLittleEndian
                ? *(nuint*) memPtr
                : (nuint) BinaryPrimitives.ReverseEndianness(*(nuint*) memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLEST(void* memPtr, nuint val) => 
            *(nuint*)memPtr = BitConverter.IsLittleEndian ? val : (nuint)BinaryPrimitives.ReverseEndianness(val);
    }
}
