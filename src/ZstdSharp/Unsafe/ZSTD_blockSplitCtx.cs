using System;

namespace ZstdSharp.Unsafe
{
    public unsafe partial struct ZSTD_blockSplitCtx
    {
        public seqStore_t fullSeqStoreChunk;

        public seqStore_t firstHalfSeqStore;

        public seqStore_t secondHalfSeqStore;

        public seqStore_t currSeqStore;

        public seqStore_t nextSeqStore;

        public fixed uint partitions[196];

        public ZSTD_entropyCTablesMetadata_t entropyMetadata;
    }
}
