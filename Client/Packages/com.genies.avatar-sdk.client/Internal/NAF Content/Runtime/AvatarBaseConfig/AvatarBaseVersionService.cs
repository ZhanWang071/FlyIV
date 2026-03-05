using Cysharp.Threading.Tasks;
using Genies.ServiceManagement;

namespace Genies.Naf.Content.AvatarBaseConfig
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarBaseVersionService
#else
    public static class AvatarBaseVersionService
#endif
    {
        private const string DefaultAvatarBaseVersion = "1.1.0";
        private const string Config = "AvatarBase/config/avatar_base_version.json";
        private static string ConfigLocation => $"{NafContentLocations.NafContentUrl}/{Config}";
        private static IContentConfigService _contentConfigService;

        public static async UniTask<string> GetAvatarBaseVersion()
        {
            var service = GetContentConfigService();
            var config = await service.FetchConfig(ConfigLocation);

            return config?.avatarBase?.version ?? DefaultAvatarBaseVersion;
        }

        private static IContentConfigService GetContentConfigService()
        {
            if (_contentConfigService == null)
            {

                _contentConfigService = ServiceManager.Get<IContentConfigService>();
                if (_contentConfigService == null)
                {
                    _contentConfigService = new SimpleContentConfigService();
                    ServiceManager.RegisterService(_contentConfigService).As<IContentConfigService>();
                }

            }
            return _contentConfigService;
        }
    }
}