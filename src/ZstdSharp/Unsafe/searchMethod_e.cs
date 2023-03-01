namespace ZstdSharp.Unsafe
{
    /* Generate hash chain search fns for each combination of (dictMode, mls) */
    public enum searchMethod_e
    {
        search_hashChain = 0,
        search_binaryTree = 1,
        search_rowHash = 2
    }
}