using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf.Content
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAssetIdConverter
#else
    public interface IAssetIdConverter
#endif
    {
        /// <summary>
        /// Resolves pipeline data for the given asset IDs by fetching from the inventory API.
        /// This populates the internal cache with metadata needed for conversion.
        /// Should be called before ConvertToUniversalIdsAsync for best performance.
        /// </summary>
        UniTask ResolveAssetsAsync(List<string> assetIds);

        /// <summary>
        /// Converts a single asset ID to its universal ID format.
        /// Assumes asset has already been resolved via ResolveAssetsAsync.
        /// </summary>
        UniTask<string> ConvertToUniversalIdAsync(string assetId);

        /// <summary>
        /// Converts multiple asset IDs to their universal ID format.
        /// Assumes assets have already been resolved via ResolveAssetsAsync.
        /// </summary>
        UniTask<Dictionary<string, string>> ConvertToUniversalIdsAsync(List<string> assetIds);

        /// <summary>
        /// Converts multiple asset IDs to their universal ID format, optionally tracking an avatar definition for later update.
        /// If old asset IDs are found and an avatar definition is provided, it will be queued for update.
        /// </summary>
        /// <param name="assetIds">List of asset IDs to convert</param>
        /// <param name="avatarDefinition">Optional avatar definition to track for updates if old IDs are found</param>
        /// <returns>Dictionary mapping original asset IDs to their universal IDs</returns>
        UniTask<Dictionary<string, string>> ConvertToUniversalIdsAsync(List<string> assetIds, Naf.AvatarDefinition avatarDefinition);

        /// <summary>
        /// Processes all pending avatar definition updates, converting old asset IDs to new ones and saving them.
        /// This should be called after all asset ID conversions are complete to batch-save avatar definitions.
        /// </summary>
        UniTask ProcessPendingAvatarUpdatesAsync();
    }
}
