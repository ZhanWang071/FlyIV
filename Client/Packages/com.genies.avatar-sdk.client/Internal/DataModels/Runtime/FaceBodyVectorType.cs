namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum FaceVectorType
#else
    public enum FaceVectorType
#endif
    {
        EyeSize,
        EyeVerticalPosition,
        EyeSpacing,
        EyeRotation,
        BrowThickness,
        BrowLength,
        BrowVerticalPosition,
        BrowSpacing,
        NoseWidth,
        NoseLength,
        NoseVerticalPosition,
        NoseTilt,
        NoseProjection,
        LipWidth,
        LipFullness,
        LipVerticalPosition,
        JawWidth,
        JawLength,
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum BodyVectorType
#else
    public enum BodyVectorType
#endif
    {
        NeckThickness,
        ShoulderBroadness,
        ChestBustline,
        ArmsThickness,
        WaistThickness,
        BellyFullness,
        HipsThickness,
        LegsThickness,
    }
}
