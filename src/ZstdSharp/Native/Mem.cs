using System;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace ZstdSharp
{
    public static unsafe partial class Methods
    {
        public static bool MEM_32bits => sizeof(nint) == 4;

        public static bool MEM_64bits => sizeof(nint) == 8;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ushort MEM_read16(void* memPtr) => Unsafe.ReadUnaligned<ushort>(memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_read32(void* memPtr) => Unsafe.ReadUnaligned<uint>(memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ulong MEM_read64(void* memPtr) => Unsafe.ReadUnaligned<ulong>(memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static nuint MEM_readST(void* memPtr) => Unsafe.ReadUnaligned<nuint>(memPtr);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_write64(void* memPtr, ulong value) => Unsafe.WriteUnaligned(memPtr, value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ushort MEM_readLE16(void* memPtr)
        {
            if (BitConverter.IsLittleEndian)
            {
                return Unsafe.ReadUnaligned<ushort>(memPtr);
            }

            return BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ushort>(memPtr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE16(void* memPtr, ushort val)
        {
            if (BitConverter.IsLittleEndian)
            {
                Unsafe.WriteUnaligned(memPtr, val);
            }
            else
            {
                Unsafe.WriteUnaligned(memPtr, BinaryPrimitives.ReverseEndianness(val));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_readLE24(void* memPtr) =>
            ((uint) (MEM_readLE16(memPtr) + (((byte*) (memPtr))[2] << 16)));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE24(void* memPtr, uint val)
        {
            MEM_writeLE16(memPtr, (ushort) (val));
            ((byte*) (memPtr))[2] = (byte) (val >> 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint MEM_readLE32(void* memPtr) =>
            BitConverter.IsLittleEndian ? Unsafe.ReadUnaligned<uint>(memPtr) : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(memPtr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE32(void* memPtr, uint val32) =>
            Unsafe.WriteUnaligned(memPtr, BitConverter.IsLittleEndian ? val32 : BinaryPrimitives.ReverseEndianness(val32));
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static ulong MEM_readLE64(void* memPtr) =>
            BitConverter.IsLittleEndian ? Unsafe.ReadUnaligned<ulong>(memPtr) : BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<ulong>(memPtr));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLE64(void* memPtr, ulong val64) =>
            Unsafe.WriteUnaligned(memPtr, BitConverter.IsLittleEndian ? val64 : BinaryPrimitives.ReverseEndianness(val64));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static nuint MEM_readLEST(void* memPtr)
        {
            if (BitConverter.IsLittleEndian)
            {
                return Unsafe.ReadUnaligned<nuint>(memPtr);
            }

            return MEM_32bits
                ? BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<uint>(memPtr))
                : (nuint) BinaryPrimitives.ReverseEndianness(Unsafe.ReadUnaligned<nuint>(memPtr));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static void MEM_writeLEST(void* memPtr, nuint val)
        {
            Unsafe.WriteUnaligned(memPtr, BitConverter.IsLittleEndian ? val : BinaryPrimitives.ReverseEndianness(val));
        }
    }
}
