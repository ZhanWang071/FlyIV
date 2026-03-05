using System;
using Genies.Services.Configs;

namespace Genies.Avatars.Services
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarServiceApiPathResolver : IApiClientPathResolver
#else
    public class AvatarServiceApiPathResolver : IApiClientPathResolver
#endif
    {
        public string GetApiBaseUrl(BackendEnvironment environment)
        {
            switch (environment)
            {
                case BackendEnvironment.QA:
                    return "https://api.qa.genies.com";
                case BackendEnvironment.Prod:
                    return "https://api.genies.com";
                case BackendEnvironment.Dev:
                    return "https://api.dev.genies.com";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }
    }
}
