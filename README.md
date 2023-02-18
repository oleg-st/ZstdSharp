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
|           Method |      Mean |     Error |    StdDev | Ratio | InstructionRetired/Op |
|----------------- |----------:|----------:|----------:|------:|----------------------:|
|   CompressNative | 24.743 ms | 0.0862 ms | 0.0764 ms |  1.00 |           349,127,604 |
|    CompressSharp | 35.408 ms | 0.1431 ms | 0.1339 ms |  1.43 |           492,475,556 |
|                  |           |           |           |       |                       |
| DecompressNative |  6.580 ms | 0.0338 ms | 0.0282 ms |  1.00 |           131,053,646 |
|  DecompressSharp |  8.255 ms | 0.0386 ms | 0.0342 ms |  1.25 |           185,826,042 |


Compression level 5
|           Method |       Mean |     Error |    StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |-----------:|----------:|----------:|------:|--------:|----------------------:|
|   CompressNative |  80.846 ms | 1.6085 ms | 2.2549 ms |  1.00 |    0.00 |         1,014,121,693 |
|    CompressSharp | 104.436 ms | 1.2555 ms | 0.9802 ms |  1.30 |    0.04 |         1,476,093,333 |
|                  |            |           |           |       |         |                       |
| DecompressNative |   7.636 ms | 0.0440 ms | 0.0390 ms |  1.00 |    0.00 |           175,539,844 |
|  DecompressSharp |  10.471 ms | 0.0795 ms | 0.0744 ms |  1.37 |    0.01 |           257,408,333 |


Compression level 15
|           Method |         Mean |      Error |     StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |-------------:|-----------:|-----------:|------:|--------:|----------------------:|
|   CompressNative | 2,392.471 ms | 19.2618 ms | 16.0844 ms |  1.00 |    0.00 |         5,508,866,667 |
|    CompressSharp | 2,497.905 ms | 22.7917 ms | 19.0321 ms |  1.04 |    0.01 |         7,915,266,667 |
|                  |              |            |            |       |         |                       |
| DecompressNative |     6.579 ms |  0.1297 ms |  0.1213 ms |  1.00 |    0.00 |           125,783,333 |
|  DecompressSharp |     8.441 ms |  0.1101 ms |  0.0976 ms |  1.29 |    0.03 |           183,633,333 |
