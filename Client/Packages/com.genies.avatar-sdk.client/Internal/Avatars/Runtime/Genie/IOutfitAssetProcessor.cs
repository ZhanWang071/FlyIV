using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IOutfitAssetProcessor
#else
    public interface IOutfitAssetProcessor
#endif
    {
        UniTask ProcessAddedAssetAsync(OutfitAsset asset);
        UniTask ProcessRemovedAssetAsync(OutfitAsset asset);
    }
}