using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Components.ShaderlessTools
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IShaderlessAssetService
#else
    public interface IShaderlessAssetService
#endif
    {
        public UniTask<Ref<T>> LoadShadersAsync<T>(Ref<T> assetRef);
    }
}
