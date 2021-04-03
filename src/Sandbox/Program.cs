using System;
using System.IO;
using Zstd.Extern;

namespace Sandbox
{
    unsafe class Program
    {
        private static readonly int level = 1;

        static void Main(string[] args)
        {
            Test1();
            Test2();

            Compress();
            Decompress();
        }

        static void Compress()
        {
            var cctx = ZstdSharp.Methods.ZSTD_createCCtx();
            var src = File.ReadAllBytes("dickens");
            var compressed = new byte[ZstdSharp.Methods.ZSTD_compressBound((nuint)src.Length)];
            fixed (byte* srcPtr = src)
            fixed (byte* compressedPtr = compressed)
            {
                var compressedLength = ZstdSharp.Methods.ZSTD_compressCCtx(cctx, compressedPtr, (nuint)compressed.Length, srcPtr, (nuint)src.Length,
                    level);
            }
            ZstdSharp.Methods.ZSTD_freeCCtx(cctx);
        }

        static void Decompress()
        {
            var dctx = ZstdSharp.Methods.ZSTD_createDCtx();
            var src = File.ReadAllBytes("dickens.zst");
            fixed (byte* srcPtr = src)
            {
                var uncompressed = new byte[ZstdSharp.Methods.ZSTD_decompressBound(srcPtr, (nuint) src.Length)];
                fixed (byte* uncompressedPtr = uncompressed)
                {
                    var decompressedLength = ZstdSharp.Methods.ZSTD_decompressDCtx(dctx, uncompressedPtr,
                        (nuint) uncompressed.Length, srcPtr, (nuint) src.Length);
                }
            }

            ZstdSharp.Methods.ZSTD_freeDCtx(dctx);
        }

        static unsafe void Test1()
        {
            var cctx = ZstdSharp.Methods.ZSTD_createCCtx();
            var dctx = ZstdSharp.Methods.ZSTD_createDCtx();

            var src = File.ReadAllBytes("dickens");
            var dest = new byte[ZstdSharp.Methods.ZSTD_compressBound((nuint) src.Length)];
            var uncompressed = new byte[src.Length];
            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                var compressedLength = ZstdSharp.Methods.ZSTD_compressCCtx(cctx, dstPtr, (nuint) dest.Length, srcPtr, (nuint) src.Length,
                    level);

                var decompressedLength = ZstdSharp.Methods.ZSTD_decompressDCtx(dctx, uncompressedPtr, (nuint) uncompressed.Length, dstPtr, compressedLength);
                Console.WriteLine($"{compressedLength} {decompressedLength} {src.Length}");
            }
            ZstdSharp.Methods.ZSTD_freeCCtx(cctx);
            ZstdSharp.Methods.ZSTD_freeDCtx(dctx);
        }

        static unsafe void Test2()
        {
            var cctx = ExternMethods.ZSTD_createCCtx();
            var dctx = ExternMethods.ZSTD_createDCtx();

            var src = File.ReadAllBytes(@"D:\.ocr\dickens");
            var dest = new byte[ExternMethods.ZSTD_compressBound((nuint)src.Length)];
            var uncompressed = new byte[src.Length];
            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                var compressedLength = ExternMethods.ZSTD_compressCCtx(cctx, (IntPtr)dstPtr, (nuint)dest.Length, (IntPtr) srcPtr, (nuint)src.Length,
                    level);

                var decompressedLength = ExternMethods.ZSTD_decompressDCtx(dctx, (IntPtr)uncompressedPtr, (nuint) uncompressed.Length, (IntPtr)dstPtr, compressedLength);
                Console.WriteLine($"{compressedLength} {decompressedLength} {src.Length}");
            }
            ExternMethods.ZSTD_freeCCtx(cctx);
            ExternMethods.ZSTD_freeDCtx(dctx);
        }
    }
}
