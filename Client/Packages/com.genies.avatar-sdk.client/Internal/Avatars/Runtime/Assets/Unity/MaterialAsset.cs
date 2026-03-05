using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class MaterialAsset : IAsset
#else
    public sealed class MaterialAsset : IAsset
#endif
    {
        public string Id { get; }
        public string Lod { get; }
        public Material Material { get; }
        
        public MaterialAsset(string id, string lod, Material material)
        {
            Id = id;
            Lod = lod;
            Material = material;
        }
    }
}