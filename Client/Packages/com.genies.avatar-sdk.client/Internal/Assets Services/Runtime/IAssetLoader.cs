using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Assets.Services
{
    /// <summary>
    /// Asynchronously loads T instances. It returns a <see cref="Ref{T}"/>
    /// to the asset that must be disposed when the asset is no longer used.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAssetLoader<T>
#else
    public interface IAssetLoader<T>
#endif
    {
        /// <summary>
        /// Loads and returns a reference to a T identified by the given <see cref="assetId"/>.
        /// </summary>
        UniTask<Ref<T>> LoadAsync(string assetId, string lod = AssetLod.Default);
    }
}
