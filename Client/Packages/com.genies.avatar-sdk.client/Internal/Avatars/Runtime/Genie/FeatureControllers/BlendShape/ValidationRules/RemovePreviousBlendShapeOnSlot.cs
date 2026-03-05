using System.Collections.Generic;

namespace Genies.Avatars.ValidationRules
{
    /// <summary>
    /// Equipping rule that will remove any previously equipped blend shape on the slot that the equipping blend shape
    /// is equipping to.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class RemovePreviousBlendShapeOnSlot : IAssetsAdjustmentRule<BlendShapeAsset>
#else
    public class RemovePreviousBlendShapeOnSlot : IAssetsAdjustmentRule<BlendShapeAsset>
#endif
    {
        public void Apply(HashSet<BlendShapeAsset> assets, BlendShapeAsset adjustedAsset)
        {
            assets.RemoveWhere(asset => asset.Slot == adjustedAsset.Slot);
        }
    }
}