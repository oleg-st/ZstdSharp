using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;
using Zstd.Extern;
using ZstdSharp.Unsafe;

namespace ZstdSharp.Test
{
    public unsafe class ZstdTest
    {
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
            var bytes = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "golden-compression",
                "PR-3517-block-splitter-corruption-test"));
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

        [Theory]
        [InlineData(31)]
        [InlineData(65539)]
        public void CompressExternalSequencesAndLiterals(int firstMatchLength)
        {
            var sequences = new[]
            {
                new ZSTD_Sequence { offset = 1, litLength = 1, matchLength = (uint)firstMatchLength },
                new ZSTD_Sequence { offset = 2, litLength = 2, matchLength = 32 },
                new ZSTD_Sequence { offset = 1, litLength = 1, matchLength = 40 },
                new ZSTD_Sequence { litLength = 1 } // Explicit end of block
            };
            var literals = Encoding.ASCII.GetBytes("AbcD!........"); // Eight bytes of required padding
            var expected = Encoding.ASCII.GetBytes(new string('A', firstMatchLength + 1)
                + string.Concat(Enumerable.Repeat("bc", 17)) + new string('D', 41) + "!");
            var compressed = new byte[Methods.ZSTD_compressBound((nuint)expected.Length)];

            var ctx = Methods.ZSTD_createCCtx();
            try
            {
                Methods.ZSTD_CCtx_setParameter(ctx, ZSTD_cParameter.ZSTD_c_experimentalParam11,
                    (int)ZSTD_sequenceFormat_e.ZSTD_sf_explicitBlockDelimiters).EnsureZstdSuccess();
                Methods.ZSTD_CCtx_setParameter(ctx, ZSTD_cParameter.ZSTD_c_experimentalParam19,
                    (int)ZSTD_paramSwitch_e.ZSTD_ps_disable).EnsureZstdSuccess(); // Skip repcode search
                fixed (ZSTD_Sequence* seq = sequences)
                fixed (byte* lit = literals, dst = compressed)
                {
                    var size = Methods.ZSTD_compressSequencesAndLiterals(ctx, dst, (nuint)compressed.Length,
                        seq, (nuint)sequences.Length, lit, 5, (nuint)literals.Length, (nuint)expected.Length).EnsureZstdSuccess();
                    using var decompressor = new Decompressor();
                    Assert.Equal(expected, decompressor.Unwrap(compressed.AsSpan(0, (int)size)).ToArray());
                }
            }
            finally
            {
                Methods.ZSTD_freeCCtx(ctx);
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
            var filename = Path.Combine(AppContext.BaseDirectory, "dickens");
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
            var filename = Path.Combine(AppContext.BaseDirectory, "dickens");
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

        [Fact]
        public void CompressCCtxReuseAcrossLevelsMatchesFreshContext()
        {
            // A previous call at a level without the row match finder (fast strategy) must not
            // leak its resolved useRowMatchFinder into the next call on the same context.
            var src = File.ReadAllBytes("dickens").AsSpan(0, 1 << 20).ToArray();
            var fresh = new byte[Methods.ZSTD_compressBound((nuint)src.Length)];
            var reused = new byte[fresh.Length];
            var freshCtx = Methods.ZSTD_createCCtx();
            var reusedCtx = Methods.ZSTD_createCCtx();
            try
            {
                fixed (byte* srcPtr = src)
                fixed (byte* freshPtr = fresh)
                fixed (byte* reusedPtr = reused)
                {
                    var freshSize = Methods.ZSTD_compressCCtx(freshCtx, freshPtr, (nuint)fresh.Length, srcPtr,
                        (nuint)src.Length, 12).EnsureZstdSuccess();

                    Methods.ZSTD_compressCCtx(reusedCtx, reusedPtr, (nuint)reused.Length, srcPtr, 1 << 16, 1)
                        .EnsureZstdSuccess();
                    var reusedSize = Methods.ZSTD_compressCCtx(reusedCtx, reusedPtr, (nuint)reused.Length, srcPtr,
                        (nuint)src.Length, 12).EnsureZstdSuccess();

                    Assert.Equal(ZSTD_paramSwitch_e.ZSTD_ps_enable, reusedCtx->appliedParams.useRowMatchFinder);
                    Assert.True(fresh.AsSpan(0, (int)freshSize).SequenceEqual(reused.AsSpan(0, (int)reusedSize)));
                }
            }
            finally
            {
                Methods.ZSTD_freeCCtx(freshCtx);
                Methods.ZSTD_freeCCtx(reusedCtx);
            }
        }

        [Fact]
        public void CompressorLevels()
        {
            Assert.Equal(Compressor.MinCompressionLevel, Methods.ZSTD_minCLevel());
            Assert.Equal(Compressor.MaxCompressionLevel, Methods.ZSTD_maxCLevel());
            Assert.Equal(Compressor.DefaultCompressionLevel, Methods.ZSTD_defaultCLevel());
        }
    }
}
