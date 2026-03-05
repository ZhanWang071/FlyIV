namespace Genies.Shaders
{
    /// <summary>
    /// All the properties available on the mega shader that are not specific to regions.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum MegaShaderProperty : byte
#else
    public enum MegaShaderProperty : byte
#endif
    {
        UserColor,
        AlbedoTransparency,
        MetallicSmoothness,
        Emissive,
        EmissiveGain,
        Normal,
        RGBAMask,
        DistressedAlbedoTransparency,
        DistressedColorTint,
        DistressedInnerColor,
        DistressedHoles,
        DistressedMetallicSmoothness,
        DistressedEmissive,
        DistressedEmissiveGain,
        DistressedNormal,
        DistressedRGBAMask,
        DistressedScale,
        DistressedShadowPower,
        DistressedRotation,
        DistressedOffset,
        DistressedPatternTexture,
        DistressedPatternScale,
        DistressedPatternRotation,
        DistressedPatternOffset,
        DistressedMaterial,
        DistressedMaterialScale,
        DistressedMaterialRotation,
        DistressedMaterialOffset,
        CustomColors,
        GlitterTiling,
        ChannelExport,
        DecalAlbedoTransparency
    }
}
