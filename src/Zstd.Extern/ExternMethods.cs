using System;
using System.Runtime.InteropServices;

namespace Zstd.Extern
{
    public class ExternMethods
    {
        private const string DllName = "libzstd";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern uint ZSTD_versionNumber();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createCCtx();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_freeCCtx(IntPtr cctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_compressCCtx(IntPtr ctx, IntPtr dst, nuint dstCapacity, IntPtr src,
            nuint srcSize, int compressionLevel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_compress2(IntPtr ctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createDCtx();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_freeDCtx(IntPtr cctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_decompressDCtx(IntPtr ctx, IntPtr dst, nuint dstCapacity, IntPtr src,
            nuint srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_compressBound(nuint srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_CCtx_setParameter(IntPtr cctx, ZSTD_cParameter param, int value);

        public enum ZSTD_cParameter
        {
            ZSTD_c_compressionLevel = 100,
        }
    }
}
