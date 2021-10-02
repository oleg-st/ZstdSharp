# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.0  
Supports .NET Core 3.1, .NET 5, .NET Standard 2.0+, .NET Framework 4.6.1+

# Usage  

ZstdSharp has an unsafe API much the same as zstd.  
There are also safe wrappers.

Compress:
```c#
var src = File.ReadAllBytes("dickens");
using var compressor = new Compressor(level);
var compressed = compressor.Wrap(src);
```

Decompress:
```c#
var src = File.ReadAllBytes("dickens.zst");
using var decompressor = new Decompressor();
var decompressed = decompressor.Unwrap(src);
```

# Benchmark

Best performance is achieved on `.NET Core`. `System.Runtime.Intrinsics` namespace is required for hardware accelerated bit and vector operations. `.NET Standard` and `.NET Framework` will use software implementation

Comparision `zstd` (native) and `ZstdSharp`  
```
BenchmarkDotNet=v0.12.1, OS=Windows 10.0.19041.1052 (2004/?/20H1)
Intel Core i7-2600K CPU 3.40GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=5.0.204
  [Host]     : .NET Core 5.0.7 (CoreCLR 5.0.721.25508, CoreFX 5.0.721.25508), X64 RyuJIT
  DefaultJob : .NET Core 5.0.7 (CoreCLR 5.0.721.25508, CoreFX 5.0.721.25508), X64 RyuJIT
```

Compression level 1
|           Method |     Mean |    Error |   StdDev | Ratio | RatioSD |
|----------------- |---------:|---------:|---------:|------:|--------:|
|   CompressNative | 51.20 ms | 1.022 ms | 1.621 ms |  1.00 |    0.00 |
|    CompressSharp | 68.76 ms | 1.287 ms | 1.074 ms |  1.34 |    0.05 |
|                  |          |          |          |       |         |
| DecompressNative | 15.64 ms | 0.253 ms | 0.224 ms |  1.00 |    0.00 |
|  DecompressSharp | 20.98 ms | 0.253 ms | 0.224 ms |  1.34 |    0.02 |


Compression level 5
|           Method |      Mean |    Error |   StdDev | Ratio | RatioSD |
|----------------- |----------:|---------:|---------:|------:|--------:|
|   CompressNative | 205.69 ms | 3.887 ms | 4.159 ms |  1.00 |    0.00 |
|    CompressSharp | 212.88 ms | 2.141 ms | 1.898 ms |  1.04 |    0.02 |
|                  |           |          |          |       |         |
| DecompressNative |  21.91 ms | 0.430 ms | 0.528 ms |  1.00 |    0.00 |
|  DecompressSharp |  27.77 ms | 0.528 ms | 0.519 ms |  1.26 |    0.03 |
