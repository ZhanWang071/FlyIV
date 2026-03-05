
namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct AddressablesCatalogConfig
#else
    public struct AddressablesCatalogConfig
#endif
    {
        public AddressablesCatalogContentType ContentType;
        public string CatalogBaseUrl;
        public string[] ExcludeContentTypes;
    }
}
