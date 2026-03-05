namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ImageLibraryAssetType
#else
    public enum ImageLibraryAssetType
#endif
    {
        none = -1,
        decal = 0,
        patch = 1,
        pattern = 2,
        tattoo = 3,
    }
}
