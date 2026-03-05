using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains a Gear element asset.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GearElementAsset : IAsset
#else
    public sealed class GearElementAsset : IAsset
#endif
    {
        public string Id  { get; }
        public string Lod { get; }

        public readonly GearSubElement[]         SubElements;
        public readonly IGenieComponentCreator[] ComponentCreators;

        public GearElementAsset(
            string id,
            string lod,
            GearSubElement[] subElements,
            IGenieComponentCreator[] componentCreators)
        {
            Id = id;
            Lod = lod;
            SubElements = subElements;
            ComponentCreators = componentCreators;
        }
    }
}
