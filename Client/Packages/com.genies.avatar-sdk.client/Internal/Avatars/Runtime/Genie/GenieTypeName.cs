namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class GenieTypeName
#else
    public static class GenieTypeName
#endif
    {
        public const string Uma = "uma";
        public const string NonUma = "non-uma";
    }
}
