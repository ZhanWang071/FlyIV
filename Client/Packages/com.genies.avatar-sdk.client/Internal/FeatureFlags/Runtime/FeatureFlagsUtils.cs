
namespace Genies.FeatureFlags
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class FeatureFlagsUtils
#else
    public static class FeatureFlagsUtils
#endif
    {
        public const string FolderPath = "Assets/Genies/Resources";
        public const string FolderName = "FeatureFlags";
        public const string FileName = "localfeatureflagsfile";
        public static string FilePath = $"{FolderPath}/{FolderName}/{FileName}.json";
    }
}
