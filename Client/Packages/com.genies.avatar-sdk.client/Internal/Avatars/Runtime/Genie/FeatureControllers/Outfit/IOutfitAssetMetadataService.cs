using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IOutfitAssetMetadataService
#else
    public interface IOutfitAssetMetadataService
#endif
    {
        UniTask<OutfitAssetMetadata> FetchAsync(string assetId);
        UniTask FetchAsync(IEnumerable<string> assetIds, ICollection<OutfitAssetMetadata> assets);
    }
}