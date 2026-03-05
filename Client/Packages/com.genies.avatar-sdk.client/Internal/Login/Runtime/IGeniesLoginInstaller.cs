using Genies.ServiceManagement;
using VContainer; // Required for IInstaller from IGeniesInstaller

namespace Genies.Login
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IGeniesLoginInstaller : IGeniesInstaller
#else
    public interface IGeniesLoginInstaller : IGeniesInstaller
#endif
    {
        new int OperationOrder => DefaultInstallationGroups.CoreServices;
    }
}
