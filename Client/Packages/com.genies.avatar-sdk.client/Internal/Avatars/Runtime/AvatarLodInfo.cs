namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct AvatarLodInfo
#else
    public struct AvatarLodInfo
#endif
    {
        public int    Index;
        public string Name;
        public int    TriangleCount;
        public string Url;
    }
}