using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Genies.CrashReporting;
using Genies.DiskCaching;
using Genies.Services.Configs;
using Genies.Services.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Inventory
{
    /// <summary>
    /// Wrapper class for caching inventory responses with metadata
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CachedInventoryResponse<T>
#else
    public class CachedInventoryResponse<T>
#endif
    {
        [JsonProperty]
        public string CacheKey;

        [JsonProperty]
        public List<T> Items;

        [JsonProperty]
        public DateTime CachedAt;

        [JsonProperty]
        public string NextCursor;

        [JsonProperty]
        public int? Limit;

        [JsonProperty]
        public List<string> Categories;

        public CachedInventoryResponse()
        {
            Items = new List<T>();
        }

        public CachedInventoryResponse(string cacheKey, List<T> items, string nextCursor = null, int? limit = null,
            List<string> categories = null)
        {
            CacheKey = cacheKey;
            Items = items ?? new List<T>();
            NextCursor = nextCursor;
            Limit = limit;
            Categories = categories;
            CachedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Wrapper class for caching a single pipeline resolve result per asset ID
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CachedPipelineItemResponse
#else
    public class CachedPipelineItemResponse
#endif
    {
        [JsonProperty] public string CacheKey;
        [JsonProperty] public string AssetId;
        [JsonProperty] public AssetPipelineInfo Info;
        [JsonProperty] public DateTime CachedAt;

        public CachedPipelineItemResponse() { }

        public CachedPipelineItemResponse(string cacheKey, string assetId, AssetPipelineInfo info)
        {
            CacheKey = cacheKey;
            AssetId = assetId;
            Info = info;
            CachedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Disk cache manager for DefaultInventoryService
    /// Caches API responses to reduce network calls and improve performance
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultInventoryServiceDiskCache
#else
    public class DefaultInventoryServiceDiskCache
#endif
    {
        private readonly DiskCache _diskCache;
        private readonly string _cacheDirectory;
        private readonly string _cacheFilePath;
        private readonly string _orgContext;
        private readonly string _appContext;
        private const string _cacheFileLocation = "DefaultInventoryCache";
        private string _cacheFileName = "default_inventory_cache.json";

        public DefaultInventoryServiceDiskCache(string orgContext, string appContext,
            int cacheExpirationSeconds = 86400, bool isDemoMode = false) // Default 24 hours
        {
            _orgContext = orgContext;
            _appContext = appContext;

            // If this looks like demo mode, ensure we look for a demo mode config.
            if (isDemoMode)
            {
                _cacheFileName = $"demo_{_cacheFileName}";
            }
            
            // Set up cache directory in Unity's persistent data path, including the current backend environment
            _cacheDirectory = Path.Combine(
                Application.persistentDataPath,
                _cacheFileLocation,
                GeniesApiConfigManager.TargetEnvironment.ToString()
            );
            
            _cacheFilePath = Path.Combine(_cacheDirectory, _cacheFileName);

            // Ensure directory exists
            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }

            // Initialize DiskCache with options
            var options = new DiskCacheOptions
            {
                cacheExpirationInSeconds = cacheExpirationSeconds,
                maxCacheSizeInBytes = 0 // No size limit
            };

            _diskCache = DiskCache.LoadFromFile(_cacheFilePath, options);

            // Clean up expired entries on initialization
            _diskCache.FindAndClearExpiredCacheEntries();
            _diskCache.SaveToFile(_cacheFilePath);
        }

        /// <summary>
        /// Generate a cache key from org, app, endpoint type, categories, and limit
        /// </summary>
        private string GenerateCacheKey(string endpointType, List<string> categories, int? limit)
        {
            var categoryKey = categories == null || categories.Count == 0
                ? "all"
                : string.Join("|", categories.OrderBy(c => c));

            // Include org and app context in the cache key to isolate caches by context
            return $"{_orgContext}_{_appContext}_{endpointType}_{categoryKey}_{limit?.ToString() ?? "unlimited"}";
        }

        /// <summary>
        /// Generic method to try to get cached data
        /// </summary>
        private bool TryGetCached<T>(string cacheKey, out List<T> items, out string nextCursor)
        {
            items = null;
            nextCursor = null;

            try
            {
                // IsObjectCached checks if the file exists and removes expired entries
                if (!_diskCache.IsObjectCached(cacheKey))
                {
                    return false;
                }

                if (!_diskCache.TryGetCachedFilePath(cacheKey, out var filePath))
                {
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var cached = JsonConvert.DeserializeObject<CachedInventoryResponse<T>>(json);

                if (cached == null)
                {
                    return false;
                }

                items = cached.Items;
                nextCursor = cached.NextCursor;
                return true;
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Failed to load cached data for key {cacheKey}: {ex.Message}", ex));
                return false;
            }
        }

        /// <summary>
        /// Generic method to cache data
        /// </summary>
        private void CacheData<T>(string cacheKey, List<T> items, string nextCursor, int? limit, List<string> categories)
        {
            try
            {
                var cached = new CachedInventoryResponse<T>(cacheKey, items, nextCursor, limit, categories);
                var json = JsonConvert.SerializeObject(cached, Formatting.None);

                var fileName = $"{cacheKey}.json";
                var filePath = Path.Combine(_cacheDirectory, fileName);

                File.WriteAllText(filePath, json);

                _diskCache.AddEntry(cacheKey, filePath);
                _diskCache.SaveToFile(_cacheFilePath);
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Failed to cache data for key {cacheKey}: {ex.Message}", ex));
            }
        }

        /// <summary>
        /// Try to get cached data for wearables
        /// </summary>
        public bool TryGetWearables(List<string> categories, int? limit, out List<ColorTaggedInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("Wearables", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache wearables data
        /// </summary>
        public void CacheWearables(List<string> categories, int? limit, List<ColorTaggedInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("Wearables", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached data for user wearables
        /// </summary>
        public bool TryGetUserWearables(List<string> categories, int? limit, out List<ColorTaggedInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("UserWearables", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache user wearables data
        /// </summary>
        public void CacheUserWearables(List<string> categories, int? limit, List<ColorTaggedInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("UserWearables", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached avatar base data
        /// </summary>
        public bool TryGetAvatarBase(List<string> categories, int? limit, out List<DefaultAvatarBaseAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarBase", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache avatar base data
        /// </summary>
        public void CacheAvatarBase(List<string> categories, int? limit, List<DefaultAvatarBaseAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarBase", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached avatar eyes data
        /// </summary>
        public bool TryGetAvatarEyes(List<string> categories, int? limit, out List<ColoredInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarEyes", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache avatar eyes data
        /// </summary>
        public void CacheAvatarEyes(List<string> categories, int? limit, List<ColoredInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarEyes", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached avatar flair data
        /// </summary>
        public bool TryGetAvatarFlair(List<string> categories, int? limit, out List<DefaultInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarFlair", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache avatar flair data
        /// </summary>
        public void CacheAvatarFlair(List<string> categories, int? limit, List<DefaultInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarFlair", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached avatar makeup data
        /// </summary>
        public bool TryGetAvatarMakeup(List<string> categories, int? limit, out List<DefaultInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarMakeup", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache avatar makeup data
        /// </summary>
        public void CacheAvatarMakeup(List<string> categories, int? limit, List<DefaultInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("AvatarMakeup", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached color presets data
        /// </summary>
        public bool TryGetColorPresets(List<string> categories, int? limit, out List<ColoredInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("ColorPresets", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache color presets data
        /// </summary>
        public void CacheColorPresets(List<string> categories, int? limit, List<ColoredInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("ColorPresets", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        /// <summary>
        /// Try to get cached image library data
        /// </summary>
        public bool TryGetImageLibrary(List<string> categories, int? limit, out List<DefaultInventoryAsset> items, out string nextCursor)
        {
            var cacheKey = GenerateCacheKey("ImageLibrary", categories, limit);
            return TryGetCached(cacheKey, out items, out nextCursor);
        }

        /// <summary>
        /// Cache image library data
        /// </summary>
        public void CacheImageLibrary(List<string> categories, int? limit, List<DefaultInventoryAsset> items, string nextCursor)
        {
            var cacheKey = GenerateCacheKey("ImageLibrary", categories, limit);
            CacheData(cacheKey, items, nextCursor, limit, categories);
        }

        // -----------------------------
        // Per-asset pipeline resolve cache
        // -----------------------------

        private string GeneratePipelineItemCacheKey(string assetId)
        {
            return $"{_orgContext}_{_appContext}_ResolvePipelineItem_{assetId}";
        }

        private static string MakeFileNameSafe(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public bool TryGetResolvedPipelineItem(string assetId, out AssetPipelineInfo info)
        {
            info = null;

            if (string.IsNullOrEmpty(assetId))
            {
                return false;
            }

            var cacheKey = GeneratePipelineItemCacheKey(assetId);

            try
            {
                if (!_diskCache.IsObjectCached(cacheKey))
                {
                    return false;
                }

                if (!_diskCache.TryGetCachedFilePath(cacheKey, out var filePath))
                {
                    return false;
                }

                var json = File.ReadAllText(filePath);
                var cached = JsonConvert.DeserializeObject<CachedPipelineItemResponse>(json);

                if (cached?.Info == null)
                {
                    return false;
                }

                info = cached.Info;
                return true;
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(
                    new Exception($"Failed to load cached pipeline item for key {cacheKey}: {ex.Message}", ex));
                return false;
            }
        }

        public void CacheResolvedPipelineItem(string assetId, AssetPipelineInfo info)
        {
            if (string.IsNullOrEmpty(assetId) || info == null)
            {
                return;
            }

            var cacheKey = GeneratePipelineItemCacheKey(assetId);

            try
            {
                var cached = new CachedPipelineItemResponse(cacheKey, assetId, info);
                var json = JsonConvert.SerializeObject(cached, Formatting.None);

                // Keep file name safe even if assetId ever contains invalid characters.
                var safeFileName = $"{MakeFileNameSafe(cacheKey)}.json";
                var filePath = Path.Combine(_cacheDirectory, safeFileName);

                File.WriteAllText(filePath, json);

                _diskCache.AddEntry(cacheKey, filePath);
                _diskCache.SaveToFile(_cacheFilePath);
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(
                    new Exception($"Failed to cache pipeline item for key {cacheKey}: {ex.Message}", ex));
            }
        }
        
        /// <summary>
        /// Clear all cached data
        /// </summary>
        public void ClearAll()
        {
            try
            {
                _diskCache.ClearCache(_cacheDirectory, _cacheFilePath);
                _diskCache.SaveToFile(_cacheFilePath);
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Failed to clear disk cache: {ex.Message}", ex));
            }
        }

        public void ClearUserCache()
        {
            try
            {
                _diskCache.ClearCacheEntriesWithKeyword(_cacheFilePath, "User");
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Failed to clear user disk cache entries: {ex.Message}", ex));
            }
        }
    }
}
