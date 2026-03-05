namespace Genies.Avatars
{
    /// <summary>
    /// Represents a tattoo transformation preset identified by a unique ID.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct TattooTransformPreset
#else
    public struct TattooTransformPreset
#endif
    {
        public string Id;
        public float PositionX;
        public float PositionY;
        public float Rotation;
        public float Scale;
    }
}