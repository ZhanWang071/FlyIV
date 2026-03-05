using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Refs;

namespace Genies.Ugc.CustomSkin
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarUgcSkinColorLoader : IAssetLoader<ColorAsset>
#else
    public class AvatarUgcSkinColorLoader : IAssetLoader<ColorAsset>
#endif
    {
        private readonly SkinColorService _skinColorService;

        public AvatarUgcSkinColorLoader(SkinColorService skinColorService)
        {
            _skinColorService = skinColorService;
        }

        public async UniTask<Ref<ColorAsset>> LoadAsync(string assetId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            using Ref<SkinColorData> containerRef = await _skinColorService.GetSkinColorForIdAsync(assetId);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            var asset = new ColorAsset(assetId, containerRef.Item.BaseColor);

            return CreateRef.FromAny(asset);
        }
    }
}
