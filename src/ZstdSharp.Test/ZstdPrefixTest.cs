using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using ZstdSharp.Unsafe;
using Xunit;

namespace ZstdSharp.Test
{
    /// <summary>
    /// Tests for Compressor.RefPrefix / Decompressor.RefPrefix (ZSTD_CCtx_refPrefix /
    /// ZSTD_DCtx_refPrefix): delta compression against a reference content ("patch-from").
    /// </summary>
    public class ZstdPrefixTest
    {
        private static byte[] CreateRandom(int size, int seed = 42)
        {
            var data = new byte[size];
            new Random(seed).NextBytes(data);
            return data;
        }

        private static int WindowLog(long totalSize)
        {
            var windowLog = 10;
            while (windowLog < 31 && (1L << windowLog) < totalSize)
                windowLog++;
            return windowLog;
        }

        // Leave the context as the only strong owner when the helper returns, even in Debug.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference InstallPrefix(Action<byte[]> setPrefix)
        {
            var prefix = new byte[1024];
            setPrefix(prefix);
            return new WeakReference(prefix);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference RejectPrefixChange(Action<byte[]> setPrefix, bool clear)
        {
            var replacement = clear ? null : new byte[1024];
            var error = Assert.Throws<ZstdException>(() => setPrefix(replacement));
            Assert.Equal(ZSTD_ErrorCode.ZSTD_error_stage_wrong, error.Code);
            return new WeakReference(replacement);
        }

        private static void AssertOriginalPrefixRetained(WeakReference original, WeakReference rejected)
        {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true);
            Assert.True(original.IsAlive);
            Assert.False(rejected.IsAlive);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RefPrefix_CompressorKeepsOriginalOnRejectedChange(bool clear)
        {
            using var compressor = new Compressor();
            var original = InstallPrefix(compressor.RefPrefix);
            Assert.Equal(OperationStatus.Done,
                compressor.WrapStream(new byte[1], new byte[64], out _, out _, isFinalBlock: false));

            var rejected = RejectPrefixChange(compressor.RefPrefix, clear);
            AssertOriginalPrefixRetained(original, rejected);
            GC.KeepAlive(compressor);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RefPrefix_DecompressorKeepsOriginalOnRejectedChange(bool clear)
        {
            using var compressor = new Compressor();
            var frame = compressor.Wrap(new byte[1]);
            using var decompressor = new Decompressor();
            var original = InstallPrefix(decompressor.RefPrefix);
            Assert.Equal(OperationStatus.NeedMoreData,
                decompressor.UnwrapStream(frame.Slice(0, 1), new byte[1], out _, out _));

            var rejected = RejectPrefixChange(decompressor.RefPrefix, clear);
            AssertOriginalPrefixRetained(original, rejected);
            GC.KeepAlive(decompressor);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void AbandonContexts(out WeakReference compressionPrefix, out WeakReference decompressionPrefix)
        {
            compressionPrefix = InstallPrefix(new Compressor().RefPrefix);
            decompressionPrefix = InstallPrefix(new Decompressor().RefPrefix);
        }

        [Fact]
        public void RefPrefix_FinalizationReleasesPrefixes()
        {
            AbandonContexts(out var compressionPrefix, out var decompressionPrefix);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            Assert.False(compressionPrefix.IsAlive);
            Assert.False(decompressionPrefix.IsAlive);
        }

        [Fact]
        public void RefPrefix_LoadDictionaryReleasesPrefixes()
        {
            using var compressor = new Compressor();
            using var decompressor = new Decompressor();
            var compressionPrefix = InstallPrefix(compressor.RefPrefix);
            var decompressionPrefix = InstallPrefix(decompressor.RefPrefix);

            var dictionary = CreateRandom(1024);
            compressor.LoadDictionary(dictionary);
            decompressor.LoadDictionary(dictionary);
            GC.Collect();
            Assert.False(compressionPrefix.IsAlive);
            Assert.False(decompressionPrefix.IsAlive);
            GC.KeepAlive(compressor);
            GC.KeepAlive(decompressor);
        }

        [Fact]
        public void RefPrefixRoundtrip_ModifiedData()
        {
            var baseData = CreateRandom(1024 * 1024);
            var targetData = (byte[])baseData.Clone();
            for (var i = 0; i < 64; i++)
                targetData[100000 + i] ^= 0xFF;

            var windowLog = WindowLog(2L * baseData.Length);

            using var compressor = new Compressor(19);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, windowLog);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
            compressor.RefPrefix(baseData);
            var delta = compressor.Wrap(targetData).ToArray();

            // 64 changed bytes in 1 MB: the delta must be tiny, not a recompression.
            Assert.True(delta.Length < 4096, $"delta too large: {delta.Length} bytes");

            using var decompressor = new Decompressor();
            decompressor.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, windowLog);
            decompressor.RefPrefix(baseData);
            var restored = decompressor.Unwrap(delta).ToArray();
            Assert.Equal(targetData, restored);
        }

        [Fact]
        public void RefPrefixRoundtrip_WrongPrefixFails()
        {
            // The target must actually reference the prefix, otherwise decompression
            // succeeds regardless of the prefix supplied.
            var baseData = CreateRandom(1024 * 1024);
            var targetData = (byte[])baseData.Clone();
            for (var i = 0; i < 64; i++)
                targetData[200000 + i] ^= 0xFF;

            using var compressor = new Compressor(19);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, WindowLog(2L * baseData.Length));
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
            compressor.RefPrefix(baseData);
            var delta = compressor.Wrap(targetData).ToArray();
            Assert.True(delta.Length < 4096, "the delta should reference the prefix");

            var wrongPrefix = CreateRandom(1024 * 1024, seed: 44);
            using var decompressor = new Decompressor();
            decompressor.RefPrefix(wrongPrefix);
            Assert.Throws<ZstdException>(() => decompressor.Unwrap(delta));
        }

        [Fact]
        public void RefPrefix_LargeReference_StaysEffective()
        {
            // Delta ("patch-from") against large references: LoadDictionary builds a CDict
            // whose effectiveness degrades beyond ~32-64 MB of content (inherited zstd
            // behavior), while a prefix is indexed with the context parameters and has no
            // such limit.
            const int size = 80 * 1024 * 1024;
            var baseData = CreateRandom(size);
            var targetData = (byte[])baseData.Clone();

            var windowLog = WindowLog(2L * size);

            using var compressor = new Compressor(3);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, windowLog);
            compressor.RefPrefix(baseData);
            var delta = compressor.Wrap(targetData).ToArray();

            // Identical content: with an effective reference the delta is <1% of the input.
            Assert.True(delta.Length < size / 100, $"prefix was not effective: {delta.Length} bytes");

            using var decompressor = new Decompressor();
            decompressor.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, windowLog);
            decompressor.RefPrefix(baseData);
            var restored = decompressor.Unwrap(delta).ToArray();
            Assert.Equal(targetData, restored);
        }

        [Fact]
        public void RefPrefix_AppliesToNextFrameOnly()
        {
            // Native semantics: the prefix is consumed by the next frame; later frames
            // compress without it unless it is referenced again.
            var baseData = CreateRandom(1024 * 1024);
            var targetData = (byte[])baseData.Clone();

            using var compressor = new Compressor(3);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
            compressor.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, WindowLog(2L * baseData.Length));

            compressor.RefPrefix(baseData);
            var first = compressor.Wrap(targetData).ToArray();
            Assert.True(first.Length < baseData.Length / 100, "first frame should use the prefix");

            var second = compressor.Wrap(targetData).ToArray();
            Assert.True(second.Length > baseData.Length / 2, "second frame should not use the prefix");

            compressor.RefPrefix(baseData);
            var third = compressor.Wrap(targetData).ToArray();
            Assert.True(third.Length < baseData.Length / 100, "re-referenced prefix should apply again");
        }
    }
}
