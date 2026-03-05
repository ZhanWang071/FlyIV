using GnWrappers;
using Unity.Collections;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class DynamicAccessorExtensions
#else
    public static class DynamicAccessorExtensions
#endif
    {
        public static unsafe string GetDataAsUTF8String(this DynamicAccessor accessor)
        {
            return System.Text.Encoding.UTF8.GetString((byte*)accessor.Data(), (int)accessor.Size());
        }

        public static void CopyTo<T>(this DynamicAccessor accessor, NativeArray<T> destination)
            where T : struct
        {
            using NativeArray<T> source = AsNativeArray<T>(accessor);
            NativeArray<T>.Copy(source, destination);
        }

        public static void CopyTo<T>(this DynamicAccessor accessor, T[] destination)
            where T : struct
        {
            using NativeArray<T> source = AsNativeArray<T>(accessor);
            NativeArray<T>.Copy(source, destination);
        }

        public static NativeArray<T> AsNativeArray<T>(this DynamicAccessor accessor)
            where T : struct
        {
            return NativeArrayUtility.PtrToNativeArray<T>(accessor.Data(), (int)accessor.Size());
        }
    }
}