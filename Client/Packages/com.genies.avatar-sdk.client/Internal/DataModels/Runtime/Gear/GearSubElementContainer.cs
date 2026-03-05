using UMA;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GearSubElementContainer : OrderedScriptableObject
#else
    public sealed class GearSubElementContainer : OrderedScriptableObject
#endif
    {
        public bool IsEditable => editableRegionCount > 0 && editableRegionsMap;
        public string UmaMaterialAddress => slotDataAsset.material.name;

        public SlotDataAsset    slotDataAsset;
        public OverlayDataAsset overlayDataAsset;
        public Texture2D        editableRegionsMap;
        public int              editableRegionCount;
    }
}
