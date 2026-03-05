using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Addressables.UniversalResourceLocation
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICustomResourceProvider
#else
    public interface ICustomResourceProvider
#endif
    {
        public UniTask<Ref<Sprite>> Provide(string internalId);
    }
}
