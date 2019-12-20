using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Formats.Alembic.Importer
{
    public static class ExtensionMethods
    {
        public static NativeArray<T> ResizeIfNeeded<T>(this ref NativeArray<T> src, int newSize, Allocator alloc = Allocator.Persistent) where T : struct
        {
            if (src.Length == newSize) return src;
            if (src.IsCreated)
            {
                src.Dispose();
            }

            src = new NativeArray<T>(newSize, alloc);
            return src;
        }

        public static void DisposeIfNeeded<T>(this ref NativeArray<T> src) where T : struct
        {
            if (src.IsCreated)
                src.Dispose();
        }

        public static unsafe IntPtr GetUnsafePointer<T>(this ref NativeArray<T> src) where T : struct
        {
            return src.IsCreated && src.Length>0 ? (IntPtr)src.GetUnsafePtr() : new IntPtr(); // 0 size arrays are considered unallocated
        }
    }
}
