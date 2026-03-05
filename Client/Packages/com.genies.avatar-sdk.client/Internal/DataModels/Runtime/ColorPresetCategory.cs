namespace Genies.ColorPresetManager
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ColorPresetCategory
#else
    public enum ColorPresetCategory
#endif
    {
        EyeColor,
        EyeBrowColor,
        FacialHairColor,
        SkinColorIcon,
        HairColor,
        SkinColor,
        MakeupColor
    }
}