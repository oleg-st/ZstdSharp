# ZstdSharp

[![NuGet package](https://img.shields.io/nuget/v/ZstdSharp.Port.svg?logo=NuGet)](https://www.nuget.org/packages/ZstdSharp.Port)

ZstdSharp is a port of [zstd compression library](https://github.com/facebook/zstd) to С#  
Based on Zstandard v1.5.2  
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
BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19044.1566 (21H2)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK=6.0.200
  [Host]     : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
  DefaultJob : .NET 6.0.2 (6.0.222.6406), X64 RyuJIT
```

Compression level 1
|           Method |      Mean |     Error |    StdDev | Ratio | InstructionRetired/Op |
|----------------- |----------:|----------:|----------:|------:|----------------------:|
|   CompressNative | 25.565 ms | 0.2983 ms | 0.2491 ms |  1.00 |           397,370,833 |
|    CompressSharp | 35.616 ms | 0.0593 ms | 0.0495 ms |  1.39 |           523,222,222 |
|                  |           |           |           |       |                       |
| DecompressNative |  6.821 ms | 0.0097 ms | 0.0086 ms |  1.00 |           154,583,333 |
|  DecompressSharp |  8.178 ms | 0.0198 ms | 0.0176 ms |  1.20 |           184,807,292 |


Compression level 5
|           Method |      Mean |     Error |    StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |----------:|----------:|----------:|------:|--------:|----------------------:|
|   CompressNative | 79.408 ms | 0.1196 ms | 0.0933 ms |  1.00 |    0.00 |         1,096,971,429 |
|    CompressSharp | 96.018 ms | 0.3943 ms | 0.3292 ms |  1.21 |    0.00 |         1,528,277,778 |
|                  |           |           |           |       |         |                       |
| DecompressNative |  8.156 ms | 0.1620 ms | 0.1663 ms |  1.00 |    0.00 |           205,591,912 |
|  DecompressSharp | 10.637 ms | 0.0937 ms | 0.0831 ms |  1.31 |    0.03 |           250,476,042 |
