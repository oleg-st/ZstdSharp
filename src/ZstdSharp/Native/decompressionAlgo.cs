using System;
using System.Runtime.InteropServices;

namespace ZstdSharp
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate nuint decompressionAlgo(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize);
}
