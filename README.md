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
BenchmarkDotNet=v0.13.2, OS=Windows 10 (10.0.19044.2251/21H2/November2021Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```

Compression level 1
|           Method |      Mean |     Error |    StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |----------:|----------:|----------:|------:|--------:|----------------------:|
|   CompressNative | 26.554 ms | 0.2640 ms | 0.2470 ms |  1.00 |    0.00 |           397,460,417 |
|    CompressSharp | 35.132 ms | 0.2346 ms | 0.2194 ms |  1.32 |    0.02 |           492,346,667 |
|                  |           |           |           |       |         |                       |
| DecompressNative |  7.096 ms | 0.0404 ms | 0.0358 ms |  1.00 |    0.00 |           154,629,167 |
|  DecompressSharp |  8.088 ms | 0.0866 ms | 0.0723 ms |  1.14 |    0.01 |           185,760,417 |


Compression level 5
|           Method |      Mean |     Error |    StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |----------:|----------:|----------:|------:|--------:|----------------------:|
|   CompressNative | 82.992 ms | 1.2924 ms | 1.1456 ms |  1.00 |    0.00 |         1,099,049,020 |
|    CompressSharp | 99.465 ms | 1.1068 ms | 0.8641 ms |  1.20 |    0.02 |         1,475,813,333 |
|                  |           |           |           |       |         |                       |
| DecompressNative |  8.322 ms | 0.1591 ms | 0.1954 ms |  1.00 |    0.00 |           205,625,000 |
|  DecompressSharp | 10.257 ms | 0.1346 ms | 0.1259 ms |  1.23 |    0.04 |           257,361,458 |


Compression level 15
|           Method |         Mean |      Error |     StdDev | Ratio | RatioSD | InstructionRetired/Op |
|----------------- |-------------:|-----------:|-----------:|------:|--------:|----------------------:|
|   CompressNative | 2,471.505 ms | 35.0230 ms | 31.0470 ms |  1.00 |    0.00 |         6,897,800,000 |
|    CompressSharp | 2,546.185 ms | 31.8153 ms | 29.7601 ms |  1.03 |    0.02 |         7,912,600,000 |
|                  |              |            |            |       |         |                       |
| DecompressNative |     7.348 ms |  0.0962 ms |  0.0900 ms |  1.00 |    0.00 |           146,806,337 |
|  DecompressSharp |     8.510 ms |  0.1176 ms |  0.1100 ms |  1.16 |    0.02 |           183,541,667 |
