namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class BlendShapeAsset : IAsset
#else
    public sealed class BlendShapeAsset : IAsset
#endif
    {
        public string Id { get; }
        public string Lod { get; }
        public string Slot { get; }
        public DnaEntry[] Dna { get; }

        public BlendShapeAsset(string id, string lod, string slot, DnaEntry[] dna)
        {
            Id = id;
            Lod = lod;
            Slot = slot;
            Dna = dna;
        }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal struct DnaEntry
#else
    public struct DnaEntry
#endif
    {
        public string Name;
        public float Value;
    }
}