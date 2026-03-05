using System.Collections.Generic;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Genies.Addressables.Naf;
using Genies.Addressables.Utils;
using Genies.Login.Native;
using Genies.ServiceManagement;

namespace Genies.Naf.Content
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class NafContentInitializer
#else
    public static class NafContentInitializer
#endif
    {
        public static bool IsInitialized { get; private set; }

        public static UniTask Initialize()
        {
            if (IsInitialized)
            {
                return UniTask.CompletedTask;
            }

            IsInitialized = true;

            // Register the resource provider but do not fetch inventory.
            // Locations will be registered on-demand as assets are added by the default inventory service
            GeniesAddressablesUtils.RegisterNewResourceProviderOnAddressables(new UniversalContentResourceProvider());
            return UniTask.CompletedTask;
        }

    }
}
