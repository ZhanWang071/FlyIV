using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// Capable of building an <see cref="OutfitAsset"/> instance from a wearable definition and metadata. It also contains some utility methods
    /// for lower level manipulation like building elements separately or creating UMA recipes from custom built elements.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IUgcOutfitAssetBuilder
#else
    public interface IUgcOutfitAssetBuilder
#endif
    {
        UniTask<OutfitAsset> BuildOutfitAssetAsync(string wearableId, Wearable wearable, OutfitAssetMetadata metadata, string lod = AssetLod.Default);
    }
}
