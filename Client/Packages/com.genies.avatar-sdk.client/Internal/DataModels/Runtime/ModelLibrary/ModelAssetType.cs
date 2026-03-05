namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum ModelAssetType
#else
    public enum ModelAssetType
#endif
    {
        None = 0,
        Decor = 1,
    }
}