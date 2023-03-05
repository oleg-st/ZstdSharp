# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)
[![NuGet package](https://img.shields.io/nuget/dt/ZstdSharp.Port?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.4  
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
|   CompressNative | 24.628 ms | 0.0589 ms | 0.0550 ms |  1.00 |
|    CompressSharp | 32.838 ms | 0.0749 ms | 0.0625 ms |  1.33 |
|                  |           |           |           |       |
| DecompressNative |  5.536 ms | 0.0121 ms | 0.0108 ms |  1.00 |
|  DecompressSharp |  6.962 ms | 0.0404 ms | 0.0377 ms |  1.26 |

Compression level 5
|           Method |      Mean |     Error |    StdDev | Ratio |
|----------------- |----------:|----------:|----------:|------:|
|   CompressNative | 69.860 ms | 0.2097 ms | 0.1962 ms |  1.00 |
|    CompressSharp | 91.441 ms | 0.2084 ms | 0.1847 ms |  1.31 |
|                  |           |           |           |       |
| DecompressNative |  6.101 ms | 0.0251 ms | 0.0222 ms |  1.00 |
|  DecompressSharp |  8.299 ms | 0.0344 ms | 0.0287 ms |  1.36 |

Compression level 15
|           Method |         Mean |      Error |     StdDev | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
|   CompressNative | 2,238.109 ms | 10.3414 ms |  9.1674 ms |  1.00 |
|    CompressSharp | 2,275.256 ms | 12.6440 ms | 11.8272 ms |  1.02 |
|                  |              |            |            |       |
| DecompressNative |     5.476 ms |  0.0224 ms |  0.0210 ms |  1.00 |
|  DecompressSharp |     6.894 ms |  0.0137 ms |  0.0122 ms |  1.26 |
