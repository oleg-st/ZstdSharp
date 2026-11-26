# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.6.0\
Supports .NET Core 3.1, .NET 5+, .NET Standard 2.0+, .NET Framework 4.6.1+

# Usage  

ZstdSharp has an unsafe API much the same as zstd.  
There are also safe wrappers.

Compress data:
```c#
var src = File.ReadAllBytes("dickens");
using var compressor = new Compressor(level);
var compressed = compressor.Wrap(src);
```

Decompress data:
```c#
var src = File.ReadAllBytes("dickens.zst");
using var decompressor = new Decompressor();
var decompressed = decompressor.Unwrap(src);
```

Streaming compression:
```c#
using var input = File.OpenRead("dickens");
using var output = File.OpenWrite("dickens.zst");
using var compressionStream = new CompressionStream(output, level);
input.CopyTo(compressionStream);
```

Streaming decompression:
```c#
using var input = File.OpenRead("dickens.zst");
using var output = File.OpenWrite("dickens");
using var decompressionStream = new DecompressionStream(input);
decompressionStream.CopyTo(output);
```

Multi-threaded compression:
```c#
using var input = File.OpenRead("dickens");
using var output = File.OpenWrite("dickens.zst");
using var compressionStream = new CompressionStream(output, level);
compressionStream.SetParameter(ZSTD_cParameter.ZSTD_c_nbWorkers, Environment.ProcessorCount);
input.CopyTo(compressionStream);
```

Delta compression ("patch-from", like `zstd --patch-from`) — compress a new version of the data
against the previous version referenced as a prefix (`ZSTD_CCtx_refPrefix`). The prefix applies
to the next frame only; reference it again before each frame:
```c#
// windowLog must cover prefix + input
var windowLog = 10;
while (windowLog < 31 && (1L << windowLog) < (long)oldVersion.Length + newVersion.Length)
    windowLog++;

using var compressor = new Compressor(level);
compressor.SetParameter(ZSTD_cParameter.ZSTD_c_enableLongDistanceMatching, 1);
compressor.SetParameter(ZSTD_cParameter.ZSTD_c_windowLog, windowLog);
compressor.RefPrefix(oldVersion);
var delta = compressor.Wrap(newVersion);

using var decompressor = new Decompressor();
decompressor.SetParameter(ZSTD_dParameter.ZSTD_d_windowLogMax, windowLog);
decompressor.RefPrefix(oldVersion);
var restored = decompressor.Unwrap(delta);
```
Note: prefer `RefPrefix` over `LoadDictionary` for delta compression. `LoadDictionary` builds a
CDict whose effectiveness degrades for reference contents larger than ~32-64 MB (inherited zstd
behavior), while a prefix is indexed with the context parameters and has no such limit.


# Benchmark

Best performance is achieved on `.NET`. `System.Runtime.Intrinsics` namespace is required for hardware accelerated bit and vector operations. `.NET Standard` and `.NET Framework` will use software implementation

Comparision `zstd` (native) and `ZstdSharp`  
```
BenchmarkDotNet v0.15.2, Windows 10 (10.0.19045.6456/22H2/2022Update)
12th Gen Intel Core i7-12700 2.10GHz, 1 CPU, 20 logical and 12 physical cores
.NET SDK 10.0.401
  [Host]    : .NET 9.0.20 (9.0.2026.41315), X64 RyuJIT AVX2
  .NET 10.0 : .NET 10.0.12 (10.0.1226.42308), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 23.523 ms | 0.0441 ms | 0.0369 ms |  1.00 |
| CompressSharp    | 23.492 ms | 0.1273 ms | 0.1128 ms |  1.00 |
|                  |           |           |           |       |
| DecompressNative |  6.216 ms | 0.0089 ms | 0.0074 ms |  1.00 |
| DecompressSharp  |  5.669 ms | 0.0084 ms | 0.0070 ms |  0.91 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 68.298 ms | 0.2423 ms | 0.1892 ms |  1.00 |
| CompressSharp    | 69.679 ms | 0.4058 ms | 0.3389 ms |  1.02 |
|                  |           |           |           |       |
| DecompressNative |  8.414 ms | 0.0212 ms | 0.0177 ms |  1.00 |
| DecompressSharp  |  6.774 ms | 0.0116 ms | 0.0097 ms |  0.81 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,271.252 ms | 13.4401 ms | 12.5718 ms |  1.00 |
| CompressSharp    | 1,848.154 ms |  6.0165 ms |  5.6278 ms |  0.81 |
|                  |              |            |            |       |
| DecompressNative |     6.551 ms |  0.0117 ms |  0.0104 ms |  1.00 |
| DecompressSharp  |     5.698 ms |  0.0078 ms |  0.0065 ms |  0.87 |

