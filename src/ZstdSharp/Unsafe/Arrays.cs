using System;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        static readonly uint* rtbTable = GetArrayPointer(new uint[8] {
            0,
            473195,
            504333,
            520860,
            550000,
            700000,
            750000,
            830000,
        });
        static readonly byte* LL_Code = GetArrayPointer(new byte[64] 
        {
            0,
            1,
            2,
            3,
            4,
            5,
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
            16,
            17,
            17,
            18,
            18,
            19,
            19,
            20,
            20,
            20,
            20,
            21,
            21,
            21,
            21,
            22,
            22,
            22,
            22,
            22,
            22,
            22,
            22,
            23,
            23,
            23,
            23,
            23,
            23,
            23,
            23,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
            24,
        });
        static readonly byte* ML_Code = GetArrayPointer(new byte[128] 
        {
            0,
            1,
            2,
            3,
            4,
            5,
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
            17,
            18,
            19,
            20,
            21,
            22,
            23,
            24,
            25,
            26,
            27,
            28,
            29,
            30,
            31,
            32,
            32,
            33,
            33,
            34,
            34,
            35,
            35,
            36,
            36,
            36,
            36,
            37,
            37,
            37,
            37,
            38,
            38,
            38,
            38,
            38,
            38,
            38,
            38,
            39,
            39,
            39,
            39,
            39,
            39,
            39,
            39,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            40,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            41,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
            42,
        });
        static readonly ulong* srcSizeTiers = GetArrayPointer(new ulong[4] 
        {
            16 * (1 << 10),
            128 * (1 << 10),
            256 * (1 << 10),
            (unchecked(0UL - 1)),
        });
        static readonly ZSTD_blockCompressor[][] blockCompressor = new ZSTD_blockCompressor[4][] 
        {
            new ZSTD_blockCompressor[10]
            {
                ZSTD_compressBlock_fast,
                ZSTD_compressBlock_fast,
                ZSTD_compressBlock_doubleFast,
                ZSTD_compressBlock_greedy,
                ZSTD_compressBlock_lazy,
                ZSTD_compressBlock_lazy2,
                ZSTD_compressBlock_btlazy2,
                ZSTD_compressBlock_btopt,
                ZSTD_compressBlock_btultra,
                ZSTD_compressBlock_btultra2,
            },
            new ZSTD_blockCompressor[10]
            {
                ZSTD_compressBlock_fast_extDict,
                ZSTD_compressBlock_fast_extDict,
                ZSTD_compressBlock_doubleFast_extDict,
                ZSTD_compressBlock_greedy_extDict,
                ZSTD_compressBlock_lazy_extDict,
                ZSTD_compressBlock_lazy2_extDict,
                ZSTD_compressBlock_btlazy2_extDict,
                ZSTD_compressBlock_btopt_extDict,
                ZSTD_compressBlock_btultra_extDict,
                ZSTD_compressBlock_btultra_extDict,
            },
            new ZSTD_blockCompressor[10]
            {
                ZSTD_compressBlock_fast_dictMatchState,
                ZSTD_compressBlock_fast_dictMatchState,
                ZSTD_compressBlock_doubleFast_dictMatchState,
                ZSTD_compressBlock_greedy_dictMatchState,
                ZSTD_compressBlock_lazy_dictMatchState,
                ZSTD_compressBlock_lazy2_dictMatchState,
                ZSTD_compressBlock_btlazy2_dictMatchState,
                ZSTD_compressBlock_btopt_dictMatchState,
                ZSTD_compressBlock_btultra_dictMatchState,
                ZSTD_compressBlock_btultra_dictMatchState,
            },
            new ZSTD_blockCompressor[10]
            {
                null,
                null,
                null,
                ZSTD_compressBlock_greedy_dedicatedDictSearch,
                ZSTD_compressBlock_lazy_dedicatedDictSearch,
                ZSTD_compressBlock_lazy2_dedicatedDictSearch,
                null,
                null,
                null,
                null,
            },
        };
        static ZSTD_blockCompressor[][] rowBasedBlockCompressors = new ZSTD_blockCompressor[4][] 
        {
            new ZSTD_blockCompressor[3]
            {
                ZSTD_compressBlock_greedy_row,
                ZSTD_compressBlock_lazy_row,
                ZSTD_compressBlock_lazy2_row,
            },
            new ZSTD_blockCompressor[3]
            {
                ZSTD_compressBlock_greedy_extDict_row,
                ZSTD_compressBlock_lazy_extDict_row,
                ZSTD_compressBlock_lazy2_extDict_row,
            },
            new ZSTD_blockCompressor[3]
            {
                ZSTD_compressBlock_greedy_dictMatchState_row,
                ZSTD_compressBlock_lazy_dictMatchState_row,
                ZSTD_compressBlock_lazy2_dictMatchState_row,
            },
            new ZSTD_blockCompressor[3]
            {
                ZSTD_compressBlock_greedy_dedicatedDictSearch_row,
                ZSTD_compressBlock_lazy_dedicatedDictSearch_row,
                ZSTD_compressBlock_lazy2_dedicatedDictSearch_row,
            },
        };
        static ZSTD_LazyVTable[][] hcVTables = new ZSTD_LazyVTable[4][] 
        {
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_HcFindBestMatch_noDict_4),
                new (ZSTD_HcFindBestMatch_noDict_5),
                new (ZSTD_HcFindBestMatch_noDict_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_HcFindBestMatch_extDict_4),
                new (ZSTD_HcFindBestMatch_extDict_5),
                new (ZSTD_HcFindBestMatch_extDict_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_HcFindBestMatch_dictMatchState_4),
                new (ZSTD_HcFindBestMatch_dictMatchState_5),
                new (ZSTD_HcFindBestMatch_dictMatchState_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_HcFindBestMatch_dedicatedDictSearch_4),
                new (ZSTD_HcFindBestMatch_dedicatedDictSearch_5),
                new (ZSTD_HcFindBestMatch_dedicatedDictSearch_6),
            },
        };
        static ZSTD_LazyVTable[][] btVTables = new ZSTD_LazyVTable[4][] 
        {
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_BtFindBestMatch_noDict_4),
                new (ZSTD_BtFindBestMatch_noDict_5),
                new (ZSTD_BtFindBestMatch_noDict_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_BtFindBestMatch_extDict_4),
                new (ZSTD_BtFindBestMatch_extDict_5),
                new (ZSTD_BtFindBestMatch_extDict_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_BtFindBestMatch_dictMatchState_4),
                new (ZSTD_BtFindBestMatch_dictMatchState_5),
                new (ZSTD_BtFindBestMatch_dictMatchState_6),
            },
            new ZSTD_LazyVTable[3]
            {
                new (ZSTD_BtFindBestMatch_dedicatedDictSearch_4),
                new (ZSTD_BtFindBestMatch_dedicatedDictSearch_5),
                new (ZSTD_BtFindBestMatch_dedicatedDictSearch_6),
            },
        };
        static ZSTD_LazyVTable[][][] rowVTables = new ZSTD_LazyVTable[4][][] 
        {
            new ZSTD_LazyVTable[3][]
            {
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_noDict_4_4),
                    new (ZSTD_RowFindBestMatch_noDict_4_5),
                    new (ZSTD_RowFindBestMatch_noDict_4_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_noDict_5_4),
                    new (ZSTD_RowFindBestMatch_noDict_5_5),
                    new (ZSTD_RowFindBestMatch_noDict_5_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_noDict_6_4),
                    new (ZSTD_RowFindBestMatch_noDict_6_5),
                    new (ZSTD_RowFindBestMatch_noDict_6_6),
                },
            },
            new ZSTD_LazyVTable[3][]
            {
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_extDict_4_4),
                    new (ZSTD_RowFindBestMatch_extDict_4_5),
                    new (ZSTD_RowFindBestMatch_extDict_4_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_extDict_5_4),
                    new (ZSTD_RowFindBestMatch_extDict_5_5),
                    new (ZSTD_RowFindBestMatch_extDict_5_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_extDict_6_4),
                    new (ZSTD_RowFindBestMatch_extDict_6_5),
                    new (ZSTD_RowFindBestMatch_extDict_6_6),
                },
            },
            new ZSTD_LazyVTable[3][]
            {
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dictMatchState_4_4),
                    new (ZSTD_RowFindBestMatch_dictMatchState_4_5),
                    new (ZSTD_RowFindBestMatch_dictMatchState_4_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dictMatchState_5_4),
                    new (ZSTD_RowFindBestMatch_dictMatchState_5_5),
                    new (ZSTD_RowFindBestMatch_dictMatchState_5_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dictMatchState_6_4),
                    new (ZSTD_RowFindBestMatch_dictMatchState_6_5),
                    new (ZSTD_RowFindBestMatch_dictMatchState_6_6),
                },
            },
            new ZSTD_LazyVTable[3][]
            {
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_4_4),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_4_5),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_4_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_5_4),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_5_5),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_5_6),
                },
                new ZSTD_LazyVTable[3]
                {
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_6_4),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_6_5),
                    new (ZSTD_RowFindBestMatch_dedicatedDictSearch_6_6),
                },
            },
        };
        static readonly uint* baseLLfreqs = GetArrayPointer(new uint[36] 
        {
            4,
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
        });
        static readonly uint* baseOFCfreqs = GetArrayPointer(new uint[32] 
        {
            6,
            2,
            1,
            1,
            2,
            3,
            4,
            4,
            4,
            3,
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
        });
        static ZSTD_getAllMatchesFn[][] getAllMatchesFns = new ZSTD_getAllMatchesFn[3][] 
        {
            new ZSTD_getAllMatchesFn[4]
            {
                ZSTD_btGetAllMatches_noDict_3,
                ZSTD_btGetAllMatches_noDict_4,
                ZSTD_btGetAllMatches_noDict_5,
                ZSTD_btGetAllMatches_noDict_6,
            },
            new ZSTD_getAllMatchesFn[4]
            {
                ZSTD_btGetAllMatches_extDict_3,
                ZSTD_btGetAllMatches_extDict_4,
                ZSTD_btGetAllMatches_extDict_5,
                ZSTD_btGetAllMatches_extDict_6,
            },
            new ZSTD_getAllMatchesFn[4]
            {
                ZSTD_btGetAllMatches_dictMatchState_3,
                ZSTD_btGetAllMatches_dictMatchState_4,
                ZSTD_btGetAllMatches_dictMatchState_5,
                ZSTD_btGetAllMatches_dictMatchState_6,
            },
        };
        static decompressionAlgo[] decompress = new decompressionAlgo[2] 
        {
            HUF_decompress4X1,
            HUF_decompress4X2,
        };
        static readonly uint* dec32table = GetArrayPointer(new uint[8] 
        {
            0,
            1,
            2,
            1,
            4,
            4,
            4,
            4,
        });
        static readonly int* dec64table = GetArrayPointer(new int[8] 
        {
            8,
            8,
            8,
            7,
            8,
            9,
            10,
            11,
        });

        private readonly static byte* emptyWindowString = GetArrayPointer(new byte[] {32, 0});
    }
}
