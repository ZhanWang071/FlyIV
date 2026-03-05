
using System;
using System.ComponentModel;
using System.Reflection;

namespace Genies.MakeupPresets
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum MakeupPresetCategory
#else
    public enum MakeupPresetCategory
#endif
    {
        None = -1,
        Stickers = 0,
        Lipstick = 1,
        Freckles = 2,
        FaceGems = 3,
        Eyeshadow = 4,
        Blush = 5,
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum TattooCategory
#else
    public enum TattooCategory
#endif
    {
        General,
    }
}
