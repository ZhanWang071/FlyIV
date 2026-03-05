namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AddressablesCatalogContentType
#else
    public enum AddressablesCatalogContentType
#endif
    {
        Static,
        Generative,
        Looks,
        Library,
        Dynamic,
        DynamicExternal,
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AddressableCatalogContentTypeExtensions
#else
    public static class AddressableCatalogContentTypeExtensions
#endif
    {
        public static string ToLowercaseString(this AddressablesCatalogContentType enumValue)
        {
            return enumValue.ToString().ToLower();
        }
    }
}
