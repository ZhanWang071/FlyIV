namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum FlairAssetType : int
#else
    public enum FlairAssetType : int
#endif
    {
        None = -1,
        Eyebrows = 0,
        Eyelashes = 1,
    }
}