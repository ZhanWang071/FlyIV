using System;
using System.Collections.Generic;
using UMA;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SlotDataAssetGroup
#else
    public struct SlotDataAssetGroup
#endif
    {
        public string              id;
        public List<SlotDataAsset> slots;
    }
}
