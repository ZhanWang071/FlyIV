using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models.Makeup;

namespace Genies.Avatars.Context
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class MakeupLoader : BaseAssetLoader<Texture2DAsset, MakeupTemplate>
#else
    public sealed class MakeupLoader : BaseAssetLoader<Texture2DAsset, MakeupTemplate>
#endif
    {
        public MakeupLoader(IAssetsService assetsService) : base(assetsService){}
        protected override UniTask<Texture2DAsset> FromContainer(string assetId, string lod, MakeupTemplate container)
        {
            var asset = new Texture2DAsset(assetId, lod, container.Map);
            return UniTask.FromResult(asset);
        }
    }
}
