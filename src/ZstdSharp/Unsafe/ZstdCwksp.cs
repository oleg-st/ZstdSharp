using System;
using System.Runtime.CompilerServices;
using static ZstdSharp.UnsafeHelper;

namespace ZstdSharp.Unsafe
{
    public static unsafe partial class Methods
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_assert_internal_consistency(ZSTD_cwksp* ws)
        {
            assert(ws->workspace <= ws->objectEnd);
            assert(ws->objectEnd <= ws->tableEnd);
            assert(ws->objectEnd <= ws->tableValidEnd);
            assert(ws->tableEnd <= ws->allocStart);
            assert(ws->tableValidEnd <= ws->allocStart);
            assert(ws->allocStart <= ws->workspaceEnd);
        }

        /**
         * Align must be a power of 2.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_align(nuint size, nuint align)
        {
            nuint mask = align - 1;

            assert((align & mask) == 0);
            return (size + mask) & ~mask;
        }

        /**
         * Use this to determine how much space in the workspace we will consume to
         * allocate this object. (Normally it should be exactly the size of the object,
         * but under special conditions, like ASAN, where we pad each object, it might
         * be larger.)
         *
         * Since tables aren't currently redzoned, you don't need to call through this
         * to figure out how much space you need for the matchState tables. Everything
         * else is though.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_alloc_size(nuint size)
        {
            if (size == 0)
            {
                return 0;
            }

            return size;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_internal_advance_phase(ZSTD_cwksp* ws, ZSTD_cwksp_alloc_phase_e phase)
        {
            assert(phase >= ws->phase);
            if (phase > ws->phase)
            {
                if (ws->phase < ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_buffers && phase >= ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_buffers)
                {
                    ws->tableValidEnd = ws->objectEnd;
                }

                if (ws->phase < ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_aligned && phase >= ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_aligned)
                {
                    ws->allocStart = (byte*)(ws->allocStart) - ((nuint)(ws->allocStart) & ((nuint)(sizeof(uint)) - 1));
                    if (ws->allocStart < ws->tableValidEnd)
                    {
                        ws->tableValidEnd = ws->allocStart;
                    }
                }

                ws->phase = phase;
            }
        }

        /**
         * Returns whether this object/buffer/etc was allocated in this workspace.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZSTD_cwksp_owns_buffer(ZSTD_cwksp* ws, void* ptr)
        {
            return (((ptr != null) && (ws->workspace <= ptr) && (ptr <= ws->workspaceEnd)) ? 1 : 0);
        }

        /**
         * Internal function. Do not use directly.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_cwksp_reserve_internal(ZSTD_cwksp* ws, nuint bytes, ZSTD_cwksp_alloc_phase_e phase)
        {
            void* alloc;
            void* bottom = ws->tableEnd;

            ZSTD_cwksp_internal_advance_phase(ws, phase);
            alloc = (byte*)(ws->allocStart) - bytes;
            if (bytes == 0)
            {
                return null;
            }

            ZSTD_cwksp_assert_internal_consistency(ws);
            assert(alloc >= bottom);
            if (alloc < bottom)
            {
                ws->allocFailed = 1;
                return null;
            }

            if (alloc < ws->tableValidEnd)
            {
                ws->tableValidEnd = alloc;
            }

            ws->allocStart = alloc;
            return alloc;
        }

        /**
         * Reserves and returns unaligned memory.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* ZSTD_cwksp_reserve_buffer(ZSTD_cwksp* ws, nuint bytes)
        {
            return (byte*)(ZSTD_cwksp_reserve_internal(ws, bytes, ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_buffers));
        }

        /**
         * Reserves and returns memory sized on and aligned on sizeof(unsigned).
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_cwksp_reserve_aligned(ZSTD_cwksp* ws, nuint bytes)
        {
            assert((bytes & ((nuint)(sizeof(uint)) - 1)) == 0);
            return ZSTD_cwksp_reserve_internal(ws, ZSTD_cwksp_align(bytes, (nuint)(sizeof(uint))), ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_aligned);
        }

        /**
         * Aligned on sizeof(unsigned). These buffers have the special property that
         * their values remain constrained, allowing us to re-use them without
         * memset()-ing them.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_cwksp_reserve_table(ZSTD_cwksp* ws, nuint bytes)
        {
            ZSTD_cwksp_alloc_phase_e phase = ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_aligned;
            void* alloc = ws->tableEnd;
            void* end = (void*)((byte*)(alloc) + bytes);
            void* top = ws->allocStart;

            assert((bytes & ((nuint)(sizeof(uint)) - 1)) == 0);
            ZSTD_cwksp_internal_advance_phase(ws, phase);
            ZSTD_cwksp_assert_internal_consistency(ws);
            assert(end <= top);
            if (end > top)
            {
                ws->allocFailed = 1;
                return null;
            }

            ws->tableEnd = end;
            return alloc;
        }

        /**
         * Aligned on sizeof(void*).
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void* ZSTD_cwksp_reserve_object(ZSTD_cwksp* ws, nuint bytes)
        {
            nuint roundedBytes = ZSTD_cwksp_align(bytes, (nuint)(sizeof(void*)));
            void* alloc = ws->objectEnd;
            void* end = (void*)((byte*)(alloc) + roundedBytes);

            assert(((nuint)(alloc) & ((nuint)(sizeof(void*)) - 1)) == 0);
            assert((bytes & ((nuint)(sizeof(void*)) - 1)) == 0);
            ZSTD_cwksp_assert_internal_consistency(ws);
            if (ws->phase != ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_objects || end > ws->workspaceEnd)
            {
                ws->allocFailed = 1;
                return null;
            }

            ws->objectEnd = end;
            ws->tableEnd = end;
            ws->tableValidEnd = end;
            return alloc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_mark_tables_dirty(ZSTD_cwksp* ws)
        {
            assert(ws->tableValidEnd >= ws->objectEnd);
            assert(ws->tableValidEnd <= ws->allocStart);
            ws->tableValidEnd = ws->objectEnd;
            ZSTD_cwksp_assert_internal_consistency(ws);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_mark_tables_clean(ZSTD_cwksp* ws)
        {
            assert(ws->tableValidEnd >= ws->objectEnd);
            assert(ws->tableValidEnd <= ws->allocStart);
            if (ws->tableValidEnd < ws->tableEnd)
            {
                ws->tableValidEnd = ws->tableEnd;
            }

            ZSTD_cwksp_assert_internal_consistency(ws);
        }

        /**
         * Zero the part of the allocated tables not already marked clean.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_clean_tables(ZSTD_cwksp* ws)
        {
            assert(ws->tableValidEnd >= ws->objectEnd);
            assert(ws->tableValidEnd <= ws->allocStart);
            if (ws->tableValidEnd < ws->tableEnd)
            {
                memset((ws->tableValidEnd), (0), (nuint)(((byte*)(ws->tableEnd) - (byte*)(ws->tableValidEnd))));
            }

            ZSTD_cwksp_mark_tables_clean(ws);
        }

        /**
         * Invalidates table allocations.
         * All other allocations remain valid.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_clear_tables(ZSTD_cwksp* ws)
        {
            ws->tableEnd = ws->objectEnd;
            ZSTD_cwksp_assert_internal_consistency(ws);
        }

        /**
         * Invalidates all buffer, aligned, and table allocations.
         * Object allocations remain valid.
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_clear(ZSTD_cwksp* ws)
        {
            ws->tableEnd = ws->objectEnd;
            ws->allocStart = ws->workspaceEnd;
            ws->allocFailed = 0;
            if (ws->phase > ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_buffers)
            {
                ws->phase = ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_buffers;
            }

            ZSTD_cwksp_assert_internal_consistency(ws);
        }

        /**
         * The provided workspace takes ownership of the buffer [start, start+size).
         * Any existing values in the workspace are ignored (the previously managed
         * buffer, if present, must be separately freed).
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_init(ZSTD_cwksp* ws, void* start, nuint size, ZSTD_cwksp_static_alloc_e isStatic)
        {
            assert(((nuint)(start) & ((nuint)(sizeof(void*)) - 1)) == 0);
            ws->workspace = start;
            ws->workspaceEnd = (byte*)(start) + size;
            ws->objectEnd = ws->workspace;
            ws->tableValidEnd = ws->objectEnd;
            ws->phase = ZSTD_cwksp_alloc_phase_e.ZSTD_cwksp_alloc_objects;
            ws->isStatic = isStatic;
            ZSTD_cwksp_clear(ws);
            ws->workspaceOversizedDuration = 0;
            ZSTD_cwksp_assert_internal_consistency(ws);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_create(ZSTD_cwksp* ws, nuint size, ZSTD_customMem customMem)
        {
            void* workspace = ZSTD_customMalloc(size, customMem);

            if (workspace == null)
            {
                return (unchecked((nuint)(-(int)ZSTD_ErrorCode.ZSTD_error_memory_allocation)));
            }

            ZSTD_cwksp_init(ws, workspace, size, ZSTD_cwksp_static_alloc_e.ZSTD_cwksp_dynamic_alloc);
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_free(ZSTD_cwksp* ws, ZSTD_customMem customMem)
        {
            void* ptr = ws->workspace;

            memset((void*)(ws), (0), ((nuint)(sizeof(ZSTD_cwksp))));
            ZSTD_customFree(ptr, customMem);
        }

        /**
         * Moves the management of a workspace from one cwksp to another. The src cwksp
         * is left in an invalid state (src must be re-init()'ed before it's used again).
         */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_move(ZSTD_cwksp* dst, ZSTD_cwksp* src)
        {
            *dst = *src;
            memset((void*)(src), (0), ((nuint)(sizeof(ZSTD_cwksp))));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_sizeof(ZSTD_cwksp* ws)
        {
            return (nuint)((byte*)(ws->workspaceEnd) - (byte*)(ws->workspace));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_used(ZSTD_cwksp* ws)
        {
            return (nuint)((byte*)(ws->tableEnd) - (byte*)(ws->workspace)) + (nuint)((byte*)(ws->workspaceEnd) - (byte*)(ws->allocStart));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZSTD_cwksp_reserve_failed(ZSTD_cwksp* ws)
        {
            return (int)ws->allocFailed;
        }

        /*-*************************************
        *  Functions Checking Free Space
        ***************************************/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static nuint ZSTD_cwksp_available_space(ZSTD_cwksp* ws)
        {
            return (nuint)((byte*)(ws->allocStart) - (byte*)(ws->tableEnd));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZSTD_cwksp_check_available(ZSTD_cwksp* ws, nuint additionalNeededSpace)
        {
            return ((ZSTD_cwksp_available_space(ws) >= additionalNeededSpace) ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZSTD_cwksp_check_too_large(ZSTD_cwksp* ws, nuint additionalNeededSpace)
        {
            return ZSTD_cwksp_check_available(ws, additionalNeededSpace * 3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ZSTD_cwksp_check_wasteful(ZSTD_cwksp* ws, nuint additionalNeededSpace)
        {
            return (((ZSTD_cwksp_check_too_large(ws, additionalNeededSpace)) != 0 && ws->workspaceOversizedDuration > 128) ? 1 : 0);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ZSTD_cwksp_bump_oversized_duration(ZSTD_cwksp* ws, nuint additionalNeededSpace)
        {
            if ((ZSTD_cwksp_check_too_large(ws, additionalNeededSpace)) != 0)
            {
                ws->workspaceOversizedDuration++;
            }
            else
            {
                ws->workspaceOversizedDuration = 0;
            }
        }
    }
}
