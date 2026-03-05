using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Inventory;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Naf.Content
{
    /// <summary>
    /// Handles inventory events and updates NAF content metadata and locations accordingly
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NafInventoryEventHandler
#else
    public class NafInventoryEventHandler
#endif
    {
        private bool _isSubscribed = false;

        public void Initialize()
        {
            if (!_isSubscribed)
            {
                ServiceManager.Get<IDefaultInventoryService>().AssetsAddedAsync += OnAssetsAdded;
                _isSubscribed = true;
            }
        }

        public void Dispose()
        {
            if (_isSubscribed)
            {
                var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
                if (defaultInventoryService != null)
                {
                    defaultInventoryService.AssetsAddedAsync -= OnAssetsAdded;
                    _isSubscribed = false;
                }
            }
        }

        private async UniTask OnAssetsAdded(List<DefaultInventoryAsset> assets)
        {
            foreach (var asset in assets)
            {
                try
                {
                    // Update NAF content metadata
                    UpdateAssetMetadata(asset);

                    // Update resource locations
                    await UpdateAssetLocations(asset);
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"[NafInventoryEventHandler] Failed to handle asset minted event for {asset?.AssetId}: {ex}");
                }
            }
        }

        /// <summary>
        /// Updates NAF content metadata for a specific asset
        /// </summary>
        /// <param name="asset">The asset to update</param>
        private void UpdateAssetMetadata(DefaultInventoryAsset asset)
        {
            if (asset.AssetType == AssetType.ColorPreset)
            {
                return; // No metadata for color presets
            }

            try
            {
                var nafContentService = ServiceManager.Get<NafContentService>();
                if (nafContentService != null)
                {
                    nafContentService.UpdateAssetMetadata(asset);
                }
                else
                {
                    CrashReporter.LogWarning($"[NafInventoryEventHandler] NafContentService not found, skipping metadata update for asset {asset?.AssetId}");
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafInventoryEventHandler] Failed to update asset metadata for {asset?.AssetId}: {ex}");
            }
        }

        /// <summary>
        /// Updates resource locations for a specific asset
        /// </summary>
        /// <param name="asset">The asset to update</param>
        private async UniTask UpdateAssetLocations(DefaultInventoryAsset asset)
        {
            try
            {
                var locationService = ServiceManager.Get<IInventoryNafLocationsProvider>();
                await locationService.UpdateAssetLocations(asset);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"[NafInventoryEventHandler] Failed to update asset locations for {asset?.AssetId}: {ex}");
            }
        }
    }
}
