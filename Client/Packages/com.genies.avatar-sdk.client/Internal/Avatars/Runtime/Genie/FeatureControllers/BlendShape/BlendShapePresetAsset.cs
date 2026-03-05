using Genies.Assets.Services;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class BlendShapePresetAsset : IAsset
#else
    public sealed class BlendShapePresetAsset : IAsset
#endif
    {
        public string Id { get; }
        public string Lod => AssetLod.Default;
        public BlendShapeAsset[] BlendShapeAssets { get; }
        
        public BlendShapePresetAsset(string id, BlendShapeAsset[] blendShapeAssets)
        {
            Id = id;
            BlendShapeAssets = blendShapeAssets;
        }
    }
}
