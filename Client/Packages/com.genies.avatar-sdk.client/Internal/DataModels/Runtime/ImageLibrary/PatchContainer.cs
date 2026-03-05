namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PatchContainer : ImageLibraryContainer
#else
    public class PatchContainer : ImageLibraryContainer
#endif
    {
        public override ImageLibraryAssetType AssetType => ImageLibraryAssetType.patch;
    }
}
