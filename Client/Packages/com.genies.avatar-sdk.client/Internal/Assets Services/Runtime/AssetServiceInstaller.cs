using Genies.Components.ShaderlessTools;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Assets.Services
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AssetServiceInstaller : IGeniesInstaller
#else
    public class AssetServiceInstaller : IGeniesInstaller
#endif
    {
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAssetsService, AddressableAssetsService>(Lifetime.Singleton);
            builder.Register<IShaderlessAssetService, ShaderlessAssetService>(Lifetime.Singleton);
        }
    }
}
