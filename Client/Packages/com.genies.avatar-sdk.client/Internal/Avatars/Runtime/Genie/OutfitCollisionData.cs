namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct OutfitCollisionData
#else
    public struct OutfitCollisionData
#endif
    {
        public int Layer;
        public OutfitCollisionType Type;
        public OutfitCollisionMode Mode;
        public OutfitHatHairMode HatHairMode;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum OutfitCollisionType
#else
    public enum OutfitCollisionType
#endif
    {
        Open,
        Closed,
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum OutfitCollisionMode
#else
    public enum OutfitCollisionMode
#endif
    {
        None,
        Blend,
        Simulated,
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal enum OutfitHatHairMode
#else
    public enum OutfitHatHairMode
#endif
    {
        None,
        Blendshape,
        Fallback,
    }
}
