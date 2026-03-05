namespace Genies.AssetLocations
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AssetLocationDefaults
#else
    public static class AssetLocationDefaults
#endif
    {
        // need to keep in sync with AssetService..
        // was not included to avoid cyclical deps.. since its just a string.
        public static readonly string[] AssetLods = { "", $"_lod1", $"_lod2", };
        public static readonly string[] IconSizes = { "", $"_x512", $"_x1024", };

    }
}
