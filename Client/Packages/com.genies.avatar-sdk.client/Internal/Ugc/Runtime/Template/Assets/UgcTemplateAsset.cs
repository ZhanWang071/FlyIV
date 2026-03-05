using Genies.Assets.Services;
using Genies.Avatars;
using UMA;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains UGC template data and assets.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class UgcTemplateAsset : IAsset
#else
    public sealed class UgcTemplateAsset : IAsset
#endif
    {
        public string Id => Data.TemplateId;
        public string Lod => AssetLod.Default;

        public UgcTemplateData Data { get; }
        public MeshHideAsset[] MeshHideAssets { get; }
        public IGenieComponentCreator[] ComponentCreators { get; }

        public UgcTemplateAsset(
            UgcTemplateData data,
            MeshHideAsset[] meshHideAssets,
            IGenieComponentCreator[] componentCreators)
        {
            Data = data;
            MeshHideAssets = meshHideAssets;
            ComponentCreators = componentCreators;
        }

    }
}
