using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Services;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.ServiceManagement;
using Genies.Services.Model;
using GnWrappers;
using UnityEngine;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Base class for NafContentService implementations that provides shared functionality
    /// for asset ID conversion and parameter fetching
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class NafContentServiceBase : IAssetParamsService, IAssetIdConverter
#else
    public abstract class NafContentServiceBase : IAssetParamsService, IAssetIdConverter
#endif
    {
        protected bool _initialized = false;
        protected readonly Dictionary<string, NafContentMetadata> _assetsByAddress = new();
        protected readonly List<string> _staticMapping = new() {"AvatarDna", "AvatarTattoo"};
        protected readonly List<string> _overrideVersion = new() {"AvatarBase",};
        protected string _AvatarBaseVersionFromConfig = null;

        // For thread-safe cache access during on-demand resolution
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        // Tracks avatar definitions that need to be saved after asset ID conversion
        private readonly Dictionary<Naf.AvatarDefinition, HashSet<string>> _pendingAvatarUpdates = new();
        private readonly SemaphoreSlim _avatarUpdateLock = new SemaphoreSlim(1, 1);

        /// <summary>
        /// Initialize the service with data from the specific source (inventory, CMS, etc.)
        /// </summary>
        public abstract UniTask Initialize();

        public async UniTask<string> ConvertToUniversalIdAsync(string assetId)
        {
            await InitializeIfNeededAsync();

            string universalId = GetUniversalId(assetId);

            // Temp Hotfix Remove spaces, using %20 does not work, until next Naf update
            universalId = universalId.Replace(' ', '+');
            return await UniTask.FromResult(universalId);
        }

        /// <summary>
        /// Batch converts multiple asset IDs to universal IDs.
        /// </summary>
        /// <param name="assetIds">List of asset IDs to convert</param>
        /// <returns>Dictionary mapping original asset IDs to their universal IDs</returns>
        public async UniTask<Dictionary<string, string>> ConvertToUniversalIdsAsync(List<string> assetIds)
        {
            return await ConvertToUniversalIdsAsync(assetIds, null);
        }

        /// <summary>
        /// Batch converts multiple asset IDs to universal IDs, optionally tracking an avatar definition for later update.
        /// </summary>
        /// <param name="assetIds">List of asset IDs to convert</param>
        /// <param name="avatarDefinition">Optional avatar definition to track for updates if old IDs are found</param>
        /// <returns>Dictionary mapping original asset IDs to their universal IDs</returns>
        public async UniTask<Dictionary<string, string>> ConvertToUniversalIdsAsync(List<string> assetIds, Naf.AvatarDefinition avatarDefinition)
        {
            await InitializeIfNeededAsync();

            if (assetIds == null || assetIds.Count == 0)
            {
                return new Dictionary<string, string>();
            }

            // Convert all asset IDs to universal IDs
            var result = new Dictionary<string, string>();
            var oldIdsFound = new HashSet<string>();

            foreach (var assetId in assetIds)
            {
                // Check if this is an old ID that needs conversion
                if (StaticAssetMapping.Dictionary.ContainsKey(assetId))
                {
                    oldIdsFound.Add(assetId);
                }

                var universalId = await ConvertToUniversalIdAsync(assetId);
                result[assetId] = universalId;
            }

            // If we found old IDs and have an avatar definition to track, register it for later update
            if (oldIdsFound.Count > 0 && avatarDefinition != null)
            {
                await _avatarUpdateLock.WaitAsync();
                try
                {
                    if (!_pendingAvatarUpdates.ContainsKey(avatarDefinition))
                    {
                        _pendingAvatarUpdates[avatarDefinition] = new HashSet<string>();
                    }

                    foreach (var oldId in oldIdsFound)
                    {
                        _pendingAvatarUpdates[avatarDefinition].Add(oldId);
                    }
                }
                finally
                {
                    _avatarUpdateLock.Release();
                }
            }

            return result;
        }

        public async UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId)
        {
            await InitializeIfNeededAsync();

            Dictionary<string, string> result = GetParams(assetId, LodLevels.DefaultLod);
            return await UniTask.FromResult(result);
        }

        /// <summary>
        /// Returns the input if it cant determine or translate the assetId
        /// </summary>
        protected string GetUniversalId(string assetId)
        {
            // Convert any old IDs to new (before stripping anything)
            if (StaticAssetMapping.Dictionary.ContainsKey(assetId))
            {
                assetId = StaticAssetMapping.Dictionary[assetId];
            }

            // Strip prefix for future lookups
            var lookupKey = ToLookupKey(assetId);

            if (!_assetsByAddress.TryGetValue(lookupKey, out var result))
            {
                // If not found, also try without stripping
                if (!_assetsByAddress.TryGetValue(assetId, out result))
                {
                    // If neither found, return non-stripped ID
                    return assetId;
                }
            }

            var pipelineId = !string.IsNullOrEmpty(result.PipelineId)
                ? (_staticMapping.Contains(result.PipelineId) ? "Static/" : $"{result.PipelineId}/")
                : string.Empty;

            return string.IsNullOrEmpty(pipelineId)? assetId :
                string.Equals(pipelineId, "Static/")? $"{pipelineId}{result.AssetAddress}" : $"{pipelineId}{result.Guid}";
        }

        protected Dictionary<string, string> GetParams(string assetId, string lod = null)
        {
            if (!_assetsByAddress.TryGetValue(ToLookupKey(assetId), out NafContentMetadata result))
            {
                return default;
            }

            var assetParams = new Dictionary<string, string>();

            if (result.UniversalBuildVersion != null)
            {
                var version = string.IsNullOrEmpty(result.UniversalBuildVersion) ? "0" : result.UniversalBuildVersion;

                // use config override for AvatarBase if set, otherwise use the version from the metadata
                if (_overrideVersion.Contains(result.PipelineId))
                {
                    version = !string.IsNullOrEmpty(_AvatarBaseVersionFromConfig) ? _AvatarBaseVersionFromConfig : result.UniversalBuildVersion;
                }

                assetParams.Add("v", version);
            }

            if (!string.IsNullOrEmpty(lod))
            {
                assetParams.Add("lod", lod);
            }

            return assetParams;
        }

        protected async UniTask InitializeIfNeededAsync()
        {
            if (!_initialized)
            {
                await Initialize();
            }
        }

        /// <summary>
        /// Strips assetType/ from assetAddress or just returns the assetId if no type is present.
        /// eg: recSjNgdNxWYeuLeD || WardrobeGear/recSjNgdNxWYeuLeD => recSjNgdNxWYeuLeD
        /// eg: Genie_Unified_gen13gp_Race_Container || Static/Genie_Unified_gen13gp_Race_Container => Genie_Unified_gen13gp_Race_Container
        /// Finds key for both types of assetIds
        /// </summary>
        protected static string ToLookupKey(string assetId)
        {
            // just substrings last part after '/'
            var pathIdx = assetId.LastIndexOf('/');
            return pathIdx == -1 ? assetId : assetId.Substring(pathIdx + 1);
        }

        /// <summary>
        /// Merges source dictionary into target dictionary, overwriting duplicates.
        /// </summary>
        protected static void Merge<TKey, TValue>(IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            foreach (KeyValuePair<TKey, TValue> kvp in source)
            {
                target[kvp.Key] = kvp.Value; // overwrites duplicates
            }
        }

        /// <summary>
        /// Processes all pending avatar definition updates, converting old asset IDs to new ones and saving them.
        /// This should be called after all asset ID conversions are complete.
        /// </summary>
        public async UniTask ProcessPendingAvatarUpdatesAsync()
        {
            Dictionary<AvatarDefinition, HashSet<string>> avatarsToUpdate;

            await _avatarUpdateLock.WaitAsync();
            try
            {
                if (_pendingAvatarUpdates.Count == 0)
                {
                    return;
                }

                // Copy the pending updates and clear the dictionary
                avatarsToUpdate = new Dictionary<Naf.AvatarDefinition, HashSet<string>>(_pendingAvatarUpdates);
                _pendingAvatarUpdates.Clear();
            }
            finally
            {
                _avatarUpdateLock.Release();
            }

            // Process each avatar definition that needs updating
            foreach (var kvp in avatarsToUpdate)
            {
                var avatarDefinition = kvp.Key;
                var oldIds = kvp.Value;

                try
                {
                    bool wasModified = false;

                    // Update equipped asset IDs
                    if (avatarDefinition.equippedAssetIds != null)
                    {
                        for (int i = 0; i < avatarDefinition.equippedAssetIds.Count; i++)
                        {
                            var assetId = avatarDefinition.equippedAssetIds[i];
                            if (StaticAssetMapping.Dictionary.TryGetValue(assetId, out var newId))
                            {
                                avatarDefinition.equippedAssetIds[i] = newId;
                                wasModified = true;
                            }
                        }
                    }

                    // Update equipped tattoo IDs
                    if (avatarDefinition.equippedTattooIds != null)
                    {
                        var tattoosToUpdate = new List<(MegaSkinTattooSlot slot, string oldId, string newId)>();
                        foreach (var tattooKvp in avatarDefinition.equippedTattooIds)
                        {
                            if (StaticAssetMapping.Dictionary.TryGetValue(tattooKvp.Value, out var newId))
                            {
                                tattoosToUpdate.Add((tattooKvp.Key, tattooKvp.Value, newId));
                            }
                        }

                        foreach (var update in tattoosToUpdate)
                        {
                            avatarDefinition.equippedTattooIds[update.slot] = update.newId;
                            wasModified = true;
                        }
                    }

                    // Save the avatar definition if it was modified
                    if (wasModified)
                    {
                        await SaveAvatarDefinitionAsync(avatarDefinition);
                    }
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"Failed to update avatar definition: {ex.Message}");
                    CrashReporter.LogHandledException(ex);
                }
            }
        }

        /// <summary>
        /// Saves an avatar definition using the IAvatarService.
        /// </summary>
        private async UniTask SaveAvatarDefinitionAsync(AvatarDefinition avatarDefinition)
        {
            var avatarService = ServiceManager.Get<IAvatarService>();
            if (avatarService == null)
            {
                CrashReporter.LogWarning("AvatarService not found. Cannot save avatar definition.");
                return;
            }

            await avatarService.UpdateAvatarAsync(avatarDefinition);
        }

        /// <summary>
        /// Resolves multiple asset IDs by fetching their pipeline data
        /// </summary>
        /// <param name="assetIds">List of asset IDs to resolve</param>
        public async UniTask ResolveAssetsAsync(List<string> assetIds)
        {
            if (assetIds == null || assetIds.Count == 0)
            {
                return;
            }

            // Convert IDs in case they haven't been already
            var convertedIdsDict = await ConvertToUniversalIdsAsync(assetIds);
            // Strip the prefix off the converted IDs
            var strippedIds = convertedIdsDict.Values.Select(ToLookupKey);

            var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
            if (defaultInventoryService == null)
            {
                CrashReporter.LogError("[NafContentServiceBase] DefaultInventoryService not found. Cannot resolve assets on-demand.");
                return;
            }

            try
            {
                var pipelineInfoDict = await defaultInventoryService.ResolvePipelineItemsAsync(strippedIds.ToList());

                await _cacheLock.WaitAsync();
                try
                {
                    foreach (var kvp in pipelineInfoDict)
                    {
                        var assetId = kvp.Key;
                        var assetPipelineInfo = kvp.Value;

                        // Resolve the correct pipeline item from the list
                        var resolvedPipelineItem = await ResolvePipelineItemFromList(
                            assetPipelineInfo.Pipeline,
                            assetPipelineInfo.AssetType
                        );

                        if (resolvedPipelineItem != null)
                        {
                            var metadata = new NafContentMetadata
                            {
                                AssetAddress = string.IsNullOrEmpty(resolvedPipelineItem.AssetAddress)
                                    ? assetId
                                    : resolvedPipelineItem.AssetAddress,
                                Guid = assetId,
                                Owner = "internal", // Assets from default inventory are internal
                                UniversalBuildVersion = resolvedPipelineItem.UniversalBuildVersion,
                                UniversalAvailable = resolvedPipelineItem.UniversalAvailable ?? false,
                                PipelineId = assetPipelineInfo.AssetType
                            };

                            _assetsByAddress[assetId] = metadata;

                            // Also map parent ID if different
                            if (!string.IsNullOrEmpty(resolvedPipelineItem.ParentId) &&
                                resolvedPipelineItem.ParentId != assetId)
                            {
                                _assetsByAddress[resolvedPipelineItem.ParentId] = metadata;
                            }
                        }
                    }
                }
                finally
                {
                    _cacheLock.Release();
                }
            }
            catch (System.Exception ex)
            {
                CrashReporter.LogError($"[NafContentServiceBase] Failed to resolve assets on-demand: {ex.Message}");
            }
        }

        /// <summary>
        /// Resolves the correct pipeline item from a list by selecting the most recent version
        /// that is universally available.
        /// </summary>
        private UniTask<PipelineItemV2> ResolvePipelineItemFromList(List<PipelineItemV2> pipelineItems, string assetType)
        {
            if (pipelineItems == null || pipelineItems.Count == 0)
            {
                if (assetType != AssetType.ColorPreset.ToString())
                {
                    CrashReporter.LogWarning($"[NafContentServiceBase] Asset type {assetType} has no pipeline defined.");
                }

                return UniTask.FromResult<PipelineItemV2>(null);
            }

            try
            {
                // Select the most recent pipeline item (highest version)
                // Prefer items that are universally available
                var selectedItem = pipelineItems
                    .OrderByDescending(p =>
                    {
                        // Parse the pipeline version to get a sortable value
                        if (int.TryParse(p.PipelineVersion?.Split('.')[0], out int majorVersion))
                        {
                            return majorVersion;
                        }
                        return 0;
                    })
                    .ThenByDescending(p => p.UniversalAvailable ?? false)
                    .ThenByDescending(p => p.AssetVersion ?? 0)
                    .FirstOrDefault();

                return UniTask.FromResult(selectedItem);
            }
            catch (System.Exception ex)
            {
                CrashReporter.LogError($"[NafContentServiceBase] Error resolving pipeline version for asset type {assetType}: {ex}");
                return UniTask.FromResult(pipelineItems.LastOrDefault());
            }
        }

        // Lods on Mac need to be High by default due normals not matching for mac m gpus
        protected static class LodLevels
        {
            public const string High = "0";
            public const string Mid = "1";
            public const string Low = "2";
            public static string DefaultLod => Low;
        }
    }
}
