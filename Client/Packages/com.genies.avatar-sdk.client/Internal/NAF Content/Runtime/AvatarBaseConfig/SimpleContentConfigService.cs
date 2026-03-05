using Cysharp.Threading.Tasks;

namespace Genies.Naf.Content.AvatarBaseConfig
{
    /// <summary>
    /// Simplified implementation of IContentConfigService to return constant config data.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SimpleContentConfigService : IContentConfigService
#else
    public class SimpleContentConfigService : IContentConfigService
#endif
    {
        private const string AvatarBaseVersion = "1.1.0";
        
        /// <summary>
        /// Fetches config from constant data.
        /// Returns a RootConfig object with a predefined AvatarBase version.
        /// </summary>
        public UniTask<RootConfig> FetchConfig(string configId)
        {
            var config = new RootConfig
            {
                avatarBase = new AvatarBase
                {
                    version = AvatarBaseVersion
                }
            };
            
            return UniTask.FromResult(config);
        }
    }
}