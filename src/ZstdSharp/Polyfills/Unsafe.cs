// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !NETCOREAPP3_0_OR_GREATER

using InlineIL;
using static InlineIL.IL.Emit;

namespace System.Runtime.CompilerServices
{ 
    public static unsafe class Unsafe
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T As<T>(object o)
            where T : struct
        {
            Ldarg(nameof(o));
            return IL.Return<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref TTo As<TFrom, TTo>(ref TFrom source)
        {
            Ldarg(nameof(source));
            return ref IL.ReturnRef<TTo>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SizeOf<T>()
        {
            Sizeof(typeof(T));
            return IL.Return<int>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T Add<T>(ref T source, int elementOffset)
        {
            Ldarg(nameof(source));
            Ldarg(nameof(elementOffset));
            Sizeof(typeof(T));
            Conv_I();
            Mul();
            IL.Emit.Add();
            return ref IL.ReturnRef<T>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SkipInit<T>(out T value)
        {
            Ret();
            throw IL.Unreachable();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T AsRef<T>(void* source)
        {
            // For .NET Core the roundtrip via a local is no longer needed
#if NETCOREAPP
            IL.Push(source);
            return ref IL.ReturnRef<T>();
#else
            // Roundtrip via a local to avoid type mismatch on return that the JIT inliner chokes on.
            IL.DeclareLocals(
                false,
                new LocalVar("local", typeof(int).MakeByRefType())
            );

            IL.Push(source);
            Stloc("local");
            Ldloc("local");
            return ref IL.ReturnRef<T>();
#endif
        }
    }
}

#endif
