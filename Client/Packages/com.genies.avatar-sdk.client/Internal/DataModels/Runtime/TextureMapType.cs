namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum TextureMapType
#else
    public enum TextureMapType
#endif
    {
        AlbedoTransparency,
        Normal,
        MetallicSmoothness,
        RgbaMask,
        Occlusion,
        Emission,
    }
}
