using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        public static void ZSTD_fillHashTable(ZSTD_matchState_t* ms, void* end, ZSTD_dictTableLoadMethod_e dtlm)
        {
            ZSTD_compressionParameters* cParams = &ms->cParams;
            uint* hashTable = ms->hashTable;
            uint hBits = cParams->hashLog;
            uint mls = cParams->minMatch;
            byte* @base = ms->window.@base;
            byte* ip = @base + ms->nextToUpdate;
            byte* iend = (byte*)end - 8;
            const uint fastHashFillStep = 3;
            for (; ip + fastHashFillStep < iend + 2; ip += fastHashFillStep)
            {
                uint curr = (uint)(ip - @base);
                nuint hash0 = ZSTD_hashPtr(ip, hBits, mls);
                hashTable[hash0] = curr;
                if (dtlm == ZSTD_dictTableLoadMethod_e.ZSTD_dtlm_fast)
                    continue;
                {
                    uint p;
                    for (p = 1; p < fastHashFillStep; ++p)
                    {
                        nuint hash = ZSTD_hashPtr(ip + p, hBits, mls);
                        if (hashTable[hash] == 0)
                        {
                            hashTable[hash] = curr + p;
                        }
                    }
                }
            }
        }

        /**
         * If you squint hard enough (and ignore repcodes), the search operation at any
         * given position is broken into 4 stages:
         *
         * 1. Hash   (map position to hash value via input read)
         * 2. Lookup (map hash val to index via hashtable read)
         * 3. Load   (map index to value at that position via input read)
         * 4. Compare
         *
         * Each of these steps involves a memory read at an address which is computed
         * from the previous step. This means these steps must be sequenced and their
         * latencies are cumulative.
         *
         * Rather than do 1->2->3->4 sequentially for a single position before moving
         * onto the next, this implementation interleaves these operations across the
         * next few positions:
         *
         * R = Repcode Read & Compare
         * H = Hash
         * T = Table Lookup
         * M = Match Read & Compare
         *
         * Pos | Time -->
         * ----+-------------------
         * N   | ... M
         * N+1 | ...   TM
         * N+2 |    R H   T M
         * N+3 |         H    TM
         * N+4 |           R H   T M
         * N+5 |                H   ...
         * N+6 |                  R ...
         *
         * This is very much analogous to the pipelining of execution in a CPU. And just
         * like a CPU, we have to dump the pipeline when we find a match (i.e., take a
         * branch).
         *
         * When this happens, we throw away our current state, and do the following prep
         * to re-enter the loop:
         *
         * Pos | Time -->
         * ----+-------------------
         * N   | H T
         * N+1 |  H
         *
         * This is also the work we do at the beginning to enter the loop initially.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [InlineMethod.Inline]
        private static nuint ZSTD_compressBlock_fast_noDict_generic(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize, uint mls, uint hasStep)
        {
            ZSTD_compressionParameters* cParams = &ms->cParams;
            uint* hashTable = ms->hashTable;
            uint hlog = cParams->hashLog;
            /* support stepSize of 0 */
            nuint stepSize = hasStep != 0 ? cParams->targetLength + (uint)(cParams->targetLength == 0 ? 1 : 0) + 1 : 2;
            byte* @base = ms->window.@base;
            byte* istart = (byte*)src;
            uint endIndex = (uint)((nuint)(istart - @base) + srcSize);
            uint prefixStartIndex = ZSTD_getLowestPrefixIndex(ms, endIndex, cParams->windowLog);
            byte* prefixStart = @base + prefixStartIndex;
            byte* iend = istart + srcSize;
            byte* ilimit = iend - 8;
            byte* anchor = istart;
            byte* ip0 = istart;
            byte* ip1;
            byte* ip2;
            byte* ip3;
            uint current0;
            uint rep_offset1 = rep[0];
            uint rep_offset2 = rep[1];
            uint offsetSaved = 0;
            /* hash for ip0 */
            nuint hash0;
            /* hash for ip1 */
            nuint hash1;
            /* match idx for ip0 */
            uint idx;
            /* src value at match idx */
            uint mval;
            uint offcode;
            byte* match0;
            nuint mLength;
            /* ip0 and ip1 are always adjacent. The targetLength skipping and
             * uncompressibility acceleration is applied to every other position,
             * matching the behavior of #1562. step therefore represents the gap
             * between pairs of positions, from ip0 to ip2 or ip1 to ip3. */
            nuint step;
            byte* nextStep;
            const nuint kStepIncr = 1 << 8 - 1;
            ip0 += ip0 == prefixStart ? 1 : 0;
            {
                uint curr = (uint)(ip0 - @base);
                uint windowLow = ZSTD_getLowestPrefixIndex(ms, curr, cParams->windowLog);
                uint maxRep = curr - windowLow;
                if (rep_offset2 > maxRep)
                {
                    offsetSaved = rep_offset2;
                    rep_offset2 = 0;
                }

                if (rep_offset1 > maxRep)
                {
                    offsetSaved = rep_offset1;
                    rep_offset1 = 0;
                }
            }

        _start:
            step = stepSize;
            nextStep = ip0 + kStepIncr;
            ip1 = ip0 + 1;
            ip2 = ip0 + step;
            ip3 = ip2 + 1;
            if (ip3 >= ilimit)
            {
                goto _cleanup;
            }

            hash0 = ZSTD_hashPtr(ip0, hlog, mls);
            hash1 = ZSTD_hashPtr(ip1, hlog, mls);
            idx = hashTable[hash0];
            do
            {
                /* load repcode match for ip[2]*/
                uint rval = MEM_read32(ip2 - rep_offset1);
                current0 = (uint)(ip0 - @base);
                hashTable[hash0] = current0;
                if (MEM_read32(ip2) == rval && rep_offset1 > 0)
                {
                    ip0 = ip2;
                    match0 = ip0 - rep_offset1;
                    mLength = ip0[-1] == match0[-1] ? 1U : 0U;
                    ip0 -= mLength;
                    match0 -= mLength;
                    assert(1 >= 1);
                    assert(1 <= 3);
                    offcode = 1 - 1;
                    mLength += 4;
                    goto _match;
                }

                if (idx >= prefixStartIndex)
                {
                    mval = MEM_read32(@base + idx);
                }
                else
                {
                    mval = MEM_read32(ip0) ^ 1;
                }

                if (MEM_read32(ip0) == mval)
                {
                    goto _offset;
                }

                idx = hashTable[hash1];
                hash0 = hash1;
                hash1 = ZSTD_hashPtr(ip2, hlog, mls);
                ip0 = ip1;
                ip1 = ip2;
                ip2 = ip3;
                current0 = (uint)(ip0 - @base);
                hashTable[hash0] = current0;
                if (idx >= prefixStartIndex)
                {
                    mval = MEM_read32(@base + idx);
                }
                else
                {
                    mval = MEM_read32(ip0) ^ 1;
                }

                if (MEM_read32(ip0) == mval)
                {
                    goto _offset;
                }

                idx = hashTable[hash1];
                hash0 = hash1;
                hash1 = ZSTD_hashPtr(ip2, hlog, mls);
                ip0 = ip1;
                ip1 = ip2;
                ip2 = ip0 + step;
                ip3 = ip1 + step;
                if (ip2 >= nextStep)
                {
                    step++;
                    Prefetch0(ip1 + 64);
                    Prefetch0(ip1 + 128);
                    nextStep += kStepIncr;
                }
            }
            while (ip3 < ilimit);
        _cleanup:
            rep[0] = rep_offset1 != 0 ? rep_offset1 : offsetSaved;
            rep[1] = rep_offset2 != 0 ? rep_offset2 : offsetSaved;
            return (nuint)(iend - anchor);
        _offset:
            match0 = @base + idx;
            rep_offset2 = rep_offset1;
            rep_offset1 = (uint)(ip0 - match0);
            assert(rep_offset1 > 0);
            offcode = rep_offset1 + (3 - 1);
            mLength = 4;
            while (ip0 > anchor && match0 > prefixStart && ip0[-1] == match0[-1])
            {
                ip0--;
                match0--;
                mLength++;
            }

        _match:
            mLength += ZSTD_count(ip0 + mLength, match0 + mLength, iend);
            ZSTD_storeSeq(seqStore, (nuint)(ip0 - anchor), anchor, iend, offcode, mLength);
            ip0 += mLength;
            anchor = ip0;
            if (ip1 < ip0)
            {
                hashTable[hash1] = (uint)(ip1 - @base);
            }

            if (ip0 <= ilimit)
            {
                assert(@base + current0 + 2 > istart);
                hashTable[ZSTD_hashPtr(@base + current0 + 2, hlog, mls)] = current0 + 2;
                hashTable[ZSTD_hashPtr(ip0 - 2, hlog, mls)] = (uint)(ip0 - 2 - @base);
                if (rep_offset2 > 0)
                {
                    while (ip0 <= ilimit && MEM_read32(ip0) == MEM_read32(ip0 - rep_offset2))
                    {
                        /* store sequence */
                        nuint rLength = ZSTD_count(ip0 + 4, ip0 + 4 - rep_offset2, iend) + 4;
                        {
                            /* swap rep_offset2 <=> rep_offset1 */
                            uint tmpOff = rep_offset2;
                            rep_offset2 = rep_offset1;
                            rep_offset1 = tmpOff;
                        }

                        hashTable[ZSTD_hashPtr(ip0, hlog, mls)] = (uint)(ip0 - @base);
                        ip0 += rLength;
                        assert(1 >= 1);
                        assert(1 <= 3);
                        ZSTD_storeSeq(seqStore, 0, anchor, iend, 1 - 1, rLength);
                        anchor = ip0;
                        continue;
                    }
                }
            }

            goto _start;
        }

        private static nuint ZSTD_compressBlock_fast_noDict_4_1(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 4, 1);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_5_1(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 5, 1);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_6_1(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 6, 1);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_7_1(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 7, 1);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_4_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 4, 0);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_5_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 5, 0);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_6_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 6, 0);
        }

        private static nuint ZSTD_compressBlock_fast_noDict_7_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_noDict_generic(ms, seqStore, rep, src, srcSize, 7, 0);
        }

        public static nuint ZSTD_compressBlock_fast(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            uint mls = ms->cParams.minMatch;
            assert(ms->dictMatchState == null);
            if (ms->cParams.targetLength > 1)
            {
                switch (mls)
                {
                    default:
                    case 4:
                        return ZSTD_compressBlock_fast_noDict_4_1(ms, seqStore, rep, src, srcSize);
                    case 5:
                        return ZSTD_compressBlock_fast_noDict_5_1(ms, seqStore, rep, src, srcSize);
                    case 6:
                        return ZSTD_compressBlock_fast_noDict_6_1(ms, seqStore, rep, src, srcSize);
                    case 7:
                        return ZSTD_compressBlock_fast_noDict_7_1(ms, seqStore, rep, src, srcSize);
                }
            }
            else
            {
                switch (mls)
                {
                    default:
                    case 4:
                        return ZSTD_compressBlock_fast_noDict_4_0(ms, seqStore, rep, src, srcSize);
                    case 5:
                        return ZSTD_compressBlock_fast_noDict_5_0(ms, seqStore, rep, src, srcSize);
                    case 6:
                        return ZSTD_compressBlock_fast_noDict_6_0(ms, seqStore, rep, src, srcSize);
                    case 7:
                        return ZSTD_compressBlock_fast_noDict_7_0(ms, seqStore, rep, src, srcSize);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_compressBlock_fast_dictMatchState_generic(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize, uint mls, uint hasStep)
        {
            ZSTD_compressionParameters* cParams = &ms->cParams;
            uint* hashTable = ms->hashTable;
            uint hlog = cParams->hashLog;
            /* support stepSize of 0 */
            uint stepSize = cParams->targetLength + (uint)(cParams->targetLength == 0 ? 1 : 0);
            byte* @base = ms->window.@base;
            byte* istart = (byte*)src;
            byte* ip = istart;
            byte* anchor = istart;
            uint prefixStartIndex = ms->window.dictLimit;
            byte* prefixStart = @base + prefixStartIndex;
            byte* iend = istart + srcSize;
            byte* ilimit = iend - 8;
            uint offset_1 = rep[0], offset_2 = rep[1];
            uint offsetSaved = 0;
            ZSTD_matchState_t* dms = ms->dictMatchState;
            ZSTD_compressionParameters* dictCParams = &dms->cParams;
            uint* dictHashTable = dms->hashTable;
            uint dictStartIndex = dms->window.dictLimit;
            byte* dictBase = dms->window.@base;
            byte* dictStart = dictBase + dictStartIndex;
            byte* dictEnd = dms->window.nextSrc;
            uint dictIndexDelta = prefixStartIndex - (uint)(dictEnd - dictBase);
            uint dictAndPrefixLength = (uint)(ip - prefixStart + dictEnd - dictStart);
            uint dictHLog = dictCParams->hashLog;
            /* if a dictionary is still attached, it necessarily means that
             * it is within window size. So we just check it. */
            uint maxDistance = 1U << (int)cParams->windowLog;
            uint endIndex = (uint)((nuint)(ip - @base) + srcSize);
            assert(endIndex - prefixStartIndex <= maxDistance);
            assert(prefixStartIndex >= (uint)(dictEnd - dictBase));
            ip += dictAndPrefixLength == 0 ? 1 : 0;
            assert(offset_1 <= dictAndPrefixLength);
            assert(offset_2 <= dictAndPrefixLength);
            while (ip < ilimit)
            {
                nuint mLength;
                nuint h = ZSTD_hashPtr(ip, hlog, mls);
                uint curr = (uint)(ip - @base);
                uint matchIndex = hashTable[h];
                byte* match = @base + matchIndex;
                uint repIndex = curr + 1 - offset_1;
                byte* repMatch = repIndex < prefixStartIndex ? dictBase + (repIndex - dictIndexDelta) : @base + repIndex;
                hashTable[h] = curr;
                if (prefixStartIndex - 1 - repIndex >= 3 && MEM_read32(repMatch) == MEM_read32(ip + 1))
                {
                    byte* repMatchEnd = repIndex < prefixStartIndex ? dictEnd : iend;
                    mLength = ZSTD_count_2segments(ip + 1 + 4, repMatch + 4, iend, repMatchEnd, prefixStart) + 4;
                    ip++;
                    assert(1 >= 1);
                    assert(1 <= 3);
                    ZSTD_storeSeq(seqStore, (nuint)(ip - anchor), anchor, iend, 1 - 1, mLength);
                }
                else if (matchIndex <= prefixStartIndex)
                {
                    nuint dictHash = ZSTD_hashPtr(ip, dictHLog, mls);
                    uint dictMatchIndex = dictHashTable[dictHash];
                    byte* dictMatch = dictBase + dictMatchIndex;
                    if (dictMatchIndex <= dictStartIndex || MEM_read32(dictMatch) != MEM_read32(ip))
                    {
                        assert(stepSize >= 1);
                        ip += (ulong)(ip - anchor >> 8) + stepSize;
                        continue;
                    }
                    else
                    {
                        /* found a dict match */
                        uint offset = curr - dictMatchIndex - dictIndexDelta;
                        mLength = ZSTD_count_2segments(ip + 4, dictMatch + 4, iend, dictEnd, prefixStart) + 4;
                        while (ip > anchor && dictMatch > dictStart && ip[-1] == dictMatch[-1])
                        {
                            ip--;
                            dictMatch--;
                            mLength++;
                        }

                        offset_2 = offset_1;
                        offset_1 = offset;
                        assert(offset > 0);
                        ZSTD_storeSeq(seqStore, (nuint)(ip - anchor), anchor, iend, offset + (3 - 1), mLength);
                    }
                }
                else if (MEM_read32(match) != MEM_read32(ip))
                {
                    assert(stepSize >= 1);
                    ip += (ulong)(ip - anchor >> 8) + stepSize;
                    continue;
                }
                else
                {
                    /* found a regular match */
                    uint offset = (uint)(ip - match);
                    mLength = ZSTD_count(ip + 4, match + 4, iend) + 4;
                    while (ip > anchor && match > prefixStart && ip[-1] == match[-1])
                    {
                        ip--;
                        match--;
                        mLength++;
                    }

                    offset_2 = offset_1;
                    offset_1 = offset;
                    assert(offset > 0);
                    ZSTD_storeSeq(seqStore, (nuint)(ip - anchor), anchor, iend, offset + (3 - 1), mLength);
                }

                ip += mLength;
                anchor = ip;
                if (ip <= ilimit)
                {
                    assert(@base + curr + 2 > istart);
                    hashTable[ZSTD_hashPtr(@base + curr + 2, hlog, mls)] = curr + 2;
                    hashTable[ZSTD_hashPtr(ip - 2, hlog, mls)] = (uint)(ip - 2 - @base);
                    while (ip <= ilimit)
                    {
                        uint current2 = (uint)(ip - @base);
                        uint repIndex2 = current2 - offset_2;
                        byte* repMatch2 = repIndex2 < prefixStartIndex ? dictBase - dictIndexDelta + repIndex2 : @base + repIndex2;
                        if (prefixStartIndex - 1 - repIndex2 >= 3 && MEM_read32(repMatch2) == MEM_read32(ip))
                        {
                            byte* repEnd2 = repIndex2 < prefixStartIndex ? dictEnd : iend;
                            nuint repLength2 = ZSTD_count_2segments(ip + 4, repMatch2 + 4, iend, repEnd2, prefixStart) + 4;
                            /* swap offset_2 <=> offset_1 */
                            uint tmpOffset = offset_2;
                            offset_2 = offset_1;
                            offset_1 = tmpOffset;
                            assert(1 >= 1);
                            assert(1 <= 3);
                            ZSTD_storeSeq(seqStore, 0, anchor, iend, 1 - 1, repLength2);
                            hashTable[ZSTD_hashPtr(ip, hlog, mls)] = current2;
                            ip += repLength2;
                            anchor = ip;
                            continue;
                        }

                        break;
                    }
                }
            }

            rep[0] = offset_1 != 0 ? offset_1 : offsetSaved;
            rep[1] = offset_2 != 0 ? offset_2 : offsetSaved;
            return (nuint)(iend - anchor);
        }

        private static nuint ZSTD_compressBlock_fast_dictMatchState_4_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_dictMatchState_generic(ms, seqStore, rep, src, srcSize, 4, 0);
        }

        private static nuint ZSTD_compressBlock_fast_dictMatchState_5_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_dictMatchState_generic(ms, seqStore, rep, src, srcSize, 5, 0);
        }

        private static nuint ZSTD_compressBlock_fast_dictMatchState_6_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_dictMatchState_generic(ms, seqStore, rep, src, srcSize, 6, 0);
        }

        private static nuint ZSTD_compressBlock_fast_dictMatchState_7_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_dictMatchState_generic(ms, seqStore, rep, src, srcSize, 7, 0);
        }

        public static nuint ZSTD_compressBlock_fast_dictMatchState(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            uint mls = ms->cParams.minMatch;
            assert(ms->dictMatchState != null);
            switch (mls)
            {
                default:
                case 4:
                    return ZSTD_compressBlock_fast_dictMatchState_4_0(ms, seqStore, rep, src, srcSize);
                case 5:
                    return ZSTD_compressBlock_fast_dictMatchState_5_0(ms, seqStore, rep, src, srcSize);
                case 6:
                    return ZSTD_compressBlock_fast_dictMatchState_6_0(ms, seqStore, rep, src, srcSize);
                case 7:
                    return ZSTD_compressBlock_fast_dictMatchState_7_0(ms, seqStore, rep, src, srcSize);
            }
        }

        private static nuint ZSTD_compressBlock_fast_extDict_generic(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize, uint mls, uint hasStep)
        {
            ZSTD_compressionParameters* cParams = &ms->cParams;
            uint* hashTable = ms->hashTable;
            uint hlog = cParams->hashLog;
            /* support stepSize of 0 */
            uint stepSize = cParams->targetLength + (uint)(cParams->targetLength == 0 ? 1 : 0);
            byte* @base = ms->window.@base;
            byte* dictBase = ms->window.dictBase;
            byte* istart = (byte*)src;
            byte* ip = istart;
            byte* anchor = istart;
            uint endIndex = (uint)((nuint)(istart - @base) + srcSize);
            uint lowLimit = ZSTD_getLowestMatchIndex(ms, endIndex, cParams->windowLog);
            uint dictStartIndex = lowLimit;
            byte* dictStart = dictBase + dictStartIndex;
            uint dictLimit = ms->window.dictLimit;
            uint prefixStartIndex = dictLimit < lowLimit ? lowLimit : dictLimit;
            byte* prefixStart = @base + prefixStartIndex;
            byte* dictEnd = dictBase + prefixStartIndex;
            byte* iend = istart + srcSize;
            byte* ilimit = iend - 8;
            uint offset_1 = rep[0], offset_2 = rep[1];
            if (prefixStartIndex == dictStartIndex)
                return ZSTD_compressBlock_fast(ms, seqStore, rep, src, srcSize);
            while (ip < ilimit)
            {
                nuint h = ZSTD_hashPtr(ip, hlog, mls);
                uint matchIndex = hashTable[h];
                byte* matchBase = matchIndex < prefixStartIndex ? dictBase : @base;
                byte* match = matchBase + matchIndex;
                uint curr = (uint)(ip - @base);
                uint repIndex = curr + 1 - offset_1;
                byte* repBase = repIndex < prefixStartIndex ? dictBase : @base;
                byte* repMatch = repBase + repIndex;
                hashTable[h] = curr;
                if (prefixStartIndex - 1 - repIndex >= 3 && offset_1 <= curr + 1 - dictStartIndex && MEM_read32(repMatch) == MEM_read32(ip + 1))
                {
                    byte* repMatchEnd = repIndex < prefixStartIndex ? dictEnd : iend;
                    nuint rLength = ZSTD_count_2segments(ip + 1 + 4, repMatch + 4, iend, repMatchEnd, prefixStart) + 4;
                    ip++;
                    assert(1 >= 1);
                    assert(1 <= 3);
                    ZSTD_storeSeq(seqStore, (nuint)(ip - anchor), anchor, iend, 1 - 1, rLength);
                    ip += rLength;
                    anchor = ip;
                }
                else
                {
                    if (matchIndex < dictStartIndex || MEM_read32(match) != MEM_read32(ip))
                    {
                        assert(stepSize >= 1);
                        ip += (ulong)(ip - anchor >> 8) + stepSize;
                        continue;
                    }

                    {
                        byte* matchEnd = matchIndex < prefixStartIndex ? dictEnd : iend;
                        byte* lowMatchPtr = matchIndex < prefixStartIndex ? dictStart : prefixStart;
                        uint offset = curr - matchIndex;
                        nuint mLength = ZSTD_count_2segments(ip + 4, match + 4, iend, matchEnd, prefixStart) + 4;
                        while (ip > anchor && match > lowMatchPtr && ip[-1] == match[-1])
                        {
                            ip--;
                            match--;
                            mLength++;
                        }

                        offset_2 = offset_1;
                        offset_1 = offset;
                        assert(offset > 0);
                        ZSTD_storeSeq(seqStore, (nuint)(ip - anchor), anchor, iend, offset + (3 - 1), mLength);
                        ip += mLength;
                        anchor = ip;
                    }
                }

                if (ip <= ilimit)
                {
                    hashTable[ZSTD_hashPtr(@base + curr + 2, hlog, mls)] = curr + 2;
                    hashTable[ZSTD_hashPtr(ip - 2, hlog, mls)] = (uint)(ip - 2 - @base);
                    while (ip <= ilimit)
                    {
                        uint current2 = (uint)(ip - @base);
                        uint repIndex2 = current2 - offset_2;
                        byte* repMatch2 = repIndex2 < prefixStartIndex ? dictBase + repIndex2 : @base + repIndex2;
                        if (prefixStartIndex - 1 - repIndex2 >= 3 && offset_2 <= curr - dictStartIndex && MEM_read32(repMatch2) == MEM_read32(ip))
                        {
                            byte* repEnd2 = repIndex2 < prefixStartIndex ? dictEnd : iend;
                            nuint repLength2 = ZSTD_count_2segments(ip + 4, repMatch2 + 4, iend, repEnd2, prefixStart) + 4;
                            {
                                /* swap offset_2 <=> offset_1 */
                                uint tmpOffset = offset_2;
                                offset_2 = offset_1;
                                offset_1 = tmpOffset;
                            }

                            assert(1 >= 1);
                            assert(1 <= 3);
                            ZSTD_storeSeq(seqStore, 0, anchor, iend, 1 - 1, repLength2);
                            hashTable[ZSTD_hashPtr(ip, hlog, mls)] = current2;
                            ip += repLength2;
                            anchor = ip;
                            continue;
                        }

                        break;
                    }
                }
            }

            rep[0] = offset_1;
            rep[1] = offset_2;
            return (nuint)(iend - anchor);
        }

        private static nuint ZSTD_compressBlock_fast_extDict_4_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_extDict_generic(ms, seqStore, rep, src, srcSize, 4, 0);
        }

        private static nuint ZSTD_compressBlock_fast_extDict_5_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_extDict_generic(ms, seqStore, rep, src, srcSize, 5, 0);
        }

        private static nuint ZSTD_compressBlock_fast_extDict_6_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_extDict_generic(ms, seqStore, rep, src, srcSize, 6, 0);
        }

        private static nuint ZSTD_compressBlock_fast_extDict_7_0(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            return ZSTD_compressBlock_fast_extDict_generic(ms, seqStore, rep, src, srcSize, 7, 0);
        }

        public static nuint ZSTD_compressBlock_fast_extDict(ZSTD_matchState_t* ms, seqStore_t* seqStore, uint* rep, void* src, nuint srcSize)
        {
            uint mls = ms->cParams.minMatch;
            switch (mls)
            {
                default:
                case 4:
                    return ZSTD_compressBlock_fast_extDict_4_0(ms, seqStore, rep, src, srcSize);
                case 5:
                    return ZSTD_compressBlock_fast_extDict_5_0(ms, seqStore, rep, src, srcSize);
                case 6:
                    return ZSTD_compressBlock_fast_extDict_6_0(ms, seqStore, rep, src, srcSize);
                case 7:
                    return ZSTD_compressBlock_fast_extDict_7_0(ms, seqStore, rep, src, srcSize);
            }
        }
    }
}