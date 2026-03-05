using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Naf.Content.AvatarBaseConfig;
using Genies.Inventory.Installers;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Naf.Content
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NafContentInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<LocationsFromInventoryInstaller>
#else
    public class NafContentInstaller : IGeniesInstaller, IGeniesInitializer,
        IRequiresInstaller<LocationsFromInventoryInstaller>
#endif
    {
        public int OperationOrder => (DefaultInstallationGroups.DefaultServices);
        // public int InitializationOrder => DefaultInstallationGroups.DefaultServices + 2;

        // Only load inventory V1 when we enable support for old AIGC wearables
        public bool IncludeInventoryV1 = false;

        public void Install(IContainerBuilder builder)
        {
            // Register ContentConfigService as singleton
            builder.Register<IContentConfigService, SimpleContentConfigService>(Lifetime.Singleton)
                .AsSelf();

            builder.Register<NafContentService>(Lifetime.Singleton)
                .WithParameter(IncludeInventoryV1)
                .AsSelf()
                .As<IAssetParamsService>()
                .As<IAssetIdConverter>();

            // Register inventory event handler to respond to asset minting events
            builder.Register<NafInventoryEventHandler>(Lifetime.Singleton);
        }

        public UniTask Initialize()
        {
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            // Initialize the inventory event handler to subscribe to events
            var eventHandler = this.GetService<NafInventoryEventHandler>();
            eventHandler?.Initialize();

            return UniTask.CompletedTask;
        }
    }
}
