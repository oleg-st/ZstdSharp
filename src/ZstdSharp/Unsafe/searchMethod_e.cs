namespace ZstdSharp.Unsafe
{
    /* *******************************
     *  Common parser - lazy strategy
     *********************************/
    public enum searchMethod_e
    {
        search_hashChain = 0,
        search_binaryTree = 1,
        search_rowHash = 2
    }
}