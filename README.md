# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.6  
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


# Benchmark

Best performance is achieved on `.NET`. `System.Runtime.Intrinsics` namespace is required for hardware accelerated bit and vector operations. `.NET Standard` and `.NET Framework` will use software implementation

Comparision `zstd` (native) and `ZstdSharp`  
```
BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.4170/22H2/2022Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK 8.0.101
  [Host]     : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.1 (8.0.123.58001), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 24.585 ms | 0.1306 ms | 0.1158 ms |  1.00 |
| CompressSharp    | 29.783 ms | 0.0920 ms | 0.0719 ms |  1.21 |
|                  |           |           |           |       |
| DecompressNative |  5.754 ms | 0.0270 ms | 0.0226 ms |  1.00 |
| DecompressSharp  |  6.264 ms | 0.0525 ms | 0.0491 ms |  1.09 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 69.339 ms | 0.5648 ms | 0.4716 ms |  1.00 |
| CompressSharp    | 82.499 ms | 0.3777 ms | 0.3533 ms |  1.19 |
|                  |           |           |           |       |
| DecompressNative |  6.308 ms | 0.0508 ms | 0.0475 ms |  1.00 |
| DecompressSharp  |  7.308 ms | 0.0437 ms | 0.0409 ms |  1.16 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,265.331 ms | 17.3366 ms | 16.2166 ms |  1.00 |
| CompressSharp    | 2,041.183 ms | 16.9635 ms | 15.8677 ms |  0.90 |
|                  |              |            |            |       |
| DecompressNative |     5.461 ms |  0.0299 ms |  0.0280 ms |  1.00 |
| DecompressSharp  |     6.230 ms |  0.0253 ms |  0.0237 ms |  1.14 |

