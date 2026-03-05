using System;

namespace Genies.Customization.Framework
{
    [Flags]
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum CustomizerViewFlags
#else
    public enum CustomizerViewFlags
#endif
    {
        None = 0,
        NavBar = 1 << 0,
        ActionBar = 1 << 1,
        Breadcrumbs = 1 << 2,
        CustomizationEditor = 1 << 3
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal static class CustomizerViewFlagsExtensions
#else
    public static class CustomizerViewFlagsExtensions
#endif
    {
        public static bool HasFlagFast(this CustomizerViewFlags value, CustomizerViewFlags flag)
        {
            return (value & flag) != 0;
        }
    }

}