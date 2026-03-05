using UnityEngine.ResourceManagement.ResourceProviders;

namespace Genies.Addressables.CustomResourceLocation
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CustomBundledAssetProvider : BundledAssetProvider
#else
    public class CustomBundledAssetProvider : BundledAssetProvider
#endif
    {
        private const string _providerSuffix = "dynamic_custom";
        private static string _customProviderIdOverride;
        public static string CustomProviderId => _customProviderIdOverride ??= $"{typeof(CustomBundledAssetProvider).FullName}{_providerSuffix}";

        public CustomBundledAssetProvider() {}

        public CustomBundledAssetProvider(string providerId = null)
        {
            _customProviderIdOverride = providerId;
            m_ProviderId = providerId ?? CustomProviderId;
        }
    }
}
