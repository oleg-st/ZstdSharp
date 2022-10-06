namespace ZstdSharp.Unsafe
{
    public enum ZSTD_sequenceFormat_e
    {
        /* Representation of ZSTD_Sequence has no block delimiters, sequences only */
        ZSTD_sf_noBlockDelimiters = 0,
        /* Representation of ZSTD_Sequence contains explicit block delimiters */
        ZSTD_sf_explicitBlockDelimiters = 1
    }
}