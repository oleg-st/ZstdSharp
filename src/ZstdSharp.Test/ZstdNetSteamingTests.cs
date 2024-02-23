using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using ZstdSharp.Unsafe;

namespace ZstdSharp.Test
{
    public enum DataFill
    {
        Random,
        Sequential
    }

    internal static class DataGenerator
    {
        private static readonly Random Random = new(1234);

        public const int LargeBufferSize = 1024 * 1024;
        public const int SmallBufferSize = 1024;

        public static MemoryStream GetSmallStream(DataFill dataFill) => GetStream(SmallBufferSize, dataFill);
        public static MemoryStream GetLargeStream(DataFill dataFill) => GetStream(LargeBufferSize, dataFill);
        public static MemoryStream GetStream(int length, DataFill dataFill) => new(GetBuffer(length, dataFill));

        public static byte[] GetSmallBuffer(DataFill dataFill) => GetBuffer(SmallBufferSize, dataFill);
        public static byte[] GetLargeBuffer(DataFill dataFill) => GetBuffer(LargeBufferSize, dataFill);

        public static byte[] GetBuffer(int length, DataFill dataFill)
        {
            var buffer = new byte[length];
            if (dataFill == DataFill.Random)
                Random.NextBytes(buffer);
            else
            {
                for (int i = 0; i < buffer.Length; i++)
                    buffer[i] = (byte) (i % 256);
            }

            return buffer;
        }
    }

    public class ZstdNetSteamingTests
    {
        [Fact]
        public async void StreamingCompressionZeroAndOneByte()
        {
            var data = new byte[] {0, 0, 0, 1, 2, 3, 4, 0, 0, 0};

            var tempStream = new MemoryStream();
            await using (var compressionStream = new CompressionStream(tempStream))
            {
                compressionStream.Write(data, 0, 0);
                compressionStream.Write(ReadOnlySpan<byte>.Empty);
                await compressionStream.WriteAsync(data, 0, 0);
                await compressionStream.WriteAsync(ReadOnlyMemory<byte>.Empty);

                compressionStream.Write(data, 3, 1);
                compressionStream.Write(new ReadOnlySpan<byte>(data, 4, 1));
                compressionStream.Flush();
                await compressionStream.WriteAsync(data, 5, 1);
                await compressionStream.WriteAsync(new ReadOnlyMemory<byte>(data, 6, 1));
                await compressionStream.FlushAsync();
            }

            tempStream.Seek(0, SeekOrigin.Begin);

            var result = new byte[data.Length];
            using (var decompressionStream = new DecompressionStream(tempStream))
            {
                Assert.Equal(0, decompressionStream.Read(result, 0, 0));
                Assert.Equal(0, decompressionStream.Read(Span<byte>.Empty));
                Assert.Equal(0, await decompressionStream.ReadAsync(result, 0, 0));
                Assert.Equal(0, await decompressionStream.ReadAsync(Memory<byte>.Empty));

                Assert.Equal(1, decompressionStream.Read(result, 3, 1));
                Assert.Equal(1, decompressionStream.Read(new Span<byte>(result, 4, 1)));
                Assert.Equal(1, await decompressionStream.ReadAsync(result, 5, 1));
                Assert.Equal(1, await decompressionStream.ReadAsync(new Memory<byte>(result, 6, 1)));
            }

            Assert.True(data.SequenceEqual(result));
        }


        [Theory]
        [InlineData(new byte[0], 0, 0)]
        [InlineData(new byte[] {1, 2, 3}, 1, 2)]
        [InlineData(new byte[] {1, 2, 3}, 0, 2)]
        [InlineData(new byte[] {1, 2, 3}, 1, 1)]
        [InlineData(new byte[] {1, 2, 3}, 0, 3)]
        public void StreamingCompressionSimpleWrite(byte[] data, int offset, int count)
        {
            var tempStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(tempStream))
                compressionStream.Write(data, offset, count);

            tempStream.Seek(0, SeekOrigin.Begin);

            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(tempStream))
                decompressionStream.CopyTo(resultStream);

            var dataToCompress = new byte[count];
            Array.Copy(data, offset, dataToCompress, 0, count);

            Assert.True(dataToCompress.SequenceEqual(resultStream.ToArray()));
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(9)]
        [InlineData(10)]
        public void StreamingDecompressionSimpleRead(int readCount)
        {
            var data = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

            var tempStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(tempStream))
                compressionStream.Write(data, 0, data.Length);

            tempStream.Seek(0, SeekOrigin.Begin);

            var buffer = new byte[data.Length];
            using (var decompressionStream = new DecompressionStream(tempStream))
            {
                int bytesRead;
                int totalBytesRead = 0;
                while ((bytesRead = decompressionStream.Read(buffer, totalBytesRead,
                    Math.Min(readCount, buffer.Length - totalBytesRead))) > 0)
                {
                    Assert.True(bytesRead <= readCount);
                    totalBytesRead += bytesRead;
                }

                Assert.Equal(data.Length, totalBytesRead);
            }

            Assert.True(data.SequenceEqual(buffer));
        }

        [Fact]
        public void StreamingDecompressionTruncatedInput()
        {
            var dataStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            var resultStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(resultStream))
                dataStream.CopyTo(compressionStream);

            // truncate resultStream
            var truncatedStream =
                new MemoryStream(resultStream.ToArray(), 0, Math.Min(32, (int) resultStream.Length / 3));

            var exception = Record.Exception(() =>
            {
                using var decompressionStream = new DecompressionStream(truncatedStream);
                decompressionStream.CopyTo(resultStream);
            });
            Assert.True(exception is EndOfStreamException);
        }

        [Fact]
        public void StreamingCompressionFlushDataFromInternalBuffers()
        {
            var testBuffer = new byte[1];

            var tempStream = new MemoryStream();
            using var compressionStream = new CompressionStream(tempStream);
            compressionStream.Write(testBuffer, 0, testBuffer.Length);
            compressionStream.Flush();

            Assert.True(tempStream.Length > 0);
            tempStream.Seek(0, SeekOrigin.Begin);

            //NOTE: without ZSTD_endStream call on compression
            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(tempStream, checkEndOfStream: false))
                decompressionStream.CopyTo(resultStream);

            Assert.True(testBuffer.SequenceEqual(resultStream.ToArray()));
        }

        [Fact]
        public void CompressionImprovesWithDictionary()
        {
            var dict = TrainDict();

            var dataStream = DataGenerator.GetSmallStream(DataFill.Sequential);

            var normalResultStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(normalResultStream))
                dataStream.CopyTo(compressionStream);

            dataStream.Seek(0, SeekOrigin.Begin);

            var dictResultStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(dictResultStream))
            {
                compressionStream.LoadDictionary(dict);
                dataStream.CopyTo(compressionStream);
            }

            Assert.True(normalResultStream.Length > dictResultStream.Length);

            dictResultStream.Seek(0, SeekOrigin.Begin);

            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(dictResultStream))
            {
                decompressionStream.LoadDictionary(dict);
                decompressionStream.CopyTo(resultStream);
            }

            Assert.True(dataStream.ToArray().SequenceEqual(resultStream.ToArray()));
        }

        [Fact]
        public void CompressionShrinksData()
        {
            var dataStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            var resultStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(resultStream))
                dataStream.CopyTo(compressionStream);

            Assert.True(dataStream.Length > resultStream.Length);
        }

        [Fact]
        public void RoundTrip_BatchToStreaming()
        {
            var data = DataGenerator.GetLargeBuffer(DataFill.Sequential);

            byte[] compressed;
            using (var compressor = new Compressor())
                compressed = compressor.Wrap(data).ToArray();

            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(new MemoryStream(compressed)))
                decompressionStream.CopyTo(resultStream);

            Assert.True(data.SequenceEqual(resultStream.ToArray()));
        }

        [Fact]
        public void RoundTrip_StreamingToBatch()
        {
            var dataStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            var tempStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(tempStream))
                dataStream.CopyTo(compressionStream);

            var resultBuffer = new byte[dataStream.Length];
            using (var decompressor = new Decompressor())
                Assert.Equal(dataStream.Length, decompressor.Unwrap(tempStream.ToArray(), resultBuffer, 0));

            Assert.True(dataStream.ToArray().SequenceEqual(resultBuffer));
        }

        [Theory, CombinatorialData]
        public void RoundTrip_StreamingToStreaming(
            [CombinatorialValues(false, true)] bool useDict, [CombinatorialValues(false, true)] bool advanced,
            [CombinatorialValues(1, 2, 7, 101, 1024, 65535, DataGenerator.LargeBufferSize,
                DataGenerator.LargeBufferSize + 1)]
            int zstdBufferSize,
            [CombinatorialValues(1, 2, 7, 101, 1024, 65535, DataGenerator.LargeBufferSize,
                DataGenerator.LargeBufferSize + 1)]
            int copyBufferSize)
        {
            var dict = useDict ? TrainDict() : null;
            var testStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            const int offset = 1;
            var buffer = new byte[copyBufferSize + offset + 1];

            var tempStream = new MemoryStream();
            using (var compressionStream =
                new CompressionStream(tempStream, Compressor.DefaultCompressionLevel, zstdBufferSize))
            {
                compressionStream.LoadDictionary(dict);
                if (advanced)
                {
                    compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, 11);
                    compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
                }

                int bytesRead;
                while ((bytesRead = testStream.Read(buffer, offset, copyBufferSize)) > 0)
                    compressionStream.Write(buffer, offset, bytesRead);
            }

            tempStream.Seek(0, SeekOrigin.Begin);

            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(tempStream, zstdBufferSize))
            {
                decompressionStream.LoadDictionary(dict);
                if (advanced)
                {
                    decompressionStream.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, 11);
                }

                int bytesRead;
                while ((bytesRead = decompressionStream.Read(buffer, offset, copyBufferSize)) > 0)
                    resultStream.Write(buffer, offset, bytesRead);
            }

            Assert.True(testStream.ToArray().SequenceEqual(resultStream.ToArray()));
        }

        [Theory, CombinatorialData]
        public async Task RoundTrip_StreamingToStreamingAsync(
            [CombinatorialValues(false, true)] bool useDict, [CombinatorialValues(false, true)] bool advanced,
            [CombinatorialValues(1, 2, 7, 101, 1024, 65535, DataGenerator.LargeBufferSize,
                DataGenerator.LargeBufferSize + 1)]
            int zstdBufferSize,
            [CombinatorialValues(1, 2, 7, 101, 1024, 65535, DataGenerator.LargeBufferSize,
                DataGenerator.LargeBufferSize + 1)]
            int copyBufferSize)
        {
            var dict = useDict ? TrainDict() : null;
            var testStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            const int offset = 1;
            var buffer = new byte[copyBufferSize + offset + 1];

            var tempStream = new MemoryStream();
            await using (var compressionStream =
                new CompressionStream(tempStream, Compressor.DefaultCompressionLevel, zstdBufferSize))
            {
                compressionStream.LoadDictionary(dict);
                if (advanced)
                {
                    compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, 11);
                    compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_checksumFlag, 1);
                }

                int bytesRead;
                while ((bytesRead = await testStream.ReadAsync(buffer, offset, copyBufferSize)) > 0)
                    await compressionStream.WriteAsync(buffer, offset, bytesRead);
            }

            tempStream.Seek(0, SeekOrigin.Begin);

            var resultStream = new MemoryStream();
            await using (var decompressionStream = new DecompressionStream(tempStream, zstdBufferSize))
            {
                decompressionStream.LoadDictionary(dict);
                if (advanced)
                {
                    decompressionStream.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, 11);
                }

                int bytesRead;
                while ((bytesRead = await decompressionStream.ReadAsync(buffer, offset, copyBufferSize)) > 0)
                    await resultStream.WriteAsync(buffer, offset, bytesRead);
            }

            Assert.True(testStream.ToArray().SequenceEqual(resultStream.ToArray()));
        }

        [Theory, CombinatorialData]
        public void Pipe_StreamingToStreaming(
            // For size 2, the first read call would return part of the data, and the rest of the data would be buffered
            // in the decompression context and need to be flushed out by a second call. For size 4, the read call would
            // not fill the output buffer, but should still return the data it has received, even if no more data is
            // currently available and the stream hasn't ended yet.
            [CombinatorialValues(2, 4)] int outputBufSize)
        {
            using (var pipeServer = new AnonymousPipeServerStream())
            using (var pipeClient = new AnonymousPipeClientStream(pipeServer.GetClientHandleAsString()))
            using (var comp = new CompressionStream(pipeServer))
            using (var decomp = new DecompressionStream(pipeClient))
            {
                var data = new byte[] { 0, 1, 2 };
                comp.Write(data);
                comp.Flush();

                var buf = new byte[outputBufSize];

                var received = new List<byte>();
                while (received.Count < data.Length)
                {
                    var count = decomp.Read(buf);
                    received.AddRange(buf.Take(count));
                }

                Assert.True(received.ToArray().SequenceEqual(data));
            }
        }

        [Theory, CombinatorialData]
        public async Task Pipe_StreamingToStreamingAsync(
            [CombinatorialValues(2, 4)] int outputBufSize)
        {
            using (var pipeServer = new AnonymousPipeServerStream())
            using (var pipeClient = new AnonymousPipeClientStream(pipeServer.GetClientHandleAsString()))
            using (var comp = new CompressionStream(pipeServer))
            using (var decomp = new DecompressionStream(pipeClient))
            {
                var data = new byte[] { 0, 1, 2 };
                await comp.WriteAsync(data);
                await comp.FlushAsync();

                var buf = new byte[outputBufSize];

                var received = new List<byte>();
                while (received.Count < data.Length)
                {
                    var count = await decomp.ReadAsync(buf);
                    received.AddRange(buf.Take(count));
                }

                Assert.True(received.ToArray().SequenceEqual(data));
            }
        }

        [Fact]
        public void RoundTrip_WrapperStreams()
        {
            using var compressor = new Compressor();
            using var decompressor = new Decompressor();

            using var dataStream = DataGenerator.GetLargeStream(DataFill.Sequential);

            using var tempStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(tempStream, compressor))
                dataStream.CopyTo(compressionStream);

            var data = DataGenerator.GetLargeBuffer(DataFill.Sequential);
            var compressed = compressor.Wrap(data).ToArray();

            using var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(new MemoryStream(compressed), decompressor))
                decompressionStream.CopyTo(resultStream);

            Assert.True(data.SequenceEqual(resultStream.ToArray()));

            var resultBuffer = new byte[dataStream.Length];
            Assert.Equal(dataStream.Length, decompressor.Unwrap(tempStream.ToArray(), resultBuffer, 0));

            Assert.True(dataStream.ToArray().SequenceEqual(resultBuffer));
        }

        private static byte[] TrainDict()
        {
            var trainingData = new byte[100][];
            for (int i = 0; i < trainingData.Length; i++)
                trainingData[i] = DataGenerator.GetSmallBuffer(DataFill.Sequential);
            return DictBuilder.TrainFromBuffer(trainingData);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void StreamingLeaveOpen(bool leaveOpen)
        {
            var data = DataGenerator.GetSmallBuffer(DataFill.Sequential);

            var tempStream = new MemoryStream();
            using (var compressionStream = new CompressionStream(tempStream, leaveOpen: leaveOpen))
                compressionStream.Write(data);

            Assert.True(leaveOpen == tempStream.CanWrite);

            var compressedData = tempStream.ToArray();
            tempStream = new MemoryStream(compressedData);

            var resultStream = new MemoryStream();
            using (var decompressionStream = new DecompressionStream(tempStream, leaveOpen: leaveOpen))
                decompressionStream.CopyTo(resultStream);

            Assert.True(leaveOpen == tempStream.CanRead);

            Assert.True(data.SequenceEqual(resultStream.ToArray()));
        }
    }
}
