using System;
using System.Runtime.InteropServices;
using GnWrappers;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class MaterialPropertiesExtensions
#else
    public static class MaterialPropertiesExtensions
#endif
    {
        public delegate void PropertyProcessor<T>(string name, T value) where T : struct;

        public static void Process<T>(this MaterialProperties properties, PropertyProcessor<T> processor)
            where T : struct
        {
            uint size = properties.Size();
            for (uint i = 0; i < size; ++i)
            {
                IntPtr valuePtr = properties.Value(i);
                T value = Marshal.PtrToStructure<T>(valuePtr);
                processor(properties.Key(i), value);
            }
        }
    }
}