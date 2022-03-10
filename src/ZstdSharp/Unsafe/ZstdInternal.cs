using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        public static readonly uint* repStartValue = GetArrayPointer(new uint[3]
        {
            1,
            4,
            8,
        });

        public static readonly nuint* ZSTD_fcs_fieldSize = GetArrayPointer(new nuint[4]
        {
            0,
            2,
            4,
            8,
        });

        public static readonly nuint* ZSTD_did_fieldSize = GetArrayPointer(new nuint[4]
        {
            0,
            1,
            2,
            4,
        });

        public const nuint ZSTD_blockHeaderSize = 3;

        public static readonly byte* LL_bits = GetArrayPointer(new byte[36]
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            1,
            1,
            1,
            1,
            2,
            2,
            3,
            3,
            4,
            6,
            7,
            8,
            9,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
        });

        public static readonly short* LL_defaultNorm = GetArrayPointer(new short[36]
        {
            4,
            3,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            1,
            1,
            1,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            2,
            3,
            2,
            1,
            1,
            1,
            1,
            1,
            -1,
            -1,
            -1,
            -1,
        });

        public const uint LL_defaultNormLog = 6;

        public static readonly byte* ML_bits = GetArrayPointer(new byte[53]
        {
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            1,
            1,
            1,
            1,
            2,
            2,
            3,
            3,
            4,
            4,
            5,
            7,
            8,
            9,
            10,
            11,
            12,
            13,
            14,
            15,
            16,
        });

        public static readonly short* ML_defaultNorm = GetArrayPointer(new short[53]
        {
            1,
            4,
            3,
            2,
            2,
            2,
            2,
            2,
            2,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
            -1,
        });

        public const uint ML_defaultNormLog = 6;

        public static readonly short* OF_defaultNorm = GetArrayPointer(new short[29]
        {
            1,
            1,
            1,
            1,
            1,
            1,
            2,
            2,
            2,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            1,
            -1,
            -1,
            -1,
            -1,
            -1,
        });

        public const uint OF_defaultNormLog = 5;

        /*-*******************************************
        *  Shared functions to include for inlining
        *********************************************/
        private static void ZSTD_copy8(void* dst, void* src)
        {
            memcpy((dst), (src), (8));
        }

        /* Need to use memmove here since the literal buffer can now be located within
           the dst buffer. In circumstances where the op "catches up" to where the
           literal buffer is, there can be partial overlaps in this call on the final
           copy if the literal is being shifted by less than 16 bytes. */
        private static void ZSTD_copy16(void* dst, void* src)
        {
            memcpy((dst), (src), (16));
        }

        /*! ZSTD_wildcopy() :
         *  Custom version of ZSTD_memcpy(), can over read/write up to WILDCOPY_OVERLENGTH bytes (if length==0)
         *  @param ovtype controls the overlap detection
         *         - ZSTD_no_overlap: The source and destination are guaranteed to be at least WILDCOPY_VECLEN bytes apart.
         *         - ZSTD_overlap_src_before_dst: The src and dst may overlap, but they MUST be at least 8 bytes apart.
         *           The src buffer must be before the dst buffer.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_wildcopy(void* dst, void* src, nint length, ZSTD_overlap_e ovtype)
        {
            nint diff = (nint)((byte*)(dst) - (byte*)(src));
            byte* ip = (byte*)(src);
            byte* op = (byte*)(dst);
            byte* oend = op + length;

            if (ovtype == ZSTD_overlap_e.ZSTD_overlap_src_before_dst && diff < 16)
            {
                do
                {

                    {
                        ZSTD_copy8((void*)op, (void*)ip);
                        op += 8;
                        ip += 8;
                    }
                }
                while (op < oend);

            }
            else
            {
                assert(diff >= 16 || diff <= -16);
                ZSTD_copy16((void*)op, (void*)ip);
                if (16 >= length)
                {
                    return;
                }

                op += 16;
                ip += 16;
                do
                {

                    {
                        ZSTD_copy16((void*)op, (void*)ip);
                        op += 16;
                        ip += 16;
                    }


                    {
                        ZSTD_copy16((void*)op, (void*)ip);
                        op += 16;
                        ip += 16;
                    }

                }
                while (op < oend);

            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_limitCopy(void* dst, nuint dstCapacity, void* src, nuint srcSize)
        {
            nuint length = ((dstCapacity) < (srcSize) ? (dstCapacity) : (srcSize));

            if (length > 0)
            {
                memcpy((dst), (src), (length));
            }

            return length;
        }

        /**
         * Returns the ZSTD_sequenceLength for the given sequences. It handles the decoding of long sequences
         * indicated by longLengthPos and longLengthType, and adds MINMATCH back to matchLength.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ZSTD_sequenceLength ZSTD_getSequenceLength(seqStore_t* seqStore, seqDef_s* seq)
        {
            ZSTD_sequenceLength seqLen;

            seqLen.litLength = seq->litLength;
            seqLen.matchLength = (uint)(seq->mlBase + 3);
            if (seqStore->longLengthPos == (uint)(seq - seqStore->sequencesStart))
            {
                if (seqStore->longLengthType == ZSTD_longLengthType_e.ZSTD_llt_literalLength)
                {
                    seqLen.litLength += 0xFFFF;
                }

                if (seqStore->longLengthType == ZSTD_longLengthType_e.ZSTD_llt_matchLength)
                {
                    seqLen.matchLength += 0xFFFF;
                }
            }

            return seqLen;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint ZSTD_highbit32(uint val)
        {
            assert(val != 0);

            {
                return (uint)BitOperations.Log2(val);
            }
        }

        /**
         * Counts the number of trailing zeros of a `size_t`.
         * Most compilers should support CTZ as a builtin. A backup
         * implementation is provided if the builtin isn't supported, but
         * it may not be terribly efficient.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ZSTD_countTrailingZeros(nuint val)
        {
            assert(val != 0);
            return (uint)BitOperations.TrailingZeroCount(val);
        }
    }
}
