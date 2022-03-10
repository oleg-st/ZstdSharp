# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.1  
Supports .NET Core 3.1, .NET 5, .NET 6, .NET Standard 2.0+, .NET Framework 4.6.1+

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
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19043.1415 (21H1/May2021Update)
Intel Core i7-2600K CPU 3.40GHz (Sandy Bridge), 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.101
  [Host]     : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
  DefaultJob : .NET 6.0.1 (6.0.121.56705), X64 RyuJIT
```

Compression level 1
|           Method |     Mean |    Error |   StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |---------:|---------:|---------:|------:|--------:|----------------------:|
|   CompressNative | 45.26 ms | 0.750 ms | 0.702 ms |  1.00 |    0.00 |           399,751,515 |
|    CompressSharp | 64.31 ms | 0.310 ms | 0.259 ms |  1.42 |    0.03 |           524,542,857 |
|                  |          |          |          |       |         |                       |
| DecompressNative | 14.27 ms | 0.281 ms | 0.335 ms |  1.00 |    0.00 |           155,238,636 |
|  DecompressSharp | 20.06 ms | 0.102 ms | 0.085 ms |  1.41 |    0.03 |           188,704,167 |


Compression level 5
|           Method |      Mean |    Error |   StdDev |    Median | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |----------:|---------:|---------:|----------:|------:|--------:|----------------------:|
|   CompressNative | 153.35 ms | 3.049 ms | 8.747 ms | 150.53 ms |  1.00 |    0.00 |         1,103,210,938 |
|    CompressSharp | 198.80 ms | 3.927 ms | 8.368 ms | 195.48 ms |  1.29 |    0.08 |         1,527,494,444 |
|                  |           |          |          |           |       |         |                       |
| DecompressNative |  17.01 ms | 0.226 ms | 0.212 ms |  16.97 ms |  1.00 |    0.00 |           206,341,667 |
|  DecompressSharp |  25.86 ms | 0.513 ms | 1.037 ms |  25.58 ms |  1.56 |    0.07 |           261,462,010 |
