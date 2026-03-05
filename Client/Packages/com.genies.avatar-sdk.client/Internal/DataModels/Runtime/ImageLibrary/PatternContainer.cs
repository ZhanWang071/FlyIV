namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class PatternContainer : ImageLibraryContainer
#else
    public class PatternContainer : ImageLibraryContainer
#endif
    {
        public override ImageLibraryAssetType AssetType => ImageLibraryAssetType.pattern;
    }
}
