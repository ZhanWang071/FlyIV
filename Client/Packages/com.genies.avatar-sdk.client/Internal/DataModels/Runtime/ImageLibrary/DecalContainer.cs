namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DecalContainer : ImageLibraryContainer
#else
    public class DecalContainer : ImageLibraryContainer
#endif
    {
        public override ImageLibraryAssetType AssetType => ImageLibraryAssetType.decal;
    }
}
