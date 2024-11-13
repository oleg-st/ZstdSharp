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
BenchmarkDotNet v0.14.0, Windows 10 (10.0.19045.5011/22H2/2022Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK 9.0.100
  [Host]   : .NET 8.0.11 (8.0.1124.51707), X64 RyuJIT AVX2
  .NET 9.0 : .NET 9.0.0 (9.0.24.52809), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 24.596 ms | 0.0341 ms | 0.0285 ms |  1.00 |
| CompressSharp    | 25.318 ms | 0.0329 ms | 0.0275 ms |  1.03 |
|                  |           |           |           |       |
| DecompressNative |  5.642 ms | 0.0145 ms | 0.0128 ms |  1.00 |
| DecompressSharp  |  5.896 ms | 0.0053 ms | 0.0045 ms |  1.05 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 69.903 ms | 0.3729 ms | 0.3306 ms |  1.00 |
| CompressSharp    | 78.997 ms | 0.1981 ms | 0.1756 ms |  1.13 |
|                  |           |           |           |       |
| DecompressNative |  6.214 ms | 0.0154 ms | 0.0137 ms |  1.00 |
| DecompressSharp  |  6.746 ms | 0.0083 ms | 0.0069 ms |  1.09 |

Compression level 15
| Method           | Mean         | Error      | StdDev    | Ratio |
|----------------- |-------------:|-----------:|----------:|------:|
| CompressNative   | 2,237.101 ms | 10.1419 ms | 9.4868 ms |  1.00 |
| CompressSharp    | 2,008.744 ms |  6.2452 ms | 5.5362 ms |  0.90 |
|                  |              |            |           |       |
| DecompressNative |     5.385 ms |  0.0088 ms | 0.0074 ms |  1.00 |
| DecompressSharp  |     5.687 ms |  0.0203 ms | 0.0180 ms |  1.06 |

