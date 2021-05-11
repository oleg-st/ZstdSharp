using System;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct ZSTD_matchState_t
    {
        /* State for window round buffer management */
        public ZSTD_window_t window;

        /* index of end of dictionary, within context's referential.
                                     * When loadedDictEnd != 0, a dictionary is in use, and still valid.
                                     * This relies on a mechanism to set loadedDictEnd=0 when dictionary is no longer within distance.
                                     * Such mechanism is provided within ZSTD_window_enforceMaxDist() and ZSTD_checkDictValidity().
                                     * When dict referential is copied into active context (i.e. not attached),
                                     * loadedDictEnd == dictSize, since referential starts from zero.
                                     */
        public uint loadedDictEnd;

        /* index from which to continue table update */
        public uint nextToUpdate;

        /* dispatch table for matches of len==3 : larger == faster, more memory */
        public uint hashLog3;

        public uint* hashTable;

        public uint* hashTable3;

        public uint* chainTable;

        /* Indicates whether this matchState is using the
                                       * dedicated dictionary search structure.
                                       */
        public int dedicatedDictSearch;

        /* optimal parser state */
        public optState_t opt;

        public ZSTD_matchState_t* dictMatchState;

        public ZSTD_compressionParameters cParams;

        public rawSeqStore_t* ldmSeqStore;
    }
}
