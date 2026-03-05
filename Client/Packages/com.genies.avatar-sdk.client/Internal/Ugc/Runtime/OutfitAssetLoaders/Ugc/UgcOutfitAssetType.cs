namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class UgcOutfitAssetType
#else
    public static class UgcOutfitAssetType
#endif
    {
        public const string Ugc = "ugc";
        public const string UgcDefault = "ugc-default";
        public const string Gear = "gear";
    }
}
