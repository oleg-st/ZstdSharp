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
12th Gen Intel Core i7-12700, 1 CPU, 20 logical and 12 physical cores
.NET SDK 8.0.205
  [Host]   : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
  .NET 8.0 : .NET 8.0.5 (8.0.524.21615), X64 RyuJIT AVX2
```

Compression level 1
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 24.590 ms | 0.0499 ms | 0.0442 ms |  1.00 |
| CompressSharp    | 29.681 ms | 0.0409 ms | 0.0362 ms |  1.21 |
|                  |           |           |           |       |
| DecompressNative |  5.769 ms | 0.0217 ms | 0.0203 ms |  1.00 |
| DecompressSharp  |  6.033 ms | 0.0206 ms | 0.0183 ms |  1.05 |

Compression level 5
| Method           | Mean      | Error     | StdDev    | Ratio |
|----------------- |----------:|----------:|----------:|------:|
| CompressNative   | 70.167 ms | 1.1738 ms | 0.9802 ms |  1.00 |
| CompressSharp    | 83.226 ms | 0.8946 ms | 0.7931 ms |  1.19 |
|                  |           |           |           |       |
| DecompressNative |  6.283 ms | 0.0317 ms | 0.0281 ms |  1.00 |
| DecompressSharp  |  6.953 ms | 0.0233 ms | 0.0218 ms |  1.11 |

Compression level 15
| Method           | Mean         | Error      | StdDev     | Ratio |
|----------------- |-------------:|-----------:|-----------:|------:|
| CompressNative   | 2,313.422 ms | 12.1522 ms | 11.3671 ms |  1.00 |
| CompressSharp    | 2,088.364 ms |  7.1917 ms |  6.3752 ms |  0.90 |
|                  |              |            |            |       |
| DecompressNative |     5.476 ms |  0.0470 ms |  0.0439 ms |  1.00 |
| DecompressSharp  |     5.852 ms |  0.0463 ms |  0.0433 ms |  1.07 |

