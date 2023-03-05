# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.2  
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


# Benchmark

Best performance is achieved on `.NET`. `System.Runtime.Intrinsics` namespace is required for hardware accelerated bit and vector operations. `.NET Standard` and `.NET Framework` will use software implementation

Comparision `zstd` (native) and `ZstdSharp`  
```
BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2604/21H2/November2021Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK=7.0.101
  [Host]     : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.1 (7.0.122.56804), X64 RyuJIT AVX2
```

Compression level 1
|           Method |      Mean |     Error |    StdDev | Ratio |
|----------------- |----------:|----------:|----------:|------:|
|   CompressNative | 22.749 ms | 0.0257 ms | 0.0214 ms |  1.00 |
|    CompressSharp | 32.547 ms | 0.0603 ms | 0.0535 ms |  1.43 |
|                  |           |           |           |       |
| DecompressNative |  5.931 ms | 0.0094 ms | 0.0083 ms |  1.00 |
|  DecompressSharp |  7.352 ms | 0.0141 ms | 0.0118 ms |  1.24 |


Compression level 5
|           Method |      Mean |     Error |    StdDev | Ratio |
|----------------- |----------:|----------:|----------:|------:|
|   CompressNative | 69.107 ms | 0.6110 ms | 0.5715 ms |  1.00 |
|    CompressSharp | 91.785 ms | 0.5487 ms | 0.4582 ms |  1.33 |
|                  |           |           |           |       |
| DecompressNative |  6.709 ms | 0.0343 ms | 0.0304 ms |  1.00 |
|  DecompressSharp |  9.126 ms | 0.0377 ms | 0.0334 ms |  1.36 |

Compression level 15
|           Method |         Mean |      Error |     StdDev | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
|   CompressNative | 2,018.248 ms | 15.9045 ms | 14.8771 ms |  1.00 |
|    CompressSharp | 2,093.249 ms | 10.4420 ms |  9.7674 ms |  1.04 |
|                  |              |            |            |       |
| DecompressNative |     5.692 ms |  0.0495 ms |  0.0439 ms |  1.00 |
|  DecompressSharp |     7.287 ms |  0.0200 ms |  0.0187 ms |  1.28 |
