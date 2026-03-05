namespace Genies.Shaders
{
    /// <summary>
    /// All the properties available on the mega shader that are specific to regions. This means that each of these properties
    /// will exist for each available region on the mega shader.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum MegaShaderRegionProperty : byte
#else
    public enum MegaShaderRegionProperty : byte
#endif
    {
        // Color
        Color,

        // Pattern
        PatternTexture,
        PatternScale,
        PatternRotation,
        PatternOffset,
        PatternHue,
        PatternSaturation,
        PatternGain,
        PatternDuotone,
        PatternDuoColor1,
        PatternDuoColor2,
        PatternDuoContrast,

        // Surface Texture
        Material,
        MaterialScale,
        MaterialRotation,
        MaterialOffset,
    }
}
