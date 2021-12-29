using System;

namespace ZstdSharp.Unsafe
{
    /**
     * This struct contains the functions necessary for lazy to search.
     * Currently, that is only searchMax. However, it is still valuable to have the
     * VTable because this makes it easier to add more functions to the VTable later.
     *
     * TODO: The start of the search function involves loading and calculating a
     * bunch of constants from the ZSTD_matchState_t. These computations could be
     * done in an initialization function, and saved somewhere in the match state.
     * Then we could pass a pointer to the saved state instead of the match state,
     * and avoid duplicate computations.
     *
     * TODO: Move the match re-winding into searchMax. This improves compression
     * ratio, and unlocks further simplifications with the next TODO.
     *
     * TODO: Try moving the repcode search into searchMax. After the re-winding
     * and repcode search are in searchMax, there is no more logic in the match
     * finder loop that requires knowledge about the dictMode. So we should be
     * able to avoid force inlining it, and we can join the extDict loop with
     * the single segment loop. It should go in searchMax instead of its own
     * function to avoid having multiple virtual function calls per search.
     */
    public partial struct ZSTD_LazyVTable
    {
        public searchMax_f searchMax;

        public ZSTD_LazyVTable(searchMax_f searchMax)
        {
            this.searchMax = searchMax;
        }
    }
}
