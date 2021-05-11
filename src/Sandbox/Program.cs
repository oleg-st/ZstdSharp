using System;
using System.IO;
using Zstd.Extern;
using ZstdSharp;
using ZstdSharp.Unsafe;

namespace Sandbox
{
    class Program
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
            var src = File.ReadAllBytes("dickens");
            var compressor = new Compressor(level);
            var compressed = compressor.Wrap(src);
            using var fs = new FileStream("dickens.zst", FileMode.Create);
            fs.Write(compressed);
        }

        static void Decompress()
        {
            var src = File.ReadAllBytes("dickens.zst");
            var decompressor = new Decompressor();
            var uncompressed = decompressor.Unwrap(src);
        }

        static unsafe void Test1()
        {
            var cctx = Methods.ZSTD_createCCtx();
            var dctx = Methods.ZSTD_createDCtx();

            var src = File.ReadAllBytes("dickens");
            var dest = new byte[Methods.ZSTD_compressBound((nuint) src.Length)];
            var uncompressed = new byte[src.Length];
            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                var compressedLength = Methods.ZSTD_compressCCtx(cctx, dstPtr, (nuint) dest.Length, srcPtr, (nuint) src.Length,
                    level);

                var decompressedLength = Methods.ZSTD_decompressDCtx(dctx, uncompressedPtr, (nuint) uncompressed.Length, dstPtr, compressedLength);
                Console.WriteLine($"{compressedLength} {decompressedLength} {src.Length}");
            }
            Methods.ZSTD_freeCCtx(cctx);
            Methods.ZSTD_freeDCtx(dctx);
        }

        static unsafe void Test2()
        {
            var cctx = ExternMethods.ZSTD_createCCtx();
            var dctx = ExternMethods.ZSTD_createDCtx();

            var src = File.ReadAllBytes("dickens");
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
