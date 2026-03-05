using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Removes from the outfit any assets of the specified subcategory.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class RemoveAnyAssetsFromSubcategory : IAssetsValidationRule<OutfitAsset>
#else
    public sealed class RemoveAnyAssetsFromSubcategory : IAssetsValidationRule<OutfitAsset>
#endif
    {
        public readonly string Subcategory;

        public RemoveAnyAssetsFromSubcategory(string subcategory)
        {
            Subcategory = subcategory;
        }

        public void Apply(HashSet<OutfitAsset> outfit)
        {
            outfit.RemoveWhere(asset => asset.Metadata.Subcategory == Subcategory);
        }
    }
}
