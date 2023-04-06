using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        /* custom memory allocation functions */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_customMalloc(nuint size, ZSTD_customMem customMem)
        {
            if (customMem.customAlloc != null)
                return customMem.customAlloc(customMem.opaque, size);
            return malloc(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_customCalloc(nuint size, ZSTD_customMem customMem)
        {
            if (customMem.customAlloc != null)
            {
                /* calloc implemented as malloc+memset;
                 * not as efficient as calloc, but next best guess for custom malloc */
                void* ptr = customMem.customAlloc(customMem.opaque, size);
                memset(ptr, 0, (uint)size);
                return ptr;
            }

            return calloc(1, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_customFree(void* ptr, ZSTD_customMem customMem)
        {
            if (ptr != null)
            {
                if (customMem.customFree != null)
                    customMem.customFree(customMem.opaque, ptr);
                else
                    free(ptr);
            }
        }
    }
}