namespace Genies.Naf.Content.AvatarBaseConfig
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class NafContentLocations
#else
    public static class NafContentLocations
#endif
    {
        public static string NafContentUrl => NafContentProd;
        private const string NafContentProd = "https://d2r0ofqkxzbpt1.cloudfront.net";
    }
}