namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TattooContainer : ImageLibraryContainer
#else
    public class TattooContainer : ImageLibraryContainer
#endif
    {
        public override ImageLibraryAssetType AssetType => ImageLibraryAssetType.tattoo;
    }
}
