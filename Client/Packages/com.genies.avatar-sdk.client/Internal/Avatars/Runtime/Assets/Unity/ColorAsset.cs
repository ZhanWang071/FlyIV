using Genies.Assets.Services;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class ColorAsset : IAsset
#else
    public sealed class ColorAsset : IAsset
#endif
    {
        public string Id { get; }
        public string Lod => AssetLod.Default;
        public Color Color { get; }

        public ColorAsset(string id, Color color)
        {
            Id = id;
            Color = color;
        }
    }
}
