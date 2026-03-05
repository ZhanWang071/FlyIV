using System;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ChildAsset
#else
    public class ChildAsset
#endif
    {
        public string guid;
        public string assetType;
        public ProtocolTag protocolTag;
    }
}