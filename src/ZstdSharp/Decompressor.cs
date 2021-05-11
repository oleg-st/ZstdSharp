using System;
using ZstdSharp.Unsafe;

namespace ZstdSharp
{
    public unsafe class Decompressor : IDisposable
    {
        private ZSTD_DCtx_s* dctx;

        public Decompressor()
        {
            dctx = Methods.ZSTD_createDCtx();
            if (dctx == null)
            {
                throw new ZstdException(ZSTD_ErrorCode.ZSTD_error_GENERIC, "Failed to create dctx");
            }
        }

        ~Decompressor()
        {
            ReleaseUnmanagedResources();
        }

        public void LoadDictionary(byte[] dict)
        {
            EnsureNotDisposed();
            if (dict == null)
            {
                Methods.ZSTD_DCtx_loadDictionary(dctx, null, 0).EnsureZstdSuccess();
            }
            else
            {

                fixed (byte* dictPtr = dict)
                {
                    Methods.ZSTD_DCtx_loadDictionary(dctx, dictPtr, (nuint) dict.Length).EnsureZstdSuccess();
                }
            }
        }

        public static ulong GetDecompressedSize(ReadOnlySpan<byte> src)
        {
            fixed (byte* srcPtr = src)
            {
                return Methods.ZSTD_decompressBound(srcPtr, (nuint) src.Length).EnsureContentSizeOk();
            }
        }

        public static ulong GetDecompressedSize(ArraySegment<byte> src)
            => GetDecompressedSize((ReadOnlySpan<byte>) src);

        public static ulong GetDecompressedSize(byte[] src, int srcOffset, int srcLength)
            => GetDecompressedSize(new ReadOnlySpan<byte>(src, srcOffset, srcLength));

        public Span<byte> Unwrap(ReadOnlySpan<byte> src, int maxDecompressedSize = int.MaxValue)
        {
            var expectedDstSize = GetDecompressedSize(src);
            if (expectedDstSize > (ulong) maxDecompressedSize)
                throw new ZstdException(ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall,
                    $"Decompressed content size {expectedDstSize} is greater than {nameof(maxDecompressedSize)} {maxDecompressedSize}");
            if (expectedDstSize > Constants.MaxByteArrayLength)
                throw new ZstdException(ZSTD_ErrorCode.ZSTD_error_dstSize_tooSmall,
                    $"Decompressed content size {expectedDstSize} is greater than max possible byte array size {Constants.MaxByteArrayLength}");

            var dest = new byte[expectedDstSize];
            var length = Unwrap(src, dest);
            return new Span<byte>(dest, 0, length);
        }

        public int Unwrap(byte[] src, byte[] dest, int offset) 
            => Unwrap(src, new Span<byte>(dest, offset, dest.Length - offset));

        public int Unwrap(ReadOnlySpan<byte> src, Span<byte> dest)
        {
            EnsureNotDisposed();
            fixed (byte* srcPtr = src)
            fixed (byte* destPtr = dest)
            {
                return (int) Methods.ZSTD_decompressDCtx(dctx, destPtr, (nuint) dest.Length, srcPtr, (nuint) src.Length)
                    .EnsureZstdSuccess();
            }
        }

        public int Unwrap(byte[] src, int srcOffset, int srcLength, byte[] dst, int dstOffset, int dstLength)
            => Unwrap(new ReadOnlySpan<byte>(src, srcOffset, srcLength), new Span<byte>(dst, dstOffset, dstLength));

        private void ReleaseUnmanagedResources()
        {
            if (dctx != null)
            {
                Methods.ZSTD_freeDCtx(dctx);
                dctx = null;
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        private void EnsureNotDisposed()
        {
            if (dctx == null)
            {
                throw new ObjectDisposedException(nameof(Decompressor));
            }
        }
    }
}
