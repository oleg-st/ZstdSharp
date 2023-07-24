// IL2CPP doesn't work well with pointers, so we replace nuint/nint with 64-bit integers
#if ENABLE_IL2CPP
global using nuint = System.UInt64;
global using nint = System.Int64;
#endif
