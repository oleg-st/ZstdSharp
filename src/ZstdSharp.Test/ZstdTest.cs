using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;
using Zstd.Extern;
using ZstdSharp.Unsafe;

namespace ZstdSharp.Test
{
    public unsafe class ZstdTest
    {
        private Span<byte> CompressNative(byte[] srcBuffer, int compressBound, int level)
        {
            var buffer = new byte[compressBound];
            fixed (byte* bufferPtr = buffer)
            fixed (byte* srcPtr = srcBuffer)
            {
                var cctx = ExternMethods.ZSTD_createCCtx();
                try
                {
                    ExternMethods.ZSTD_CCtx_setParameter(cctx, ExternMethods.ZSTD_cParameter.ZSTD_c_compressionLevel,
                        level);

                    var length = ExternMethods.ZSTD_compress2(cctx,
                        (IntPtr) bufferPtr, (nuint) buffer.Length,
                        (IntPtr) srcPtr, (nuint) srcBuffer.Length);
                    return new Span<byte>(buffer, 0, (int) length);
                }
                finally
                {

                    ExternMethods.ZSTD_freeCCtx(cctx);
                }
            }
        }

        private Span<byte> DecompressNative(ReadOnlySpan<byte> src, int decompressBound)
        {
            fixed (byte* srcPtr = src)
            {
                var buffer = new byte[decompressBound];
                fixed (byte* decompressedBufferNativePtr = buffer)
                {
                    var dctx = ExternMethods.ZSTD_createDCtx();
                    try
                    {
                        var length = ExternMethods.ZSTD_decompressDCtx(dctx,
                            (IntPtr) decompressedBufferNativePtr,
                            (nuint) buffer.Length,
                            (IntPtr) srcPtr, (nuint) src.Length);
                        return new Span<byte>(buffer, 0, (int) length);
                    }
                    finally
                    {

                        ExternMethods.ZSTD_freeDCtx(dctx);
                    }
                }
            }
        }

        public static IEnumerable<object[]> LevelsData =>
            Enumerable.Range(-5, 5)
                .Concat(Enumerable.Range(1, Compressor.MaxCompressionLevel))
                .Select(level => new object[] {level});

        [Theory]
        [MemberData(nameof(LevelsData))]
        public void CompressAndDecompressWithNative(int level)
        {
            var srcBuffer = File.ReadAllBytes("dickens");

            var compressBound = Compressor.GetCompressBound(srcBuffer.Length);
            Assert.Equal((int) ExternMethods.ZSTD_compressBound((nuint) srcBuffer.Length), compressBound);

            using var compressor = new Compressor(level);
            var compressedSharp = compressor.Wrap(srcBuffer);
            var compressedNative = CompressNative(srcBuffer, compressBound, level);
            Assert.True(compressedNative.SequenceEqual(compressedSharp));

            var decompressBound = (int) Decompressor.GetDecompressedSize(compressedSharp);
            Assert.Equal(decompressBound, srcBuffer.Length);

            using var decompressor = new Decompressor();
            var decompressedSharp = decompressor.Unwrap(compressedSharp);
            var decompressedNative = DecompressNative(compressedNative, decompressBound);
            Assert.True(decompressedSharp.SequenceEqual(decompressedNative));
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
