using Genies.ServiceManagement;
using VContainer;

namespace Genies.Wearables
{
    /// <summary>
    /// Dependency injection installer for the wearable service.
    /// Registers the IWearableService interface with its WearableService implementation as a singleton.
    /// </summary>
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class WearableServiceInstaller : IGeniesInstaller
#else
    public class WearableServiceInstaller : IGeniesInstaller
#endif
    {
        /// <summary>
        /// Installs the wearable service dependencies into the DI container.
        /// </summary>
        /// <param name="builder">The container builder to register dependencies with.</param>
        public void Install(IContainerBuilder builder)
        {
            builder.Register<IWearableService, WearableService>(Lifetime.Singleton);
        }
    }
}
