using static ZstdSharp.UnsafeHelper;
using System;
using System.Runtime.CompilerServices;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        private static DTableDesc HUF_getDTableDesc(uint* table)
        {
            DTableDesc dtd;
            memcpy(&dtd, table, (uint)sizeof(DTableDesc));
            return dtd;
        }

        /**
         * Packs 4 HUF_DEltX1 structs into a U64. This is used to lay down 4 entries at
         * a time.
         */
        [InlineMethod.Inline]
        private static ulong HUF_DEltX1_set4(byte symbol, byte nbBits)
        {
            ulong D4;
            if (BitConverter.IsLittleEndian)
            {
                D4 = (ulong)((symbol << 8) + nbBits);
            }
            else
            {
                D4 = (ulong)(symbol + (nbBits << 8));
            }

            D4 *= 0x0001000100010001UL;
            return D4;
        }

        /**
         * Increase the tableLog to targetTableLog and rescales the stats.
         * If tableLog > targetTableLog this is a no-op.
         * @returns New tableLog
         */
        private static uint HUF_rescaleStats(byte* huffWeight, uint* rankVal, uint nbSymbols, uint tableLog, uint targetTableLog)
        {
            if (tableLog > targetTableLog)
                return tableLog;
            if (tableLog < targetTableLog)
            {
                uint scale = targetTableLog - tableLog;
                uint s;
                for (s = 0; s < nbSymbols; ++s)
                {
                    huffWeight[s] += (byte)(huffWeight[s] == 0 ? 0 : scale);
                }

                for (s = targetTableLog; s > scale; --s)
                {
                    rankVal[s] = rankVal[s - scale];
                }

                for (s = scale; s > 0; --s)
                {
                    rankVal[s] = 0;
                }
            }

            return targetTableLog;
        }

        public static nuint HUF_readDTableX1_wksp(uint* DTable, void* src, nuint srcSize, void* workSpace, nuint wkspSize)
        {
            return HUF_readDTableX1_wksp_bmi2(DTable, src, srcSize, workSpace, wkspSize, 0);
        }

        public static nuint HUF_readDTableX1_wksp_bmi2(uint* DTable, void* src, nuint srcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            uint tableLog = 0;
            uint nbSymbols = 0;
            nuint iSize;
            void* dtPtr = DTable + 1;
            HUF_DEltX1* dt = (HUF_DEltX1*)dtPtr;
            HUF_ReadDTableX1_Workspace* wksp = (HUF_ReadDTableX1_Workspace*)workSpace;
            if ((uint)sizeof(HUF_ReadDTableX1_Workspace) > wkspSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_tableLog_tooLarge));
            iSize = HUF_readStats_wksp(wksp->huffWeight, 255 + 1, wksp->rankVal, &nbSymbols, &tableLog, src, srcSize, wksp->statsWksp, sizeof(uint) * 218, bmi2);
            if (ERR_isError(iSize))
                return iSize;
            {
                DTableDesc dtd = HUF_getDTableDesc(DTable);
                uint maxTableLog = (uint)(dtd.maxTableLog + 1);
                uint targetTableLog = maxTableLog < 11 ? maxTableLog : 11;
                tableLog = HUF_rescaleStats(wksp->huffWeight, wksp->rankVal, nbSymbols, tableLog, targetTableLog);
                if (tableLog > (uint)(dtd.maxTableLog + 1))
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_tableLog_tooLarge));
                dtd.tableType = 0;
                dtd.tableLog = (byte)tableLog;
                memcpy(DTable, &dtd, (uint)sizeof(DTableDesc));
            }

            {
                int n;
                int nextRankStart = 0;
                const int unroll = 4;
                int nLimit = (int)nbSymbols - unroll + 1;
                for (n = 0; n < (int)tableLog + 1; n++)
                {
                    uint curr = (uint)nextRankStart;
                    nextRankStart += (int)wksp->rankVal[n];
                    wksp->rankStart[n] = curr;
                }

                for (n = 0; n < nLimit; n += unroll)
                {
                    int u;
                    for (u = 0; u < unroll; ++u)
                    {
                        nuint w = wksp->huffWeight[n + u];
                        wksp->symbols[wksp->rankStart[w]++] = (byte)(n + u);
                    }
                }

                for (; n < (int)nbSymbols; ++n)
                {
                    nuint w = wksp->huffWeight[n];
                    wksp->symbols[wksp->rankStart[w]++] = (byte)n;
                }
            }

            {
                uint w;
                int symbol = (int)wksp->rankVal[0];
                int rankStart = 0;
                for (w = 1; w < tableLog + 1; ++w)
                {
                    int symbolCount = (int)wksp->rankVal[w];
                    int length = 1 << (int)w >> 1;
                    int uStart = rankStart;
                    byte nbBits = (byte)(tableLog + 1 - w);
                    int s;
                    int u;
                    switch (length)
                    {
                        case 1:
                            for (s = 0; s < symbolCount; ++s)
                            {
                                HUF_DEltX1 D;
                                D.@byte = wksp->symbols[symbol + s];
                                D.nbBits = nbBits;
                                dt[uStart] = D;
                                uStart += 1;
                            }

                            break;
                        case 2:
                            for (s = 0; s < symbolCount; ++s)
                            {
                                HUF_DEltX1 D;
                                D.@byte = wksp->symbols[symbol + s];
                                D.nbBits = nbBits;
                                dt[uStart + 0] = D;
                                dt[uStart + 1] = D;
                                uStart += 2;
                            }

                            break;
                        case 4:
                            for (s = 0; s < symbolCount; ++s)
                            {
                                ulong D4 = HUF_DEltX1_set4(wksp->symbols[symbol + s], nbBits);
                                MEM_write64(dt + uStart, D4);
                                uStart += 4;
                            }

                            break;
                        case 8:
                            for (s = 0; s < symbolCount; ++s)
                            {
                                ulong D4 = HUF_DEltX1_set4(wksp->symbols[symbol + s], nbBits);
                                MEM_write64(dt + uStart, D4);
                                MEM_write64(dt + uStart + 4, D4);
                                uStart += 8;
                            }

                            break;
                        default:
                            for (s = 0; s < symbolCount; ++s)
                            {
                                ulong D4 = HUF_DEltX1_set4(wksp->symbols[symbol + s], nbBits);
                                for (u = 0; u < length; u += 16)
                                {
                                    MEM_write64(dt + uStart + u + 0, D4);
                                    MEM_write64(dt + uStart + u + 4, D4);
                                    MEM_write64(dt + uStart + u + 8, D4);
                                    MEM_write64(dt + uStart + u + 12, D4);
                                }

                                assert(u == length);
                                uStart += length;
                            }

                            break;
                    }

                    symbol += symbolCount;
                    rankStart += symbolCount * length;
                }
            }

            return iSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte HUF_decodeSymbolX1(BIT_DStream_t* Dstream, HUF_DEltX1* dt, uint dtLog)
        {
            /* note : dtLog >= 1 */
            nuint val = BIT_lookBitsFast(Dstream, dtLog);
            byte c = dt[val].@byte;
            BIT_skipBits(Dstream, dt[val].nbBits);
            return c;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decodeStreamX1(byte* p, BIT_DStream_t* bitDPtr, byte* pEnd, HUF_DEltX1* dt, uint dtLog)
        {
            byte* pStart = p;
            if (pEnd - p > 3)
            {
                while (BIT_reloadDStream(bitDPtr) == BIT_DStream_status.BIT_DStream_unfinished && p < pEnd - 3)
                {
                    if (MEM_64bits)
                        *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
                    if (MEM_64bits || 12 <= 12)
                        *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
                    if (MEM_64bits)
                        *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
                    *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
                }
            }
            else
            {
                BIT_reloadDStream(bitDPtr);
            }

            if (MEM_32bits)
                while (BIT_reloadDStream(bitDPtr) == BIT_DStream_status.BIT_DStream_unfinished && p < pEnd)
                    *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
            while (p < pEnd)
                *p++ = HUF_decodeSymbolX1(bitDPtr, dt, dtLog);
            return (nuint)(pEnd - pStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decompress1X1_usingDTable_internal_body(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            byte* op = (byte*)dst;
            byte* oend = op + dstSize;
            void* dtPtr = DTable + 1;
            HUF_DEltX1* dt = (HUF_DEltX1*)dtPtr;
            BIT_DStream_t bitD;
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            uint dtLog = dtd.tableLog;
            {
                nuint _var_err__ = BIT_initDStream(&bitD, cSrc, cSrcSize);
                if (ERR_isError(_var_err__))
                    return _var_err__;
            }

            HUF_decodeStreamX1(op, &bitD, oend, dt, dtLog);
            if (BIT_endOfDStream(&bitD) == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            return dstSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decompress4X1_usingDTable_internal_body(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            if (cSrcSize < 10)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            {
                byte* istart = (byte*)cSrc;
                byte* ostart = (byte*)dst;
                byte* oend = ostart + dstSize;
                byte* olimit = oend - 3;
                void* dtPtr = DTable + 1;
                HUF_DEltX1* dt = (HUF_DEltX1*)dtPtr;
                /* Init */
                BIT_DStream_t bitD1;
                BIT_DStream_t bitD2;
                BIT_DStream_t bitD3;
                BIT_DStream_t bitD4;
                nuint length1 = MEM_readLE16(istart);
                nuint length2 = MEM_readLE16(istart + 2);
                nuint length3 = MEM_readLE16(istart + 4);
                nuint length4 = cSrcSize - (length1 + length2 + length3 + 6);
                /* jumpTable */
                byte* istart1 = istart + 6;
                byte* istart2 = istart1 + length1;
                byte* istart3 = istart2 + length2;
                byte* istart4 = istart3 + length3;
                nuint segmentSize = (dstSize + 3) / 4;
                byte* opStart2 = ostart + segmentSize;
                byte* opStart3 = opStart2 + segmentSize;
                byte* opStart4 = opStart3 + segmentSize;
                byte* op1 = ostart;
                byte* op2 = opStart2;
                byte* op3 = opStart3;
                byte* op4 = opStart4;
                DTableDesc dtd = HUF_getDTableDesc(DTable);
                uint dtLog = dtd.tableLog;
                uint endSignal = 1;
                if (length4 > cSrcSize)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (opStart4 > oend)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                {
                    nuint _var_err__ = BIT_initDStream(&bitD1, istart1, length1);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD2, istart2, length2);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD3, istart3, length3);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD4, istart4, length4);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                if ((nuint)(oend - op4) >= (uint)sizeof(nuint))
                {
                    for (; (endSignal & (uint)(op4 < olimit ? 1 : 0)) != 0;)
                    {
                        if (MEM_64bits)
                            *op1++ = HUF_decodeSymbolX1(&bitD1, dt, dtLog);
                        if (MEM_64bits)
                            *op2++ = HUF_decodeSymbolX1(&bitD2, dt, dtLog);
                        if (MEM_64bits)
                            *op3++ = HUF_decodeSymbolX1(&bitD3, dt, dtLog);
                        if (MEM_64bits)
                            *op4++ = HUF_decodeSymbolX1(&bitD4, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            *op1++ = HUF_decodeSymbolX1(&bitD1, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            *op2++ = HUF_decodeSymbolX1(&bitD2, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            *op3++ = HUF_decodeSymbolX1(&bitD3, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            *op4++ = HUF_decodeSymbolX1(&bitD4, dt, dtLog);
                        if (MEM_64bits)
                            *op1++ = HUF_decodeSymbolX1(&bitD1, dt, dtLog);
                        if (MEM_64bits)
                            *op2++ = HUF_decodeSymbolX1(&bitD2, dt, dtLog);
                        if (MEM_64bits)
                            *op3++ = HUF_decodeSymbolX1(&bitD3, dt, dtLog);
                        if (MEM_64bits)
                            *op4++ = HUF_decodeSymbolX1(&bitD4, dt, dtLog);
                        *op1++ = HUF_decodeSymbolX1(&bitD1, dt, dtLog);
                        *op2++ = HUF_decodeSymbolX1(&bitD2, dt, dtLog);
                        *op3++ = HUF_decodeSymbolX1(&bitD3, dt, dtLog);
                        *op4++ = HUF_decodeSymbolX1(&bitD4, dt, dtLog);
                        endSignal &= BIT_reloadDStreamFast(&bitD1) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        endSignal &= BIT_reloadDStreamFast(&bitD2) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        endSignal &= BIT_reloadDStreamFast(&bitD3) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        endSignal &= BIT_reloadDStreamFast(&bitD4) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                    }
                }

                if (op1 > opStart2)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (op2 > opStart3)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (op3 > opStart4)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                HUF_decodeStreamX1(op1, &bitD1, opStart2, dt, dtLog);
                HUF_decodeStreamX1(op2, &bitD2, opStart3, dt, dtLog);
                HUF_decodeStreamX1(op3, &bitD3, opStart4, dt, dtLog);
                HUF_decodeStreamX1(op4, &bitD4, oend, dt, dtLog);
                {
                    uint endCheck = BIT_endOfDStream(&bitD1) & BIT_endOfDStream(&bitD2) & BIT_endOfDStream(&bitD3) & BIT_endOfDStream(&bitD4);
                    if (endCheck == 0)
                        return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                }

                return dstSize;
            }
        }

        private static nuint HUF_decompress4X1_usingDTable_internal_default(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            return HUF_decompress4X1_usingDTable_internal_body(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        private static nuint HUF_decompress1X1_usingDTable_internal(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            return HUF_decompress1X1_usingDTable_internal_body(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        private static nuint HUF_decompress4X1_usingDTable_internal(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            return HUF_decompress4X1_usingDTable_internal_default(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        public static nuint HUF_decompress1X1_usingDTable(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            if (dtd.tableType != 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_GENERIC));
            return HUF_decompress1X1_usingDTable_internal(dst, dstSize, cSrc, cSrcSize, DTable, 0);
        }

        public static nuint HUF_decompress1X1_DCtx_wksp(uint* DCtx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            byte* ip = (byte*)cSrc;
            nuint hSize = HUF_readDTableX1_wksp(DCtx, cSrc, cSrcSize, workSpace, wkspSize);
            if (ERR_isError(hSize))
                return hSize;
            if (hSize >= cSrcSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_srcSize_wrong));
            ip += hSize;
            cSrcSize -= hSize;
            return HUF_decompress1X1_usingDTable_internal(dst, dstSize, ip, cSrcSize, DCtx, 0);
        }

        public static nuint HUF_decompress4X1_usingDTable(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            if (dtd.tableType != 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_GENERIC));
            return HUF_decompress4X1_usingDTable_internal(dst, dstSize, cSrc, cSrcSize, DTable, 0);
        }

        private static nuint HUF_decompress4X1_DCtx_wksp_bmi2(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            byte* ip = (byte*)cSrc;
            nuint hSize = HUF_readDTableX1_wksp_bmi2(dctx, cSrc, cSrcSize, workSpace, wkspSize, bmi2);
            if (ERR_isError(hSize))
                return hSize;
            if (hSize >= cSrcSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_srcSize_wrong));
            ip += hSize;
            cSrcSize -= hSize;
            return HUF_decompress4X1_usingDTable_internal(dst, dstSize, ip, cSrcSize, dctx, bmi2);
        }

        public static nuint HUF_decompress4X1_DCtx_wksp(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            return HUF_decompress4X1_DCtx_wksp_bmi2(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize, 0);
        }

        /**
         * Constructs a HUF_DEltX2 in a U32.
         */
        [InlineMethod.Inline]
        private static uint HUF_buildDEltX2U32(uint symbol, uint nbBits, uint baseSeq, int level)
        {
            uint seq;
            if (BitConverter.IsLittleEndian)
            {
                seq = level == 1 ? symbol : baseSeq + (symbol << 8);
                return seq + (nbBits << 16) + ((uint)level << 24);
            }
            else
            {
                seq = level == 1 ? symbol << 8 : (baseSeq << 8) + symbol;
                return (seq << 16) + (nbBits << 8) + (uint)level;
            }
        }

        /**
         * Constructs a HUF_DEltX2.
         */
        [InlineMethod.Inline]
        private static HUF_DEltX2 HUF_buildDEltX2(uint symbol, uint nbBits, uint baseSeq, int level)
        {
            HUF_DEltX2 DElt;
            uint val = HUF_buildDEltX2U32(symbol, nbBits, baseSeq, level);
            memcpy(&DElt, &val, sizeof(uint));
            return DElt;
        }

        /**
         * Constructs 2 HUF_DEltX2s and packs them into a U64.
         */
        [InlineMethod.Inline]
        private static ulong HUF_buildDEltX2U64(uint symbol, uint nbBits, ushort baseSeq, int level)
        {
            uint DElt = HUF_buildDEltX2U32(symbol, nbBits, baseSeq, level);
            return DElt + ((ulong)DElt << 32);
        }

        /**
         * Fills the DTable rank with all the symbols from [begin, end) that are each
         * nbBits long.
         *
         * @param DTableRank The start of the rank in the DTable.
         * @param begin The first symbol to fill (inclusive).
         * @param end The last symbol to fill (exclusive).
         * @param nbBits Each symbol is nbBits long.
         * @param tableLog The table log.
         * @param baseSeq If level == 1 { 0 } else { the first level symbol }
         * @param level The level in the table. Must be 1 or 2.
         */
        [InlineMethod.Inline]
        private static void HUF_fillDTableX2ForWeight(HUF_DEltX2* DTableRank, sortedSymbol_t* begin, sortedSymbol_t* end, uint nbBits, uint tableLog, ushort baseSeq, int level)
        {
            /* quiet static-analyzer */
            uint length = 1U << (int)(tableLog - nbBits & 0x1F);
            sortedSymbol_t* ptr;
            assert(level >= 1 && level <= 2);
            switch (length)
            {
                case 1:
                    for (ptr = begin; ptr != end; ++ptr)
                    {
                        HUF_DEltX2 DElt = HUF_buildDEltX2(ptr->symbol, nbBits, baseSeq, level);
                        *DTableRank++ = DElt;
                    }

                    break;
                case 2:
                    for (ptr = begin; ptr != end; ++ptr)
                    {
                        HUF_DEltX2 DElt = HUF_buildDEltX2(ptr->symbol, nbBits, baseSeq, level);
                        DTableRank[0] = DElt;
                        DTableRank[1] = DElt;
                        DTableRank += 2;
                    }

                    break;
                case 4:
                    for (ptr = begin; ptr != end; ++ptr)
                    {
                        ulong DEltX2 = HUF_buildDEltX2U64(ptr->symbol, nbBits, baseSeq, level);
                        memcpy(DTableRank + 0, &DEltX2, sizeof(ulong));
                        memcpy(DTableRank + 2, &DEltX2, sizeof(ulong));
                        DTableRank += 4;
                    }

                    break;
                case 8:
                    for (ptr = begin; ptr != end; ++ptr)
                    {
                        ulong DEltX2 = HUF_buildDEltX2U64(ptr->symbol, nbBits, baseSeq, level);
                        memcpy(DTableRank + 0, &DEltX2, sizeof(ulong));
                        memcpy(DTableRank + 2, &DEltX2, sizeof(ulong));
                        memcpy(DTableRank + 4, &DEltX2, sizeof(ulong));
                        memcpy(DTableRank + 6, &DEltX2, sizeof(ulong));
                        DTableRank += 8;
                    }

                    break;
                default:
                    for (ptr = begin; ptr != end; ++ptr)
                    {
                        ulong DEltX2 = HUF_buildDEltX2U64(ptr->symbol, nbBits, baseSeq, level);
                        HUF_DEltX2* DTableRankEnd = DTableRank + length;
                        for (; DTableRank != DTableRankEnd; DTableRank += 8)
                        {
                            memcpy(DTableRank + 0, &DEltX2, sizeof(ulong));
                            memcpy(DTableRank + 2, &DEltX2, sizeof(ulong));
                            memcpy(DTableRank + 4, &DEltX2, sizeof(ulong));
                            memcpy(DTableRank + 6, &DEltX2, sizeof(ulong));
                        }
                    }

                    break;
            }
        }

        /* HUF_fillDTableX2Level2() :
         * `rankValOrigin` must be a table of at least (HUF_TABLELOG_MAX + 1) U32 */
        [InlineMethod.Inline]
        private static void HUF_fillDTableX2Level2(HUF_DEltX2* DTable, uint targetLog, uint consumedBits, uint* rankVal, int minWeight, int maxWeight1, sortedSymbol_t* sortedSymbols, uint* rankStart, uint nbBitsBaseline, ushort baseSeq)
        {
            if (minWeight > 1)
            {
                /* quiet static-analyzer */
                uint length = 1U << (int)(targetLog - consumedBits & 0x1F);
                /* baseSeq */
                ulong DEltX2 = HUF_buildDEltX2U64(baseSeq, consumedBits, 0, 1);
                int skipSize = (int)rankVal[minWeight];
                assert(length > 1);
                assert((uint)skipSize < length);
                switch (length)
                {
                    case 2:
                        assert(skipSize == 1);
                        memcpy(DTable, &DEltX2, sizeof(ulong));
                        break;
                    case 4:
                        assert(skipSize <= 4);
                        memcpy(DTable + 0, &DEltX2, sizeof(ulong));
                        memcpy(DTable + 2, &DEltX2, sizeof(ulong));
                        break;
                    default:
                        {
                            int i;
                            for (i = 0; i < skipSize; i += 8)
                            {
                                memcpy(DTable + i + 0, &DEltX2, sizeof(ulong));
                                memcpy(DTable + i + 2, &DEltX2, sizeof(ulong));
                                memcpy(DTable + i + 4, &DEltX2, sizeof(ulong));
                                memcpy(DTable + i + 6, &DEltX2, sizeof(ulong));
                            }
                        }

                        break;
                }
            }

            {
                int w;
                for (w = minWeight; w < maxWeight1; ++w)
                {
                    int begin = (int)rankStart[w];
                    int end = (int)rankStart[w + 1];
                    uint nbBits = nbBitsBaseline - (uint)w;
                    uint totalBits = nbBits + consumedBits;
                    HUF_fillDTableX2ForWeight(DTable + rankVal[w], sortedSymbols + begin, sortedSymbols + end, totalBits, targetLog, baseSeq, 2);
                }
            }
        }

        private static void HUF_fillDTableX2(HUF_DEltX2* DTable, uint targetLog, sortedSymbol_t* sortedList, uint* rankStart, rankValCol_t* rankValOrigin, uint maxWeight, uint nbBitsBaseline)
        {
            uint* rankVal = (uint*)rankValOrigin[0];
            /* note : targetLog >= srcLog, hence scaleLog <= 1 */
            int scaleLog = (int)(nbBitsBaseline - targetLog);
            uint minBits = nbBitsBaseline - maxWeight;
            int w;
            int wEnd = (int)maxWeight + 1;
            for (w = 1; w < wEnd; ++w)
            {
                int begin = (int)rankStart[w];
                int end = (int)rankStart[w + 1];
                uint nbBits = nbBitsBaseline - (uint)w;
                if (targetLog - nbBits >= minBits)
                {
                    /* Enough room for a second symbol. */
                    int start = (int)rankVal[w];
                    /* quiet static-analyzer */
                    uint length = 1U << (int)(targetLog - nbBits & 0x1F);
                    int minWeight = (int)(nbBits + (uint)scaleLog);
                    int s;
                    if (minWeight < 1)
                        minWeight = 1;
                    for (s = begin; s != end; ++s)
                    {
                        HUF_fillDTableX2Level2(DTable + start, targetLog, nbBits, (uint*)rankValOrigin[nbBits], minWeight, wEnd, sortedList, rankStart, nbBitsBaseline, sortedList[s].symbol);
                        start += (int)length;
                    }
                }
                else
                {
                    HUF_fillDTableX2ForWeight(DTable + rankVal[w], sortedList + begin, sortedList + end, nbBits, targetLog, 0, 1);
                }
            }
        }

        public static nuint HUF_readDTableX2_wksp(uint* DTable, void* src, nuint srcSize, void* workSpace, nuint wkspSize)
        {
            return HUF_readDTableX2_wksp_bmi2(DTable, src, srcSize, workSpace, wkspSize, 0);
        }

        public static nuint HUF_readDTableX2_wksp_bmi2(uint* DTable, void* src, nuint srcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            uint tableLog, maxW, nbSymbols;
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            uint maxTableLog = dtd.maxTableLog;
            nuint iSize;
            /* force compiler to avoid strict-aliasing */
            void* dtPtr = DTable + 1;
            HUF_DEltX2* dt = (HUF_DEltX2*)dtPtr;
            uint* rankStart;
            HUF_ReadDTableX2_Workspace* wksp = (HUF_ReadDTableX2_Workspace*)workSpace;
            if ((uint)sizeof(HUF_ReadDTableX2_Workspace) > wkspSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_GENERIC));
            rankStart = wksp->rankStart0 + 1;
            memset(wksp->rankStats, 0, sizeof(uint) * 13);
            memset(wksp->rankStart0, 0, sizeof(uint) * 15);
            if (maxTableLog > 12)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_tableLog_tooLarge));
            iSize = HUF_readStats_wksp(wksp->weightList, 255 + 1, wksp->rankStats, &nbSymbols, &tableLog, src, srcSize, wksp->calleeWksp, sizeof(uint) * 218, bmi2);
            if (ERR_isError(iSize))
                return iSize;
            if (tableLog > maxTableLog)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_tableLog_tooLarge));
            if (tableLog <= 11 && maxTableLog > 11)
                maxTableLog = 11;
            for (maxW = tableLog; wksp->rankStats[maxW] == 0; maxW--)
            {
            }

            {
                uint w, nextRankStart = 0;
                for (w = 1; w < maxW + 1; w++)
                {
                    uint curr = nextRankStart;
                    nextRankStart += wksp->rankStats[w];
                    rankStart[w] = curr;
                }

                rankStart[0] = nextRankStart;
                rankStart[maxW + 1] = nextRankStart;
            }

            {
                uint s;
                for (s = 0; s < nbSymbols; s++)
                {
                    uint w = wksp->weightList[s];
                    uint r = rankStart[w]++;
                    wksp->sortedSymbol[r].symbol = (byte)s;
                }

                rankStart[0] = 0;
            }

            {
                uint* rankVal0 = (uint*)wksp->rankVal[0];
                {
                    /* tableLog <= maxTableLog */
                    int rescale = (int)(maxTableLog - tableLog - 1);
                    uint nextRankVal = 0;
                    uint w;
                    for (w = 1; w < maxW + 1; w++)
                    {
                        uint curr = nextRankVal;
                        nextRankVal += wksp->rankStats[w] << (int)(w + (uint)rescale);
                        rankVal0[w] = curr;
                    }
                }

                {
                    uint minBits = tableLog + 1 - maxW;
                    uint consumed;
                    for (consumed = minBits; consumed < maxTableLog - minBits + 1; consumed++)
                    {
                        uint* rankValPtr = (uint*)wksp->rankVal[consumed];
                        uint w;
                        for (w = 1; w < maxW + 1; w++)
                        {
                            rankValPtr[w] = rankVal0[w] >> (int)consumed;
                        }
                    }
                }
            }

            HUF_fillDTableX2(dt, maxTableLog, (sortedSymbol_t*)wksp->sortedSymbol, wksp->rankStart0, wksp->rankVal, maxW, tableLog + 1);
            dtd.tableLog = (byte)maxTableLog;
            dtd.tableType = 1;
            memcpy(DTable, &dtd, (uint)sizeof(DTableDesc));
            return iSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static uint HUF_decodeSymbolX2(void* op, BIT_DStream_t* DStream, HUF_DEltX2* dt, uint dtLog)
        {
            /* note : dtLog >= 1 */
            nuint val = BIT_lookBitsFast(DStream, dtLog);
            memcpy(op, &dt[val].sequence, 2);
            BIT_skipBits(DStream, dt[val].nbBits);
            return dt[val].length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint HUF_decodeLastSymbolX2(void* op, BIT_DStream_t* DStream, HUF_DEltX2* dt, uint dtLog)
        {
            /* note : dtLog >= 1 */
            nuint val = BIT_lookBitsFast(DStream, dtLog);
            memcpy(op, &dt[val].sequence, 1);
            if (dt[val].length == 1)
            {
                BIT_skipBits(DStream, dt[val].nbBits);
            }
            else
            {
                if (DStream->bitsConsumed < (uint)(sizeof(nuint) * 8))
                {
                    BIT_skipBits(DStream, dt[val].nbBits);
                    if (DStream->bitsConsumed > (uint)(sizeof(nuint) * 8))
                        DStream->bitsConsumed = (uint)(sizeof(nuint) * 8);
                }
            }

            return 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decodeStreamX2(byte* p, BIT_DStream_t* bitDPtr, byte* pEnd, HUF_DEltX2* dt, uint dtLog)
        {
            byte* pStart = p;
            if ((nuint)(pEnd - p) >= (uint)sizeof(nuint))
            {
                if (dtLog <= 11 && MEM_64bits)
                {
                    while (BIT_reloadDStream(bitDPtr) == BIT_DStream_status.BIT_DStream_unfinished && p < pEnd - 9)
                    {
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                    }
                }
                else
                {
                    while (BIT_reloadDStream(bitDPtr) == BIT_DStream_status.BIT_DStream_unfinished && p < pEnd - (sizeof(nuint) - 1))
                    {
                        if (MEM_64bits)
                            p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        if (MEM_64bits)
                            p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                        p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                    }
                }
            }
            else
            {
                BIT_reloadDStream(bitDPtr);
            }

            if ((nuint)(pEnd - p) >= 2)
            {
                while (BIT_reloadDStream(bitDPtr) == BIT_DStream_status.BIT_DStream_unfinished && p <= pEnd - 2)
                    p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
                while (p <= pEnd - 2)
                    p += HUF_decodeSymbolX2(p, bitDPtr, dt, dtLog);
            }

            if (p < pEnd)
                p += HUF_decodeLastSymbolX2(p, bitDPtr, dt, dtLog);
            return (nuint)(p - pStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decompress1X2_usingDTable_internal_body(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            BIT_DStream_t bitD;
            {
                nuint _var_err__ = BIT_initDStream(&bitD, cSrc, cSrcSize);
                if (ERR_isError(_var_err__))
                    return _var_err__;
            }

            {
                byte* ostart = (byte*)dst;
                byte* oend = ostart + dstSize;
                /* force compiler to not use strict-aliasing */
                void* dtPtr = DTable + 1;
                HUF_DEltX2* dt = (HUF_DEltX2*)dtPtr;
                DTableDesc dtd = HUF_getDTableDesc(DTable);
                HUF_decodeStreamX2(ostart, &bitD, oend, dt, dtd.tableLog);
            }

            if (BIT_endOfDStream(&bitD) == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            return dstSize;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint HUF_decompress4X2_usingDTable_internal_body(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            if (cSrcSize < 10)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            {
                byte* istart = (byte*)cSrc;
                byte* ostart = (byte*)dst;
                byte* oend = ostart + dstSize;
                byte* olimit = oend - (sizeof(nuint) - 1);
                void* dtPtr = DTable + 1;
                HUF_DEltX2* dt = (HUF_DEltX2*)dtPtr;
                /* Init */
                BIT_DStream_t bitD1;
                BIT_DStream_t bitD2;
                BIT_DStream_t bitD3;
                BIT_DStream_t bitD4;
                nuint length1 = MEM_readLE16(istart);
                nuint length2 = MEM_readLE16(istart + 2);
                nuint length3 = MEM_readLE16(istart + 4);
                nuint length4 = cSrcSize - (length1 + length2 + length3 + 6);
                /* jumpTable */
                byte* istart1 = istart + 6;
                byte* istart2 = istart1 + length1;
                byte* istart3 = istart2 + length2;
                byte* istart4 = istart3 + length3;
                nuint segmentSize = (dstSize + 3) / 4;
                byte* opStart2 = ostart + segmentSize;
                byte* opStart3 = opStart2 + segmentSize;
                byte* opStart4 = opStart3 + segmentSize;
                byte* op1 = ostart;
                byte* op2 = opStart2;
                byte* op3 = opStart3;
                byte* op4 = opStart4;
                uint endSignal = 1;
                DTableDesc dtd = HUF_getDTableDesc(DTable);
                uint dtLog = dtd.tableLog;
                if (length4 > cSrcSize)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (opStart4 > oend)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                {
                    nuint _var_err__ = BIT_initDStream(&bitD1, istart1, length1);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD2, istart2, length2);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD3, istart3, length3);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                {
                    nuint _var_err__ = BIT_initDStream(&bitD4, istart4, length4);
                    if (ERR_isError(_var_err__))
                        return _var_err__;
                }

                if ((nuint)(oend - op4) >= (uint)sizeof(nuint))
                {
                    for (; (endSignal & (uint)(op4 < olimit ? 1 : 0)) != 0;)
                    {
                        if (MEM_64bits)
                            op1 += HUF_decodeSymbolX2(op1, &bitD1, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            op1 += HUF_decodeSymbolX2(op1, &bitD1, dt, dtLog);
                        if (MEM_64bits)
                            op1 += HUF_decodeSymbolX2(op1, &bitD1, dt, dtLog);
                        op1 += HUF_decodeSymbolX2(op1, &bitD1, dt, dtLog);
                        if (MEM_64bits)
                            op2 += HUF_decodeSymbolX2(op2, &bitD2, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            op2 += HUF_decodeSymbolX2(op2, &bitD2, dt, dtLog);
                        if (MEM_64bits)
                            op2 += HUF_decodeSymbolX2(op2, &bitD2, dt, dtLog);
                        op2 += HUF_decodeSymbolX2(op2, &bitD2, dt, dtLog);
                        endSignal &= BIT_reloadDStreamFast(&bitD1) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        endSignal &= BIT_reloadDStreamFast(&bitD2) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        if (MEM_64bits)
                            op3 += HUF_decodeSymbolX2(op3, &bitD3, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            op3 += HUF_decodeSymbolX2(op3, &bitD3, dt, dtLog);
                        if (MEM_64bits)
                            op3 += HUF_decodeSymbolX2(op3, &bitD3, dt, dtLog);
                        op3 += HUF_decodeSymbolX2(op3, &bitD3, dt, dtLog);
                        if (MEM_64bits)
                            op4 += HUF_decodeSymbolX2(op4, &bitD4, dt, dtLog);
                        if (MEM_64bits || 12 <= 12)
                            op4 += HUF_decodeSymbolX2(op4, &bitD4, dt, dtLog);
                        if (MEM_64bits)
                            op4 += HUF_decodeSymbolX2(op4, &bitD4, dt, dtLog);
                        op4 += HUF_decodeSymbolX2(op4, &bitD4, dt, dtLog);
                        endSignal &= BIT_reloadDStreamFast(&bitD3) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                        endSignal &= BIT_reloadDStreamFast(&bitD4) == BIT_DStream_status.BIT_DStream_unfinished ? 1U : 0U;
                    }
                }

                if (op1 > opStart2)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (op2 > opStart3)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                if (op3 > opStart4)
                    return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                HUF_decodeStreamX2(op1, &bitD1, opStart2, dt, dtLog);
                HUF_decodeStreamX2(op2, &bitD2, opStart3, dt, dtLog);
                HUF_decodeStreamX2(op3, &bitD3, opStart4, dt, dtLog);
                HUF_decodeStreamX2(op4, &bitD4, oend, dt, dtLog);
                {
                    uint endCheck = BIT_endOfDStream(&bitD1) & BIT_endOfDStream(&bitD2) & BIT_endOfDStream(&bitD3) & BIT_endOfDStream(&bitD4);
                    if (endCheck == 0)
                        return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
                }

                return dstSize;
            }
        }

        private static nuint HUF_decompress4X2_usingDTable_internal_default(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            return HUF_decompress4X2_usingDTable_internal_body(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        private static nuint HUF_decompress4X2_usingDTable_internal(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            return HUF_decompress4X2_usingDTable_internal_default(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        private static nuint HUF_decompress1X2_usingDTable_internal(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            return HUF_decompress1X2_usingDTable_internal_body(dst, dstSize, cSrc, cSrcSize, DTable);
        }

        public static nuint HUF_decompress1X2_usingDTable(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            if (dtd.tableType != 1)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_GENERIC));
            return HUF_decompress1X2_usingDTable_internal(dst, dstSize, cSrc, cSrcSize, DTable, 0);
        }

        public static nuint HUF_decompress1X2_DCtx_wksp(uint* DCtx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            byte* ip = (byte*)cSrc;
            nuint hSize = HUF_readDTableX2_wksp(DCtx, cSrc, cSrcSize, workSpace, wkspSize);
            if (ERR_isError(hSize))
                return hSize;
            if (hSize >= cSrcSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_srcSize_wrong));
            ip += hSize;
            cSrcSize -= hSize;
            return HUF_decompress1X2_usingDTable_internal(dst, dstSize, ip, cSrcSize, DCtx, 0);
        }

        public static nuint HUF_decompress4X2_usingDTable(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            if (dtd.tableType != 1)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_GENERIC));
            return HUF_decompress4X2_usingDTable_internal(dst, dstSize, cSrc, cSrcSize, DTable, 0);
        }

        private static nuint HUF_decompress4X2_DCtx_wksp_bmi2(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            byte* ip = (byte*)cSrc;
            nuint hSize = HUF_readDTableX2_wksp(dctx, cSrc, cSrcSize, workSpace, wkspSize);
            if (ERR_isError(hSize))
                return hSize;
            if (hSize >= cSrcSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_srcSize_wrong));
            ip += hSize;
            cSrcSize -= hSize;
            return HUF_decompress4X2_usingDTable_internal(dst, dstSize, ip, cSrcSize, dctx, bmi2);
        }

        public static nuint HUF_decompress4X2_DCtx_wksp(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            return HUF_decompress4X2_DCtx_wksp_bmi2(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize, 0);
        }

        /* ***********************************/
        /* Universal decompression selectors */
        /* ***********************************/
        public static nuint HUF_decompress1X_usingDTable(void* dst, nuint maxDstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            return dtd.tableType != 0 ? HUF_decompress1X2_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, 0) : HUF_decompress1X1_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, 0);
        }

        public static nuint HUF_decompress4X_usingDTable(void* dst, nuint maxDstSize, void* cSrc, nuint cSrcSize, uint* DTable)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            return dtd.tableType != 0 ? HUF_decompress4X2_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, 0) : HUF_decompress4X1_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, 0);
        }

        public static algo_time_t[][] algoTime = new algo_time_t[16][] { new algo_time_t[2] { new algo_time_t { tableTime = 0, decode256Time = 0 }, new algo_time_t { tableTime = 1, decode256Time = 1 } }, new algo_time_t[2] { new algo_time_t { tableTime = 0, decode256Time = 0 }, new algo_time_t { tableTime = 1, decode256Time = 1 } }, new algo_time_t[2] { new algo_time_t { tableTime = 150, decode256Time = 216 }, new algo_time_t { tableTime = 381, decode256Time = 119 } }, new algo_time_t[2] { new algo_time_t { tableTime = 170, decode256Time = 205 }, new algo_time_t { tableTime = 514, decode256Time = 112 } }, new algo_time_t[2] { new algo_time_t { tableTime = 177, decode256Time = 199 }, new algo_time_t { tableTime = 539, decode256Time = 110 } }, new algo_time_t[2] { new algo_time_t { tableTime = 197, decode256Time = 194 }, new algo_time_t { tableTime = 644, decode256Time = 107 } }, new algo_time_t[2] { new algo_time_t { tableTime = 221, decode256Time = 192 }, new algo_time_t { tableTime = 735, decode256Time = 107 } }, new algo_time_t[2] { new algo_time_t { tableTime = 256, decode256Time = 189 }, new algo_time_t { tableTime = 881, decode256Time = 106 } }, new algo_time_t[2] { new algo_time_t { tableTime = 359, decode256Time = 188 }, new algo_time_t { tableTime = 1167, decode256Time = 109 } }, new algo_time_t[2] { new algo_time_t { tableTime = 582, decode256Time = 187 }, new algo_time_t { tableTime = 1570, decode256Time = 114 } }, new algo_time_t[2] { new algo_time_t { tableTime = 688, decode256Time = 187 }, new algo_time_t { tableTime = 1712, decode256Time = 122 } }, new algo_time_t[2] { new algo_time_t { tableTime = 825, decode256Time = 186 }, new algo_time_t { tableTime = 1965, decode256Time = 136 } }, new algo_time_t[2] { new algo_time_t { tableTime = 976, decode256Time = 185 }, new algo_time_t { tableTime = 2131, decode256Time = 150 } }, new algo_time_t[2] { new algo_time_t { tableTime = 1180, decode256Time = 186 }, new algo_time_t { tableTime = 2070, decode256Time = 175 } }, new algo_time_t[2] { new algo_time_t { tableTime = 1377, decode256Time = 185 }, new algo_time_t { tableTime = 1731, decode256Time = 202 } }, new algo_time_t[2] { new algo_time_t { tableTime = 1412, decode256Time = 185 }, new algo_time_t { tableTime = 1695, decode256Time = 202 } } };
        /** HUF_selectDecoder() :
         *  Tells which decoder is likely to decode faster,
         *  based on a set of pre-computed metrics.
         * @return : 0==HUF_decompress4X1, 1==HUF_decompress4X2 .
         *  Assumption : 0 < dstSize <= 128 KB */
        public static uint HUF_selectDecoder(nuint dstSize, nuint cSrcSize)
        {
            assert(dstSize > 0);
            assert(dstSize <= 128 * 1024);
            {
                /* Q < 16 */
                uint Q = cSrcSize >= dstSize ? 15 : (uint)(cSrcSize * 16 / dstSize);
                uint D256 = (uint)(dstSize >> 8);
                uint DTime0 = algoTime[Q][0].tableTime + algoTime[Q][0].decode256Time * D256;
                uint DTime1 = algoTime[Q][1].tableTime + algoTime[Q][1].decode256Time * D256;
                DTime1 += DTime1 >> 5;
                return DTime1 < DTime0 ? 1U : 0U;
            }
        }

        public static nuint HUF_decompress4X_hufOnly_wksp(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            if (dstSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            if (cSrcSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            {
                uint algoNb = HUF_selectDecoder(dstSize, cSrcSize);
                return algoNb != 0 ? HUF_decompress4X2_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize) : HUF_decompress4X1_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize);
            }
        }

        public static nuint HUF_decompress1X_DCtx_wksp(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize)
        {
            if (dstSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            if (cSrcSize > dstSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            if (cSrcSize == dstSize)
            {
                memcpy(dst, cSrc, (uint)dstSize);
                return dstSize;
            }

            if (cSrcSize == 1)
            {
                memset(dst, *(byte*)cSrc, (uint)dstSize);
                return dstSize;
            }

            {
                uint algoNb = HUF_selectDecoder(dstSize, cSrcSize);
                return algoNb != 0 ? HUF_decompress1X2_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize) : HUF_decompress1X1_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize);
            }
        }

        /* BMI2 variants.
         * If the CPU has BMI2 support, pass bmi2=1, otherwise pass bmi2=0.
         */
        public static nuint HUF_decompress1X_usingDTable_bmi2(void* dst, nuint maxDstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            return dtd.tableType != 0 ? HUF_decompress1X2_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, bmi2) : HUF_decompress1X1_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, bmi2);
        }

        public static nuint HUF_decompress1X1_DCtx_wksp_bmi2(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            byte* ip = (byte*)cSrc;
            nuint hSize = HUF_readDTableX1_wksp_bmi2(dctx, cSrc, cSrcSize, workSpace, wkspSize, bmi2);
            if (ERR_isError(hSize))
                return hSize;
            if (hSize >= cSrcSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_srcSize_wrong));
            ip += hSize;
            cSrcSize -= hSize;
            return HUF_decompress1X1_usingDTable_internal(dst, dstSize, ip, cSrcSize, dctx, bmi2);
        }

        public static nuint HUF_decompress4X_usingDTable_bmi2(void* dst, nuint maxDstSize, void* cSrc, nuint cSrcSize, uint* DTable, int bmi2)
        {
            DTableDesc dtd = HUF_getDTableDesc(DTable);
            return dtd.tableType != 0 ? HUF_decompress4X2_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, bmi2) : HUF_decompress4X1_usingDTable_internal(dst, maxDstSize, cSrc, cSrcSize, DTable, bmi2);
        }

        public static nuint HUF_decompress4X_hufOnly_wksp_bmi2(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, void* workSpace, nuint wkspSize, int bmi2)
        {
            if (dstSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            if (cSrcSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            {
                uint algoNb = HUF_selectDecoder(dstSize, cSrcSize);
                return algoNb != 0 ? HUF_decompress4X2_DCtx_wksp_bmi2(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize, bmi2) : HUF_decompress4X1_DCtx_wksp_bmi2(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, wkspSize, bmi2);
            }
        }

        public static nuint HUF_readDTableX1(uint* DTable, void* src, nuint srcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_readDTableX1_wksp(DTable, src, srcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress1X1_DCtx(uint* DCtx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress1X1_DCtx_wksp(DCtx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress1X1(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* DTable = stackalloc uint[2049];
            DTable[0] = (uint)(12 - 1) * 0x01000001;
            memset(DTable + 1, 0, sizeof(uint) * 2048);
            return HUF_decompress1X1_DCtx(DTable, dst, dstSize, cSrc, cSrcSize);
        }

        public static nuint HUF_readDTableX2(uint* DTable, void* src, nuint srcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_readDTableX2_wksp(DTable, src, srcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress1X2_DCtx(uint* DCtx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress1X2_DCtx_wksp(DCtx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress1X2(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* DTable = stackalloc uint[4097];
            DTable[0] = (uint)12 * 0x01000001;
            memset(DTable + 1, 0, sizeof(uint) * 4096);
            return HUF_decompress1X2_DCtx(DTable, dst, dstSize, cSrc, cSrcSize);
        }

        public static nuint HUF_decompress4X1_DCtx(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress4X1_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }

        /* ****************************************
         *  Advanced decompression functions
         ******************************************/
        public static nuint HUF_decompress4X1(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* DTable = stackalloc uint[2049];
            DTable[0] = (uint)(12 - 1) * 0x01000001;
            memset(DTable + 1, 0, sizeof(uint) * 2048);
            return HUF_decompress4X1_DCtx(DTable, dst, dstSize, cSrc, cSrcSize);
        }

        public static nuint HUF_decompress4X2_DCtx(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress4X2_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress4X2(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* DTable = stackalloc uint[4097];
            DTable[0] = (uint)12 * 0x01000001;
            memset(DTable + 1, 0, sizeof(uint) * 4096);
            return HUF_decompress4X2_DCtx(DTable, dst, dstSize, cSrc, cSrcSize);
        }

        public static delegate* managed<void*, nuint, void*, nuint, nuint>[] decompress = new delegate* managed<void*, nuint, void*, nuint, nuint>[2] { &HUF_decompress4X1, &HUF_decompress4X2 };
        /** HUF_decompress() :
         *  Decompress HUF data from buffer 'cSrc', of size 'cSrcSize',
         *  into already allocated buffer 'dst', of minimum size 'dstSize'.
         * `originalSize` : **must** be the ***exact*** size of original (uncompressed) data.
         *  Note : in contrast with FSE, HUF_decompress can regenerate
         *         RLE (cSrcSize==1) and uncompressed (cSrcSize==dstSize) data,
         *         because it knows size to regenerate (originalSize).
         * @return : size of regenerated data (== originalSize),
         *           or an error code, which can be tested using HUF_isError()
         */
        public static nuint HUF_decompress(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            if (dstSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            if (cSrcSize > dstSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            if (cSrcSize == dstSize)
            {
                memcpy(dst, cSrc, (uint)dstSize);
                return dstSize;
            }

            if (cSrcSize == 1)
            {
                memset(dst, *(byte*)cSrc, (uint)dstSize);
                return dstSize;
            }

            {
                uint algoNb = HUF_selectDecoder(dstSize, cSrcSize);
                return decompress[algoNb](dst, dstSize, cSrc, cSrcSize);
            }
        }

        public static nuint HUF_decompress4X_DCtx(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            if (dstSize == 0)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall));
            if (cSrcSize > dstSize)
                return unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_corruption_detected));
            if (cSrcSize == dstSize)
            {
                memcpy(dst, cSrc, (uint)dstSize);
                return dstSize;
            }

            if (cSrcSize == 1)
            {
                memset(dst, *(byte*)cSrc, (uint)dstSize);
                return dstSize;
            }

            {
                uint algoNb = HUF_selectDecoder(dstSize, cSrcSize);
                return algoNb != 0 ? HUF_decompress4X2_DCtx(dctx, dst, dstSize, cSrc, cSrcSize) : HUF_decompress4X1_DCtx(dctx, dst, dstSize, cSrc, cSrcSize);
            }
        }

        public static nuint HUF_decompress4X_hufOnly(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress4X_hufOnly_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }

        public static nuint HUF_decompress1X_DCtx(uint* dctx, void* dst, nuint dstSize, void* cSrc, nuint cSrcSize)
        {
            uint* workSpace = stackalloc uint[640];
            return HUF_decompress1X_DCtx_wksp(dctx, dst, dstSize, cSrc, cSrcSize, workSpace, sizeof(uint) * 640);
        }
    }
}