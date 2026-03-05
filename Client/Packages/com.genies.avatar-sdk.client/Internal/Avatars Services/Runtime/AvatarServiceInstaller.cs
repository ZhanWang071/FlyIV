using Genies.Login;
using Genies.ServiceManagement;
using VContainer;

namespace Genies.Avatars.Services
{
    [AutoResolve]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarServiceInstaller : IGeniesInstaller, IRequiresInstaller<IGeniesLoginInstaller>
#else
    public class AvatarServiceInstaller : IGeniesInstaller, IRequiresInstaller<IGeniesLoginInstaller>
#endif
    {
        public void Install(IContainerBuilder builder)
        {
            var avatarService = new AvatarService();
            avatarService.RegisterSelf().As<IAvatarService>();
        }
    }
}
