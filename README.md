# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.5  
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
| CompressNative   | 24.356 ms | 0.1083 ms | 0.1013 ms |  1.00 |
| CompressSharp    | 29.433 ms | 0.0486 ms | 0.0380 ms |  1.21 |
|                  |           |           |           |       |
| DecompressNative |  5.450 ms | 0.0173 ms | 0.0162 ms |  1.00 |
| DecompressSharp  |  6.200 ms | 0.0369 ms | 0.0345 ms |  1.14 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 69.554 ms | 0.3478 ms | 0.3253 ms |  1.00 |
| CompressSharp    | 84.095 ms | 0.5404 ms | 0.4791 ms |  1.21 |
|                  |           |           |           |       |
| DecompressNative |  6.068 ms | 0.0173 ms | 0.0153 ms |  1.00 |
| DecompressSharp  |  7.171 ms | 0.0458 ms | 0.0428 ms |  1.18 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,302.852 ms | 17.3599 ms | 16.2385 ms |  1.00 |
| CompressSharp    | 2,051.227 ms |  8.6078 ms |  7.6306 ms |  0.89 |
|                  |              |            |            |       |
| DecompressNative |     5.289 ms |  0.0207 ms |  0.0194 ms |  1.00 |
| DecompressSharp  |     6.050 ms |  0.0212 ms |  0.0166 ms |  1.14 |
