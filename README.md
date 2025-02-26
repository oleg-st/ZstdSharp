# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.7  
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
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5487/22H2/2022Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.100
  [Host]   : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 25.086 ms | 0.0423 ms | 0.0353 ms |  1.00 |
| CompressSharp    | 25.118 ms | 0.0419 ms | 0.0350 ms |  1.00 |
|                  |           |           |           |       |
| DecompressNative |  5.225 ms | 0.0079 ms | 0.0070 ms |  1.00 |
| DecompressSharp  |  5.858 ms | 0.0107 ms | 0.0095 ms |  1.12 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 71.237 ms | 0.2326 ms | 0.1942 ms |  1.00 |
| CompressSharp    | 80.301 ms | 0.2706 ms | 0.2399 ms |  1.13 |
|                  |           |           |           |       |
| DecompressNative |  6.131 ms | 0.0104 ms | 0.0092 ms |  1.00 |
| DecompressSharp  |  6.710 ms | 0.0137 ms | 0.0121 ms |  1.09 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,240.476 ms |  8.2758 ms |  7.7412 ms |  1.00 |
| CompressSharp    | 2,026.139 ms | 13.6936 ms | 12.1390 ms |  0.90 |
|                  |              |            |            |       |
| DecompressNative |     5.292 ms |  0.0098 ms |  0.0087 ms |  1.00 |
| DecompressSharp  |     5.649 ms |  0.0187 ms |  0.0166 ms |  1.07 |

