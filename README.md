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
| CompressNative   | 23.433 ms | 0.0336 ms | 0.0262 ms |  1.00 |
| CompressSharp    | 24.154 ms | 0.0295 ms | 0.0230 ms |  1.03 |
|                  |           |           |           |       |
| DecompressNative |  6.195 ms | 0.0082 ms | 0.0068 ms |  1.00 |
| DecompressSharp  |  5.638 ms | 0.0045 ms | 0.0038 ms |  0.91 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 67.810 ms | 0.3842 ms | 0.3208 ms |  1.00 |
| CompressSharp    | 72.678 ms | 0.8016 ms | 0.7106 ms |  1.07 |
|                  |           |           |           |       |
| DecompressNative |  8.388 ms | 0.0159 ms | 0.0133 ms |  1.00 |
| DecompressSharp  |  6.771 ms | 0.0091 ms | 0.0076 ms |  0.81 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,229.371 ms | 11.3172 ms | 10.0324 ms |  1.00 |
| CompressSharp    | 1,995.334 ms |  7.4021 ms |  6.5618 ms |  0.90 |
|                  |              |            |            |       |
| DecompressNative |     6.550 ms |  0.0122 ms |  0.0108 ms |  1.00 |
| DecompressSharp  |     5.697 ms |  0.0388 ms |  0.0344 ms |  0.87 |

