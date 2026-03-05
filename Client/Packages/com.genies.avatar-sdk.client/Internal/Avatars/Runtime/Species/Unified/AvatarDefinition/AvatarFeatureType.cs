namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarFeatureType
#else
    public static class AvatarFeatureType
#endif
    {
        public const string DecoratedSkin = "DecoratedSkin";
    }
}