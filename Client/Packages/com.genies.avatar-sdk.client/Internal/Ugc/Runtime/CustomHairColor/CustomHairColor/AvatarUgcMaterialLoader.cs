using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Models;
using Genies.Refs;

namespace Genies.Ugc.CustomHair
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarUgcMaterialLoader : ISlottedAssetLoader<MaterialAsset>
#else
    public class AvatarUgcMaterialLoader : ISlottedAssetLoader<MaterialAsset>
#endif
    {

        private readonly HairColorService _hairColorService;
        private readonly IAssetsService _assetsService;

        public AvatarUgcMaterialLoader(HairColorService hairColorService, IAssetsService assetsService)
        {
            _assetsService = assetsService;
            _hairColorService = hairColorService;
        }

        public async UniTask<Ref<MaterialAsset>> LoadAsync(string assetId, string slotId, string lod = AssetLod.Default)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return default;
            }

            MaterialAsset materialAsset = null;
            //Custom hair loading.
            if (slotId.Equals(UnifiedMaterialSlot.Hair))
            {
                var materialRef = await _hairColorService.GetHairMaterialForIdAsync(assetId);
                if (!materialRef.IsAlive)
                {
                    return default;
                }

                materialAsset = new MaterialAsset(assetId, AssetLod.Default, materialRef.Item);
                return CreateRef.FromDependentResource(materialAsset, materialRef);
            }

            //Other slots as normal
            Ref<MaterialDataContainer> containerRef = await _assetsService.LoadAssetAsync<MaterialDataContainer>(assetId);
            if (!containerRef.IsAlive)
            {
                return default;
            }

            materialAsset = new MaterialAsset(assetId, AssetLod.Default, containerRef.Item.targetMaterial);
            return CreateRef.FromDependentResource(materialAsset, containerRef);
        }
    }
}
