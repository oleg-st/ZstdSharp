using System;
using System.Runtime.InteropServices;

namespace Zstd.Extern
{
    public class ExternMethods
    {
        private const string DllName = "libzstd.dll";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createCCtx();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_freeCCtx(IntPtr cctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_compressCCtx(IntPtr ctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize, int compressionLevel);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr ZSTD_createDCtx();
        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_freeDCtx(IntPtr cctx);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_decompressDCtx(IntPtr ctx, IntPtr dst, nuint dstCapacity, IntPtr src, nuint srcSize);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern nuint ZSTD_compressBound(nuint srcSize);
    }
}
