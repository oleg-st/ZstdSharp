using System;
using System.IO;
using BenchmarkDotNet.Attributes;
using Zstd.Extern;

namespace ZstdSharp.Benchmark
{
    public unsafe class Benchmark
    {
        private byte[] src;
        private byte[] dest;
        private byte[] uncompressed;

        private ZSTD_CCtx_s* cCtx;
        private ZSTD_DCtx_s* dCtx;

        private IntPtr cCtxNative;
        private IntPtr dCtxNative;

        private readonly int level = 1;
        private nuint compressedLength;

        [GlobalSetup]
        public void Setup()
        {
            cCtx = Methods.ZSTD_createCCtx();
            dCtx = Methods.ZSTD_createDCtx();

            cCtxNative = ExternMethods.ZSTD_createCCtx();
            dCtxNative = ExternMethods.ZSTD_createDCtx();

            src = File.ReadAllBytes("dickens");
            dest = new byte[Methods.ZSTD_compressBound((nuint) src.Length)];
            uncompressed = new byte[src.Length];

            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            {
                compressedLength = ExternMethods.ZSTD_compressCCtx(cCtxNative, (IntPtr) dstPtr, (nuint) dest.Length,
                    (IntPtr) srcPtr, (nuint) src.Length,
                    level);
            }
        }

        [Benchmark]
        public void CompressSharp()
        {
            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            {
                Methods.ZSTD_compressCCtx(cCtx, dstPtr, (nuint) dest.Length, srcPtr, (nuint) src.Length, level);
            }
        }

        [Benchmark]
        public void DecompressSharp()
        {
            fixed (byte* dstPtr = dest)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                Methods.ZSTD_decompressDCtx(dCtx, uncompressedPtr, (nuint) uncompressed.Length, dstPtr,
                    compressedLength);
            }
        }

        [Benchmark]
        public void CompressNative()
        {
            fixed (byte* dstPtr = dest)
            fixed (byte* srcPtr = src)
            {
                ExternMethods.ZSTD_compressCCtx(cCtxNative, (IntPtr) dstPtr, (nuint) dest.Length, (IntPtr) srcPtr,
                    (nuint) src.Length, level);
            }
        }

        [Benchmark]
        public void DecompressNative()
        {
            fixed (byte* dstPtr = dest)
            fixed (byte* uncompressedPtr = uncompressed)
            {
                ExternMethods.ZSTD_decompressDCtx(dCtxNative, (IntPtr) uncompressedPtr, (nuint) uncompressed.Length,
                    (IntPtr) dstPtr, compressedLength);
            }
        }
    }
}
