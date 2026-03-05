using Genies.Addressables;
using Genies.Assets.Services;
using Genies.ServiceManagement;
using Genies.Ugc;
using VContainer;

namespace Genies.Avatars.Context
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarContextServicesInstaller : IGeniesInstaller, IRequiresInstaller<AddressableServicesInstaller>
#else
    public class AvatarContextServicesInstaller : IGeniesInstaller, IRequiresInstaller<AddressableServicesInstaller>
#endif
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<ProjectedTexturesProvider>(Lifetime.Singleton);
            builder.Register<IAssetLoader<UgcTemplateAsset>, UgcTemplateLoader>(Lifetime.Singleton);
            builder.Register<IAssetLoader<UgcElementAsset>, UgcElementLoader>(Lifetime.Singleton);
            builder.Register<IUgcTemplateDataService, UgcTemplateDataService>(Lifetime.Singleton);

            //Projected textures
            builder.Register<IProjectedTextureService, ProjectedTextureRemoteLoaderService>(Lifetime.Singleton);
        }
    }
}
