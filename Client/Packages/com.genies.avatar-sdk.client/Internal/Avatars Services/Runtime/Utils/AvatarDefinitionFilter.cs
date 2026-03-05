using System.Collections.Generic;
using Genies.UGCW.Data;
using Newtonsoft.Json.Linq;

namespace Genies.Avatars.Services
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarDefinitionFilter
#else
    public static class AvatarDefinitionFilter
#endif
    {
        private static readonly List<string> _persistentOutfitAttributes = new() {"hair", "facialHair" };

        public static void FilterNonPersistentAttributes(Avatars.AvatarDefinition avatarDefinition)
        {
            avatarDefinition.Outfits = GetFilteredOutfit(avatarDefinition.Outfits);
            avatarDefinition.AvatarFeatures = new Dictionary<string, JToken>()
            {
                { "DecoratedSkin", JToken.FromObject(new DecoratedSkinDefinition()) },
            };
        }

        private static string[][] GetFilteredOutfit(string[][] avatarDefinitionOutfits)
        {
            var filteredOutfit = new List<string>()
            {
                "underwearBottom-0001-boxers_skin0000", "underwearTop-0002-tankTop_skin0000",
            };


            foreach (var outfit in avatarDefinitionOutfits)
            {
                foreach (var wearable in outfit)
                {
                    if (IsPersistentAttributePresent(wearable))
                    {
                        filteredOutfit.Add(wearable);
                    }
                }
            }

            return new[] {filteredOutfit.ToArray() };
        }

        private static bool IsPersistentAttributePresent(string wearable)
        {
            foreach (var attribute in _persistentOutfitAttributes)
            {
                if (wearable.Contains(attribute))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
