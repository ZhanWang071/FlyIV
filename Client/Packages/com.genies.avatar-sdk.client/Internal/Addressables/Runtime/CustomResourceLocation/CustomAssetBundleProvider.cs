using UnityEngine.ResourceManagement.ResourceProviders;

namespace Genies.Addressables.CustomResourceLocation
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CustomAssetBundleProvider : AssetBundleProvider
#else
    public class CustomAssetBundleProvider : AssetBundleProvider
#endif
    {
        private const string _providerSuffix = "dynamic_custom";
        private static string _customProviderIdOverride;
        public static string CustomProviderId => _customProviderIdOverride ??= $"{typeof(CustomAssetBundleProvider).FullName}{_providerSuffix}";

        public CustomAssetBundleProvider() {}

        public CustomAssetBundleProvider(string providerId = null)
        {
            _customProviderIdOverride = providerId;
            m_ProviderId = CustomProviderId;
        }
    }
}
