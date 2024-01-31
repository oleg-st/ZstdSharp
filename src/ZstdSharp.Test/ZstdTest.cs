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
                var exception = Record.Exception(() => System.Runtime.CompilerServices.RuntimeHelpers.PrepareMethod(method.MethodHandle));
                Assert.True(exception == null, $"Method {method.Name} jit failed");
            }
        }

        [Fact]
        public void BlockSplitterCorruptionTest()
        {
            var bytes = File.ReadAllBytes("golden-compression/PR-3517-block-splitter-corruption-test");
            var compressor = new Compressor(19);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_minMatch, 7);
            // ZSTD_c_experimentalParam13 -> ZSTD_c_useBlockSplitter
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_experimentalParam13,
                (int) ZSTD_paramSwitch_e.ZSTD_ps_enable);
            var compressed = compressor.Wrap(bytes);

            var decompressor = new Decompressor();
            var decompressed = decompressor.Unwrap(compressed).ToArray();

            Assert.Equal(decompressed.Length, bytes.Length);
            Assert.True(decompressed.SequenceEqual(bytes));
        }

        [Fact]
        public void CheckAttributes()
        {
            foreach (var method in typeof(Methods).Module.GetTypes().SelectMany(t => t.GetMethods(BindingFlags.DeclaredOnly |
                BindingFlags.NonPublic |
                BindingFlags.Public | BindingFlags.Instance |
                BindingFlags.Static)))
            {
                var exception = Record.Exception(() => method.GetCustomAttributes(true));
                Assert.True(exception == null, $"Method {method.Name} has invalid attributes");
            }
        }

        [Fact]
        public void VersionMatch()
        {
            Assert.Equal(Methods.ZSTD_versionNumber(), ExternMethods.ZSTD_versionNumber());
        }

        [Fact]
        public void CompressMultiThread()
        {
            const int level = 1;
            const int numThreads = 4;
            const string filename = "dickens";
            var srcBuffer = File.ReadAllBytes(filename);

            using var compressor = new Compressor(level);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, numThreads);
            var compressed = compressor.Wrap(srcBuffer);

            using var decompressor = new Decompressor();
            var decompressed = decompressor.Unwrap(compressed);
            Assert.True(decompressed.SequenceEqual(srcBuffer));
        }

        [Fact]
        public void CompressStreamMultiThread()
        {
            const int level = 1;
            const int numThreads = 4;
            const string filename = "dickens";
            using var input = File.OpenRead(filename);
            using var output = new MemoryStream();

            using (var compressionStream = new CompressionStream(output, level))
            {
                compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, numThreads);
                input.CopyTo(compressionStream);
            }

            using var decompressor = new Decompressor();
            var decompressed = decompressor.Unwrap(output.ToArray());
            Assert.True(decompressed.SequenceEqual(File.ReadAllBytes(filename)));
        }
    }
}
