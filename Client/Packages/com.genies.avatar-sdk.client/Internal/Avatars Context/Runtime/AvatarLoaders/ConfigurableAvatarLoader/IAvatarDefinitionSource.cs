using Cysharp.Threading.Tasks;

namespace Genies.Avatars.Context
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAvatarDefinitionSource
#else
    public interface IAvatarDefinitionSource
#endif
    {
        UniTask<string> GetDefinitionAsync();
    }
}