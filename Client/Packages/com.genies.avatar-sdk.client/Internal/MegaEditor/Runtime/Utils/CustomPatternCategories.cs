namespace Genies.Looks.Customization.Utils
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class CustomPatternCategories
#else
    public static class CustomPatternCategories
#endif
    {
        public static string MyPatterns => "My Patterns";
    }
}
