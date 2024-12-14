using System;
using System.IO;
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

        static ExternMethods()
        {
            // todo simple way to load different native libraries
            var currentDirectory = Directory.GetCurrentDirectory();
            try
            {
#if NETFRAMEWORK
                var platform = Environment.Is64BitProcess ? "x64" : "x86";
#else
                var platform = RuntimeInformation.ProcessArchitecture switch
                {
                    Architecture.X86 => "x86",
                    Architecture.X64 => "x64",
                    Architecture.Arm => "arm",
                    Architecture.Arm64 => "arm64",
                    _ => throw new PlatformNotSupportedException(),
                };
#endif

                if (Directory.Exists(platform))
                {
                    Directory.SetCurrentDirectory(platform);
                }

                // load library
                ZSTD_versionNumber();
            }
            finally
            {
                Directory.SetCurrentDirectory(currentDirectory);
            }
        }
    }
}
