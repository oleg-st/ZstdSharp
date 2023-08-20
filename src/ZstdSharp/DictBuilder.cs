using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZstdSharp.Unsafe;

namespace ZstdSharp
{
    public static unsafe class DictBuilder
    {
        public static byte[] TrainFromBuffer(IEnumerable<byte[]> samples, int dictCapacity = DefaultDictCapacity) 
            => TrainFromBufferFastCover(samples, Methods.ZSTD_defaultCLevel(), dictCapacity).ToArray();

        public static Span<byte> TrainFromBufferFastCover(IEnumerable<byte[]> samples, int level, int dictCapacity = DefaultDictCapacity)
        {
            // same as in ZDICT_trainFromBuffer
            return TrainFromBufferFastCover(samples, new ZDICT_fastCover_params_t
            {
                d = 8,
                steps = 4,
                zParams = new ZDICT_params_t {compressionLevel = level}
            }, dictCapacity);
        }

        public static Span<byte> TrainFromBufferFastCover(IEnumerable<byte[]> samples, ZDICT_fastCover_params_t @params, int dictCapacity = DefaultDictCapacity)
        {
            var ms = new MemoryStream();
            var samplesSizes = samples.Select(sample =>
            {
                ms.Write(sample, 0, sample.Length);
                return (nuint) sample.Length;
            }).ToArray();

            var dictBuffer = new byte[dictCapacity];
            fixed (byte* dictBufferPtr = dictBuffer)
            fixed (byte* samplesBufferPtr = ms.GetBuffer())
            fixed (nuint* samplesSizesPtr = samplesSizes)
            {
                var dictSize = (int) Methods
                    .ZDICT_optimizeTrainFromBuffer_fastCover(dictBufferPtr, (nuint) dictCapacity, samplesBufferPtr, samplesSizesPtr,
                        (uint) samplesSizes.Length, &@params)
                    .EnsureZdictSuccess();

                return new Span<byte>(dictBuffer, 0, dictSize);
            }
        }

        public const int DefaultDictCapacity = 112640; // Used by zstd utility by default
    }
}
