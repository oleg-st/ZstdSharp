# ZstdSharp
ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Supports .NET Core 3.1 and .NET 5

# Usage  

ZstdSharp has an unsafe API much the same as zstd.  
There are no safe wrappers yet.

Compress:

```c#
var cctx = ZstdSharp.Methods.ZSTD_createCCtx();

var src = File.ReadAllBytes("dickens");
var compressed = new byte[ZstdSharp.Methods.ZSTD_compressBound((nuint)src.Length)];
fixed (byte* srcPtr = src)
fixed (byte* compressedPtr = compressed)
{
    var compressedLength = ZstdSharp.Methods.ZSTD_compressCCtx(cctx, compressedPtr, (nuint)compressed.Length,
        srcPtr, (nuint)src.Length, level);
}

ZstdSharp.Methods.ZSTD_freeCCtx(cctx);
```

Decompress:
```c#
var dctx = ZstdSharp.Methods.ZSTD_createDCtx();

var src = File.ReadAllBytes("dickens.zst");
fixed (byte* srcPtr = src)
{
    var uncompressed = new byte[ZstdSharp.Methods.ZSTD_decompressBound(srcPtr, (nuint) src.Length)];
    fixed (byte* uncompressedPtr = uncompressed)
    {
        var decompressedLength = ZstdSharp.Methods.ZSTD_decompressDCtx(dctx, uncompressedPtr, (nuint) uncompressed.Length,
            srcPtr, (nuint) src.Length);
    }
}

ZstdSharp.Methods.ZSTD_freeDCtx(dctx);
```

# Benchmark

Comparision `zstd` (native) and `ZstdSharp`, compression level = 1.

```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.867 (2004/?/20H1)
Intel Core i7-2600K CPU 3.40GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.201
  [Host]     : .NET Core 3.1.13 (CoreCLR 4.700.21.11102, CoreFX 4.700.21.11602), X64 RyuJIT
  DefaultJob : .NET Core 3.1.13 (CoreCLR 4.700.21.11102, CoreFX 4.700.21.11602), X64 RyuJIT
```

|           Method |     Mean |    Error |   StdDev | Ratio |
|----------------- |---------:|---------:|---------:|------:|
|   CompressNative | 48.99 ms | 0.056 ms | 0.047 ms |  1.00 |
|    CompressSharp | 78.29 ms | 0.318 ms | 0.298 ms |  1.60 |
|                  |          |          |          |       |
| DecompressNative | 16.54 ms | 0.098 ms | 0.087 ms |  1.00 |
|  DecompressSharp | 21.91 ms | 0.110 ms | 0.103 ms |  1.32 |
