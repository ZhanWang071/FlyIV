using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Refs;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IOutfitAssetLoader
#else
    public interface IOutfitAssetLoader
#endif
    {
        IReadOnlyList<string> SupportedTypes { get; }

        UniTask<Ref<OutfitAsset>> LoadAsync(OutfitAssetMetadata metadata, string lod = AssetLod.Default);
        bool IsOutfitAssetTypeSupported(string type);
    }
}
