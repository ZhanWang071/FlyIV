namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ColorPresetType
#else
    public enum ColorPresetType
#endif
    {
        None = -1,
        FlairEyelash = 0,
        FlairEyebrow = 1,
        Hair = 2,
        FacialHair = 3,
        Eyes = 4,
        Skin = 5,
        Makeup = 6,
    }
}
