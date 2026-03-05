using System;
using Cysharp.Threading.Tasks;
using Genies.Addressables.UniversalResourceLocation;
using Genies.CrashReporting;
using Genies.Inventory.Installers;
using Genies.Naf.Content;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Naf.Addressables
{
    [AutoResolve]
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NafResourceProviderInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<NafContentInstaller>,
        IRequiresInstaller<InventoryServiceInstaller>
#else
    public class NafResourceProviderInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<NafContentInstaller>,
        IRequiresInstaller<InventoryServiceInstaller>
#endif
    {
        public int OperationOrder => DefaultInstallationGroups.DefaultServices + 2;

        // if this does not work just make a mono that on enable Registers this classes..
        public NafAssetResolverConfig nafResolverConfig;
        public void Install(IContainerBuilder builder)
        {
            if (nafResolverConfig == null)
            {
#if GENIES_INTERNAL
                CrashReporter.Log("Using default NAF asset resolver configuration.");
#endif

                if (NafSettings.TryLoadProject(out NafSettings nafSettings) ||
                    NafSettings.TryLoadDefault(out nafSettings)) // Fallback to default
                {
                    nafResolverConfig = nafSettings.defaultAssetResolverConfig;
                }
            }

            if (nafResolverConfig != null)
            {
                builder.RegisterInstance(nafResolverConfig);
            }
            else
            {
                Debug.LogWarning($"Failed to register instance of {nameof(NafAssetResolverConfig)}.");
            }

            builder.Register<NafContentResourceProvider>(Lifetime.Singleton)
                .As<ICustomResourceProvider>()
                .WithParameter(nafResolverConfig);

            builder.Register<NafContentLocationsFromInventory>(Lifetime.Singleton)
                .As<IInventoryNafLocationsProvider>();
        }

        public async UniTask Initialize()
        {
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            if (NafContentInitializer.IsInitialized is false)
            {
                await NafContentInitializer.Initialize();
            }
        }
    }
}
