using System;

namespace Genies.Models {
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum WardrobeSubcategory {
#else
    public enum WardrobeSubcategory {
#endif
        none,
        hair,
        eyebrows,
        eyelashes,
        facialHair,
        underwearTop,
        hoodie,
        shirt,
        jacket,
        dress,
        pants,
        shorts,
        skirt,
        underwearBottom,
        socks,
        shoes,
        bag,
        bracelet,
        earrings,
        glasses,
        hat,
        mask,
        watch,
        all
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal static class WardrobeSubcategoryExtensions {
#else
    public static class WardrobeSubcategoryExtensions {
#endif
        public static WardrobeSubcategory FromString(string wardrobeSubcategoryName) {
            if (Enum.TryParse(wardrobeSubcategoryName, true, out WardrobeSubcategory wardrobeSubcategory))
            {
                return wardrobeSubcategory;
            }

            return WardrobeSubcategory.none;
        }

        public static WardrobeSubcategory FromAssetName(string assetName) {
            foreach (var subcategory in (WardrobeSubcategory[]) Enum.GetValues(typeof(WardrobeSubcategory))) {
                if (assetName.ToLower().StartsWith(subcategory.ToString().ToLower()))
                {
                    return subcategory;
                }
            }

            return WardrobeSubcategory.none;
        }
    }
}
