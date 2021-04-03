using System;
using System.IO;
using System.Reflection;
using Xunit;
using Zstd.Extern;

namespace ZstdSharp.Test
{
    public unsafe class ZstdTest
    {
        private readonly byte[] srcBuffer;
        private readonly byte[] destBuffer;
        private readonly byte[] decompressedBuffer;

        private readonly ZSTD_CCtx_s* cCtx;
        private readonly ZSTD_DCtx_s* dCtx;

        private readonly IntPtr cCtxNative;
        private readonly IntPtr dCtxNative;

        public ZstdTest()
        {
            cCtx = Methods.ZSTD_createCCtx();
            dCtx = Methods.ZSTD_createDCtx();

            cCtxNative = ExternMethods.ZSTD_createCCtx();
            dCtxNative = ExternMethods.ZSTD_createDCtx();

            srcBuffer = File.ReadAllBytes("dickens");
            destBuffer = new byte[Methods.ZSTD_compressBound((nuint)srcBuffer.Length)];
            decompressedBuffer = new byte[srcBuffer.Length];
        }

        [Theory]
        [InlineData(-5)]
        [InlineData(-4)]
        [InlineData(-3)]
        [InlineData(-2)]
        [InlineData(-1)]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(9)]
        [InlineData(10)]
        [InlineData(11)]
        [InlineData(12)]
        [InlineData(13)]
        [InlineData(14)]
        [InlineData(15)]
        [InlineData(16)]
        [InlineData(17)]
        [InlineData(18)]
        [InlineData(19)]
        [InlineData(20)]
        [InlineData(21)]
        [InlineData(22)]

        public void TestCompressDecompress(int level)
        {
            fixed (byte* dstPtr = destBuffer)
            fixed (byte* srcPtr = srcBuffer)
            fixed (byte* uncompressedPtr = decompressedBuffer)
            {
                Assert.Equal(ExternMethods.ZSTD_compressBound((nuint) srcBuffer.Length),
                    Methods.ZSTD_compressBound((nuint) srcBuffer.Length));

                var compressed = Methods.ZSTD_compressCCtx(cCtx, dstPtr, (nuint)destBuffer.Length, srcPtr, (nuint)srcBuffer.Length, level);
                var compressedNative = ExternMethods.ZSTD_compressCCtx(cCtxNative, (IntPtr)dstPtr, (nuint)destBuffer.Length, (IntPtr)srcPtr,
                    (nuint)srcBuffer.Length, level);

                Assert.Equal(compressedNative, compressed);

                var decompressed = Methods.ZSTD_decompressDCtx(dCtx, uncompressedPtr, (nuint) decompressedBuffer.Length, dstPtr, compressed);

                var decompressedNative = ExternMethods.ZSTD_decompressDCtx(dCtxNative, (IntPtr) uncompressedPtr, (nuint) decompressedBuffer.Length,
                    (IntPtr) dstPtr, compressed);

                Assert.Equal(decompressedNative, decompressed);
            }
        }

        [Fact]
        public void JitMethods()
        {
            foreach (var method in typeof(Methods).GetMethods(BindingFlags.DeclaredOnly |
                                                              BindingFlags.NonPublic |
                                                              BindingFlags.Public | BindingFlags.Instance |
                                                              BindingFlags.Static))
            {
                bool isValid;
                try
                {
                    System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle);
                    isValid = true;
                }
                catch (Exception)
                {
                    isValid = false;
                }

                Assert.True(isValid, $"Method {method.Name}");
            }
        }
    }
}
