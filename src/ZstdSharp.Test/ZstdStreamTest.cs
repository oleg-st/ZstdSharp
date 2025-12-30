using System;
using System.Buffers;
using System.IO;
using System.Linq;
using Xunit;

namespace ZstdSharp.Test
{
    public class ZstdStreamTest
    {
        [Fact]
        public void EmptyCompress()
        {
            var compressor = new Compressor();
            var data = DataGenerator.GetSmallBuffer(DataFill.Random);

            Assert.Equal(compressor.WrapStream(data, Span<byte>.Empty, out var bytesConsumed, out var bytesWritten, true), OperationStatus.DestinationTooSmall);
            Assert.Equal(bytesConsumed, 0);
            Assert.Equal(bytesWritten, 0);
        }

        [Fact]
        public void EmptyDecompress()
        {
            var decompressor = new Decompressor();
            var data = DataGenerator.GetSmallBuffer(DataFill.Random);

            Assert.Equal(decompressor.UnwrapStream(data, Span<byte>.Empty, out var bytesConsumed, out var bytesWritten), OperationStatus.DestinationTooSmall);
            Assert.Equal(bytesConsumed, 0);
            Assert.Equal(bytesWritten, 0);
        }

        [Fact]
        public void OneShot()
        {
            var compressor = new Compressor();
            var data = DataGenerator.GetSmallBuffer(DataFill.Random);
            var decompressor = new Decompressor();
            var compressBuffer = new byte[Compressor.GetCompressBound(data.Length)];

            Assert.Equal(compressor.WrapStream(data, compressBuffer, out var compressBytesConsumed, out var compressBytesWritten, true),
                OperationStatus.Done);
            Assert.Equal(compressBytesConsumed, data.Length);

            var decompressBuffer = new byte[data.Length];
            var compressedSpan = new ReadOnlySpan<byte>(compressBuffer, 0, compressBytesWritten);
            Assert.Equal(decompressor.UnwrapStream(compressedSpan, decompressBuffer, out var decompressBytesConsumed, out var decompressBytesWritten), OperationStatus.Done);
            Assert.Equal(decompressBytesWritten, data.Length);
            Assert.Equal(decompressBytesConsumed, compressBytesWritten);
            Assert.True(decompressBuffer.SequenceEqual(data));
        }

        [Fact]
        public void TwoConcatenated()
        {
            var compressor = new Compressor();
            var data = DataGenerator.GetSmallBuffer(DataFill.Random);
            var decompressor = new Decompressor();
            var compressBuffer = new byte[Compressor.GetCompressBound(data.Length) * 2];

            // compress 1
            Assert.Equal(compressor.WrapStream(data, compressBuffer, out var compressBytesConsumed1, out var compressBytesWritten1, true),
                OperationStatus.Done);
            Assert.Equal(compressBytesConsumed1, data.Length);

            // compress 2
            Assert.Equal(compressor.WrapStream(data, new Span<byte>(compressBuffer).Slice(compressBytesWritten1), out var compressBytesConsumed2, out var compressBytesWritten2, true),
                OperationStatus.Done);
            Assert.Equal(compressBytesConsumed2, data.Length);

            var decompressBuffer = new byte[data.Length * 2];
            var compressedSpan = new ReadOnlySpan<byte>(compressBuffer, 0, compressBytesWritten1 + compressBytesWritten2);
            Assert.Equal(decompressor.UnwrapStream(compressedSpan, decompressBuffer, out var decompressBytesConsumed, out var decompressBytesWritten), OperationStatus.Done);
            Assert.Equal(decompressBytesWritten, data.Length * 2);
            Assert.Equal(decompressBytesConsumed, compressBytesWritten1 + compressBytesWritten2);
            Assert.True(new ReadOnlySpan<byte>(decompressBuffer).Slice(0, data.Length).SequenceEqual(data));
            Assert.True(new ReadOnlySpan<byte>(decompressBuffer).Slice(data.Length).SequenceEqual(data));
        }

        [Fact]
        public void TestNeedMoreInput()
        {
            var compressor = new Compressor();
            var data = DataGenerator.GetSmallBuffer(DataFill.Random);
            var decompressor = new Decompressor();
            var compressBuffer = new byte[Compressor.GetCompressBound(data.Length)];

            Assert.Equal(
                compressor.WrapStream(data, compressBuffer, out var compressBytesConsumed, out var compressBytesWritten,
                    true),
                OperationStatus.Done);
            Assert.Equal(compressBytesConsumed, data.Length);

            var decompressBuffer = new byte[data.Length];
            // split
            var truncatedLength = compressBytesWritten / 2;
            var compressedSpan1 = new ReadOnlySpan<byte>(compressBuffer, 0, truncatedLength);
            var compressedSpan2 =
                new ReadOnlySpan<byte>(compressBuffer, truncatedLength, compressBytesWritten - truncatedLength);

            // more data
            Assert.Equal(
                decompressor.UnwrapStream(compressedSpan1, decompressBuffer, out var decompressBytesConsumed1,
                    out var decompressBytesWritten1), OperationStatus.NeedMoreData);
            // consumed all input
            Assert.Equal(decompressBytesConsumed1, compressedSpan1.Length);

            // leftover
            Assert.Equal(
                decompressor.UnwrapStream(compressedSpan2,
                    new Span<byte>(decompressBuffer, decompressBytesWritten1,
                        decompressBuffer.Length - decompressBytesWritten1), out var decompressBytesConsumed2,
                    out var decompressBytesWritten2), OperationStatus.Done);

            // consumed all input
            Assert.Equal(decompressBytesConsumed2, compressedSpan2.Length);
            Assert.Equal(decompressBytesWritten1 + decompressBytesWritten2, data.Length);

            Assert.True(decompressBuffer.SequenceEqual(data));
        }

        [Theory]
        [InlineData(1, 512)]
        [InlineData(512, 1)]
        [InlineData(512, 512)]
        [InlineData(1024, DataGenerator.LargeBufferSize)]
        [InlineData(DataGenerator.LargeBufferSize, 1024)]
        public void WrapChunked(int bufferSize, int chunkSize)
        {
            var compressor = new Compressor();
            var decompressor = new Decompressor();
            var data = DataGenerator.GetLargeBuffer(DataFill.Random);

            var dest = new byte[bufferSize];
            var ms = new MemoryStream();
            var srcSpan = new ReadOnlySpan<byte>(data);
            var destSpan = new Span<byte>(dest);
            OperationStatus status;
            var offset = 0;
            bool isFinalBlock;
            do
            {
                var chunk = srcSpan.Slice(offset);
                isFinalBlock = chunk.Length < chunkSize;
                status = compressor.WrapStream(chunk, destSpan, out var bytesConsumed, out var bytesWritten, isFinalBlock);
                Assert.True(status is OperationStatus.Done or OperationStatus.DestinationTooSmall);
                ms.Write(dest, 0, bytesWritten);
                offset += bytesConsumed;
            } while (status != OperationStatus.Done || !isFinalBlock);

            var compressed = ms.ToArray();
            var decompressed = decompressor.Unwrap(compressed);
            Assert.True(decompressed.SequenceEqual(data));
        }

        [Theory]
        [InlineData(1, 512)]
        [InlineData(512, 1)]
        [InlineData(512, 512)]
        [InlineData(1024, DataGenerator.LargeBufferSize)]
        [InlineData(DataGenerator.LargeBufferSize, 1024)]
        public void UnwrapChunked(int bufferSize, int chunkSize)
        {
            var compressor = new Compressor();
            var decompressor = new Decompressor();
            var data = DataGenerator.GetLargeBuffer(DataFill.Random);
            var compressed = compressor.Wrap(data);

            var dest = new byte[bufferSize];
            var ms = new MemoryStream();
            var srcSpan = compressed;
            var destSpan = new Span<byte>(dest);
            OperationStatus status;
            var offset = 0;
            do
            {
                var chunk = srcSpan.Slice(offset);
                status = decompressor.UnwrapStream(chunk, destSpan, out var bytesConsumed, out var bytesWritten);
                Assert.True(status is OperationStatus.Done or OperationStatus.DestinationTooSmall);
                ms.Write(dest, 0, bytesWritten);
                offset += bytesConsumed;
            } while (status != OperationStatus.Done);

            var decompressed = ms.ToArray();
            Assert.True(decompressed.SequenceEqual(data));
        }
    }
}
