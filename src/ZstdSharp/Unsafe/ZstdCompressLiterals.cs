using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        public static nuint ZSTD_noCompressLiterals(void* dst, nuint dstCapacity, void* src, nuint srcSize)
        {
            byte* ostart = (byte*)dst;
            uint flSize = (uint)(1 + (srcSize > 31 ? 1 : 0) + (srcSize > 4095 ? 1 : 0));
            if (srcSize + flSize > dstCapacity)
            {
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            }

            switch (flSize)
            {
                case 1:
                    ostart[0] = (byte)((uint)symbolEncodingType_e.set_basic + (srcSize << 3));
                    break;
                case 2:
                    MEM_writeLE16(ostart, (ushort)((uint)symbolEncodingType_e.set_basic + (1 << 2) + (srcSize << 4)));
                    break;
                case 3:
                    MEM_writeLE32(ostart, (uint)((uint)symbolEncodingType_e.set_basic + (3 << 2) + (srcSize << 4)));
                    break;
                default:
                    assert(0 != 0);
                    break;
            }

            memcpy(ostart + flSize, src, (uint)srcSize);
            return srcSize + flSize;
        }

        public static nuint ZSTD_compressRleLiteralsBlock(void* dst, nuint dstCapacity, void* src, nuint srcSize)
        {
            byte* ostart = (byte*)dst;
            uint flSize = (uint)(1 + (srcSize > 31 ? 1 : 0) + (srcSize > 4095 ? 1 : 0));
            switch (flSize)
            {
                case 1:
                    ostart[0] = (byte)((uint)symbolEncodingType_e.set_rle + (srcSize << 3));
                    break;
                case 2:
                    MEM_writeLE16(ostart, (ushort)((uint)symbolEncodingType_e.set_rle + (1 << 2) + (srcSize << 4)));
                    break;
                case 3:
                    MEM_writeLE32(ostart, (uint)((uint)symbolEncodingType_e.set_rle + (3 << 2) + (srcSize << 4)));
                    break;
                default:
                    assert(0 != 0);
                    break;
            }

            ostart[flSize] = *(byte*)src;
            return flSize + 1;
        }

        /* If suspectUncompressible then some sampling checks will be run to potentially skip huffman coding */
        public static nuint ZSTD_compressLiterals(ZSTD_hufCTables_t* prevHuf, ZSTD_hufCTables_t* nextHuf, ZSTD_strategy strategy, int disableLiteralCompression, void* dst, nuint dstCapacity, void* src, nuint srcSize, void* entropyWorkspace, nuint entropyWorkspaceSize, int bmi2, uint suspectUncompressible)
        {
            nuint minGain = ZSTD_minGain(srcSize, strategy);
            nuint lhSize = (nuint)(3 + (srcSize >= 1 * (1 << 10) ? 1 : 0) + (srcSize >= 16 * (1 << 10) ? 1 : 0));
            byte* ostart = (byte*)dst;
            uint singleStream = srcSize < 256 ? 1U : 0U;
            symbolEncodingType_e hType = symbolEncodingType_e.set_compressed;
            nuint cLitSize;
            memcpy(nextHuf, prevHuf, (uint)sizeof(ZSTD_hufCTables_t));
            if (disableLiteralCompression != 0)
                return ZSTD_noCompressLiterals(dst, dstCapacity, src, srcSize);
            {
                nuint minLitSize = (nuint)(prevHuf->repeatMode == HUF_repeat.HUF_repeat_valid ? 6 : 63);
                if (srcSize <= minLitSize)
                    return ZSTD_noCompressLiterals(dst, dstCapacity, src, srcSize);
            }

            if (dstCapacity < lhSize + 1)
            {
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            }

            {
                HUF_repeat repeat = prevHuf->repeatMode;
                int preferRepeat = strategy < ZSTD_strategy.ZSTD_lazy ? srcSize <= 1024 ? 1 : 0 : 0;
                if (repeat == HUF_repeat.HUF_repeat_valid && lhSize == 3)
                    singleStream = 1;
                cLitSize = singleStream != 0 ? HUF_compress1X_repeat(ostart + lhSize, dstCapacity - lhSize, src, srcSize, 255, 11, entropyWorkspace, entropyWorkspaceSize, (nuint*)nextHuf->CTable, &repeat, preferRepeat, bmi2, suspectUncompressible) : HUF_compress4X_repeat(ostart + lhSize, dstCapacity - lhSize, src, srcSize, 255, 11, entropyWorkspace, entropyWorkspaceSize, (nuint*)nextHuf->CTable, &repeat, preferRepeat, bmi2, suspectUncompressible);
                if (repeat != HUF_repeat.HUF_repeat_none)
                {
                    hType = symbolEncodingType_e.set_repeat;
                }
            }

            if (cLitSize == 0 || cLitSize >= srcSize - minGain || ERR_isError(cLitSize))
            {
                memcpy(nextHuf, prevHuf, (uint)sizeof(ZSTD_hufCTables_t));
                return ZSTD_noCompressLiterals(dst, dstCapacity, src, srcSize);
            }

            if (cLitSize == 1)
            {
                memcpy(nextHuf, prevHuf, (uint)sizeof(ZSTD_hufCTables_t));
                return ZSTD_compressRleLiteralsBlock(dst, dstCapacity, src, srcSize);
            }

            if (hType == symbolEncodingType_e.set_compressed)
            {
                nextHuf->repeatMode = HUF_repeat.HUF_repeat_check;
            }

            switch (lhSize)
            {
                case 3:
                    {
                        uint lhc = (uint)(hType + ((singleStream == 0 ? 1 : 0) << 2)) + ((uint)srcSize << 4) + ((uint)cLitSize << 14);
                        MEM_writeLE24(ostart, lhc);
                        break;
                    }

                case 4:
                    {
                        uint lhc = (uint)(hType + (2 << 2)) + ((uint)srcSize << 4) + ((uint)cLitSize << 18);
                        MEM_writeLE32(ostart, lhc);
                        break;
                    }

                case 5:
                    {
                        uint lhc = (uint)(hType + (3 << 2)) + ((uint)srcSize << 4) + ((uint)cLitSize << 22);
                        MEM_writeLE32(ostart, lhc);
                        ostart[4] = (byte)(cLitSize >> 10);
                        break;
                    }

                default:
                    assert(0 != 0);
                    break;
            }

            return lhSize + cLitSize;
        }
    }
}