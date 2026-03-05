namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BaseAddressableProvider
#else
    public class BaseAddressableProvider
#endif
    {
        private const string DynamicContentUrl = "https://d3vwr5y0neqoqu.cloudfront.net";

        public static string DynBaseUrl
        {
            get
            {
                return DynamicContentUrl;
            }
        }

    }
}
