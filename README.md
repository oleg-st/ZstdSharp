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
BenchmarkDotNet v0.13.10, Windows 10 (10.0.19045.3693/22H2/2022Update)
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK 8.0.100
  [Host]     : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
  DefaultJob : .NET 8.0.0 (8.0.23.53103), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 24.541 ms | 0.0948 ms | 0.0887 ms |  1.00 |
| CompressSharp    | 30.003 ms | 0.1020 ms | 0.0796 ms |  1.22 |
|                  |           |           |           |       |
| DecompressNative |  5.507 ms | 0.0587 ms | 0.0549 ms |  1.00 |
| DecompressSharp  |  6.503 ms | 0.0251 ms | 0.0196 ms |  1.18 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 69.568 ms | 0.5700 ms | 0.5053 ms |  1.00 |
| CompressSharp    | 84.631 ms | 0.6347 ms | 0.5626 ms |  1.22 |
|                  |           |           |           |       |
| DecompressNative |  6.091 ms | 0.0253 ms | 0.0236 ms |  1.00 |
| DecompressSharp  |  7.662 ms | 0.0071 ms | 0.0066 ms |  1.26 |

Compression level 15
| Method           | Mean         | Error     | StdDev    | Ratio |
|----------------- |-------------:|----------:|----------:|------:|
| CompressNative   | 2,233.266 ms | 9.9060 ms | 9.2661 ms |  1.00 |
| CompressSharp    | 2,018.259 ms | 4.0816 ms | 3.4083 ms |  0.90 |
|                  |              |           |           |       |
| DecompressNative |     5.318 ms | 0.0284 ms | 0.0252 ms |  1.00 |
| DecompressSharp  |     6.309 ms | 0.0188 ms | 0.0167 ms |  1.19 |
