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
|   CompressNative | 24.709 ms | 0.0282 ms | 0.0250 ms |  1.00 |
|    CompressSharp | 33.090 ms | 0.0500 ms | 0.0443 ms |  1.34 |
|                  |           |           |           |       |
| DecompressNative |  5.568 ms | 0.0166 ms | 0.0155 ms |  1.00 |
|  DecompressSharp |  6.974 ms | 0.0086 ms | 0.0076 ms |  1.25 |

Compression level 5
|           Method |      Mean |     Error |    StdDev | Ratio |
|----------------- |----------:|----------:|----------:|------:|
|   CompressNative | 70.781 ms | 0.1872 ms | 0.1563 ms |  1.00 |
|    CompressSharp | 94.276 ms | 0.2301 ms | 0.2040 ms |  1.33 |
|                  |           |           |           |       |
| DecompressNative |  6.221 ms | 0.0094 ms | 0.0078 ms |  1.00 |
|  DecompressSharp |  8.484 ms | 0.0134 ms | 0.0105 ms |  1.36 |

Compression level 15
|           Method |         Mean |      Error |     StdDev | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
|   CompressNative | 2,304.967 ms | 19.6909 ms | 17.4555 ms |  1.00 |
|    CompressSharp | 2,336.182 ms | 19.8808 ms | 17.6238 ms |  1.01 |
|                  |              |            |            |       |
| DecompressNative |     5.541 ms |  0.0174 ms |  0.0145 ms |  1.00 |
|  DecompressSharp |     6.987 ms |  0.0227 ms |  0.0201 ms |  1.26 |
