using Cysharp.Threading.Tasks;

namespace Genies.Naf.Content.AvatarBaseConfig
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IContentConfigService
#else
    public interface IContentConfigService
#endif
    {
        public UniTask<RootConfig> FetchConfig(string configId);
    }
}