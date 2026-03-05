using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk.Avatar.Samples.CustomAvatarEditor
{
    /// <summary>
    /// Static class to retrieve data used for UI display and asset equipping
    /// </summary>
    public static class AssetInfoDataSource
    {
        private static readonly Dictionary<WearablesCategory, List<WearableAssetInfo>> _wearablesInfo = new();
        private static readonly Dictionary<AvatarFeatureCategory, List<AvatarFeaturesInfo>> _avatarFeaturesInfo = new();
        private static List<WearableAssetInfo> _hairInfo = new();
        private static readonly Dictionary<ColorType, List<IAvatarColor>> _colorsInfo = new();

        /// <summary>
        /// Gets data about wearables to be used for UI.
        /// </summary>
        /// <param name="category">The category of wearables you want to get</param>
        /// <returns>A list of info for wearables such as name, category, and icon</returns>
        public static async UniTask<List<WearableAssetInfo>> GetWearablesDataForCategory(WearablesCategory category)
        {
            if (_wearablesInfo.ContainsKey(category))
            {
                return _wearablesInfo[category];
            }

            var wearables = await AvatarSdk.GetDefaultWearablesByCategoryAsync(category);
            _wearablesInfo[category] = wearables;
            return wearables;
        }

        /// <summary>
        /// Gets data about hair to be used for UI.
        /// </summary>
        /// <returns>A list of info about hair such as name and icon</returns>
        public static async UniTask<List<WearableAssetInfo>> GetAvatarHair()
        {
            if (_hairInfo.Count > 0)
            {
                return _hairInfo;
            }

            var hair = await AvatarSdk.GetDefaultHairAssets(HairType.Hair);
            _hairInfo = hair.Take(15).ToList(); // There are many hair options, so for simplicity
                                                // We only show the first 15
            return _hairInfo;
        }

        /// <summary>
        /// Gets data about avatar features to be used for UI.
        /// </summary>
        /// <param name="category">The type of feature you want to get, if null, returns all</param>
        /// <returns>A list of info about avatar features such as name, category, and icon</returns>
        public static async UniTask<List<AvatarFeaturesInfo>> GetAvatarFeatureDataForCategory(AvatarFeatureCategory category)
        {
            if (_avatarFeaturesInfo.ContainsKey(category))
            {
                return _avatarFeaturesInfo[category];
            }

            var avatarFeatures = await AvatarSdk.GetDefaultAvatarFeaturesByCategory(category);
            _avatarFeaturesInfo[category] = avatarFeatures;
            return avatarFeatures;
        }

        /// <summary>
        /// Gets color options for different avatar features.
        /// </summary>
        /// <param name="category">The type of feature to get colors for</param>
        /// <returns>A list of objects containing color information for the requested feature</returns>
        public static async UniTask<List<IAvatarColor>> GetColorDataForCategory(ColorType category)
        {
            if (_colorsInfo.ContainsKey(category))
            {
                return _colorsInfo[category];
            }

            var colors = await AvatarSdk.GetDefaultColorsAsync(category);
            _colorsInfo[category] = colors;
            return colors;
        }

        /// <summary>
        /// Gets current statistics for the given face or body feature (like eye size or vertical position for the feature 'eyes')
        /// </summary>
        /// <param name="statType">The type of feature to get stats for</param>
        /// <returns>A dictionary mapping a given statistic to its current value</returns>
        public static IReadOnlyDictionary<AvatarFeatureStat, float> GetCurrentStatsForCategory(AvatarFeatureStatType statType)
        {
            return AvatarSdk.GetAvatarFeatureStats(EditorInitializer.GetSpawnedAvatar(), statType);
        }
    }
}
