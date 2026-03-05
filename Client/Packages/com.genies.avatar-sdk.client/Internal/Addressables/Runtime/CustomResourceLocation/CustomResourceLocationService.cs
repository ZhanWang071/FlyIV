using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine.ResourceManagement;

namespace Genies.Addressables.CustomResourceLocation
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CustomResourceLocationService : BaseAddressablesService
#else
    public class CustomResourceLocationService : BaseAddressablesService
#endif
    {
        public static void InitializeResourceProviders()
        {
            ResourceManager rm = UnityEngine.AddressableAssets.Addressables.ResourceManager;

            if (rm.ResourceProviders.All(provider => provider.ProviderId != CustomBundledAssetProvider.CustomProviderId))
            {
                rm.ResourceProviders.Add(new CustomBundledAssetProvider(CustomBundledAssetProvider.CustomProviderId));
            }

            if (rm.ResourceProviders.All(provider => provider.ProviderId != CustomAssetBundleProvider.CustomProviderId))
            {
                rm.ResourceProviders.Add(new CustomAssetBundleProvider(CustomAssetBundleProvider.CustomProviderId));
            }
        }
    }
}
