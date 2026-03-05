using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ISlottedAssetLoader <TAsset>
#else
    public interface ISlottedAssetLoader <TAsset>
#endif
        where TAsset : IAsset
    {
        /// <summary>
        /// Loads and returns a reference to a <see cref="TAsset"/> instance identified by the given <see cref="assetId"/>.
        /// </summary>
        UniTask<Ref<TAsset>> LoadAsync(string assetId, string slotId, string lod = AssetLod.Default);
    }
}
