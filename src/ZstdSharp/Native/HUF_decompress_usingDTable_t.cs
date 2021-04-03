using System;
using System.Runtime.InteropServices;

namespace ZstdSharp
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate nuint HUF_decompress_usingDTable_t(void* dst, nuint dstSize, void* cSrc, nuint cSrcSize, uint* DTable);
}
