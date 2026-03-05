using System.Collections.Generic;
using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Model;
using UnityEngine;
using UniTaskCompletionSource = Cysharp.Threading.Tasks.UniTaskCompletionSource;

namespace Genies.Inventory
{
    /// <summary>
    /// Service to get default assets from inventory, or assets scoped to an app or org.
    /// Replacement for the V1 <see cref="InventoryService"/>, this is a separate service
    /// due to using a different API configuration
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DefaultInventoryService : IDefaultInventoryService
#else
    public class DefaultInventoryService : IDefaultInventoryService
#endif
    {
        private IInventoryV2Api _inventoryApi;
        private UniTaskCompletionSource _apiInitializationSource;
        private readonly string _orgContext, _appContext;
        private readonly DefaultInventoryServiceDiskCache _diskCache;
        private readonly bool _enableDiskCache;

        private bool _isDemoMode = false;
        
        public event Func<List<DefaultInventoryAsset>, UniTask> AssetsAddedAsync;

        #region Constructor and Fetch State + Config Classes

        public DefaultInventoryService(string orgContext = "ALL", string appContext = "SDK_ALL", bool enableDiskCache = true, int cacheExpirationSeconds = 86400, bool isDemoMode = false) // 24 hours
        {
            _orgContext = orgContext;
            _appContext = appContext;
            _isDemoMode = isDemoMode;
            _enableDiskCache = enableDiskCache;

            if (_enableDiskCache)
            {
                _diskCache = new DefaultInventoryServiceDiskCache(_orgContext, _appContext, cacheExpirationSeconds, isDemoMode);
            }

            AwaitApiInitialization().Forget();
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task.Preserve();
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();

            _inventoryApi = new InventoryV2Api();

            // In demo mode, we dont need a token. The intent is that we'll use cached data.
            if (!_isDemoMode)
            { 
                await GeniesLoginSdk.WaitUntilLoggedInAsync();
            }
            
            _inventoryApi.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            _apiInitializationSource.TrySetResult();
            _apiInitializationSource = null;
        }

        private class FetchState<TInventoryItem>
        {
            public Dictionary<string, List<TInventoryItem>> Caches = new(); // Category -> cache
            public HashSet<string> CachedAssetIds = new(); // Track AssetIds already in Caches to prevent duplicates
            public Dictionary<string, UniTaskCompletionSource<List<TInventoryItem>>> CompletionSourcesByCategory = new(); // Track per category
            public Dictionary<string, string> NextCursorsByCategory = new(); // Track cursor per category combination
            public HashSet<string> FetchedCategoryCombinations = new(); // Track which category combinations have been fetched (even if 0 items)
            public readonly object SyncLock = new();
            public List<string> CurrentCategories; // Store categories for LoadMore continuity
            public bool HasFetchedAllCategories; // True if we've fetched with categories==null and NextCursor==null
            public bool HasMoreData(string cacheKey) => NextCursorsByCategory.TryGetValue(cacheKey, out var cursor) && !string.IsNullOrEmpty(cursor);

            /// <summary>
            /// Adds an item to the category cache if its AssetId hasn't been seen before.
            /// Returns true if the item was added (not a duplicate).
            /// </summary>
            public bool TryAddToCache(TInventoryItem item, string category)
            {
                if (item is DefaultInventoryAsset asset && !string.IsNullOrEmpty(asset.AssetId))
                {
                    if (!CachedAssetIds.Add(asset.AssetId))
                    {
                        return false; // Duplicate
                    }
                }

                if (Caches.ContainsKey(category))
                {
                    Caches[category].Add(item);
                }
                else
                {
                    Caches[category] = new List<TInventoryItem> { item };
                }

                return true;
            }

            public void ClearCache()
            {
                lock (SyncLock)
                {
                    foreach (var pair in Caches)
                    {
                        pair.Value?.Clear();
                    }

                    Caches.Clear();
                    CachedAssetIds.Clear();
                    HasFetchedAllCategories = false;
                    CompletionSourcesByCategory.Clear();
                    NextCursorsByCategory.Clear();
                    FetchedCategoryCombinations.Clear();
                }
            }
        }

        /// <summary>
        /// Configuration object that defines how to fetch and map a specific inventory endpoint
        /// </summary>
        private class InventoryEndpointConfig<TResponse, TInventoryItem>
        {
            public FetchState<TInventoryItem> State { get; set; }
            public Func<int?, string, List<string>, Task<TResponse>> FetchFunc { get; set; }
            public Func<TResponse, List<TInventoryItem>> MapFunc { get; set; }
            public Func<TResponse, string> ExtractNextCursor { get; set; }
            public string ErrorContext { get; set; }
        }

        #endregion

        #region Configs and States

        private readonly FetchState<ColorTaggedInventoryAsset> _userWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _defaultWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _allWearablesState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _avatarState = new();
        private readonly FetchState<DefaultAvatarBaseAsset> _avatarBaseState = new();
        private readonly FetchState<DefaultAnimationLibraryAsset> _animationLibraryState = new();
        private readonly FetchState<ColoredInventoryAsset> _avatarEyesState = new();
        private readonly FetchState<DefaultInventoryAsset> _avatarFlairState = new();
        private readonly FetchState<DefaultInventoryAsset> _avatarMakeupState = new();
        private readonly Dictionary<string, FetchState<ColoredInventoryAsset>> _colorPresetsStatesByCategory = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _decorState = new();
        private readonly FetchState<DefaultInventoryAsset> _imageLibraryState = new();
        private readonly FetchState<ColorTaggedInventoryAsset> _modelLibraryState = new();

        private InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset> _userWearablesConfig;
        private InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset> _defaultWearablesConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarResponse, ColorTaggedInventoryAsset> _avatarConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarBaseResponse, DefaultAvatarBaseAsset> _avatarBaseConfig;
        private InventoryEndpointConfig<GetInventoryV2AnimationLibraryResponse, DefaultAnimationLibraryAsset> _animationLibraryConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarEyesResponse, ColoredInventoryAsset> _avatarEyesConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarFlairResponse, DefaultInventoryAsset> _avatarFlairConfig;
        private InventoryEndpointConfig<GetInventoryV2AvatarMakeupResponse, DefaultInventoryAsset> _avatarMakeupConfig;
        private InventoryEndpointConfig<GetInventoryV2ColorPresetsResponse, ColoredInventoryAsset> _colorPresetsConfig;
        private InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset> _decorConfig;
        private InventoryEndpointConfig<GetInventoryV2ImageLibraryResponse, DefaultInventoryAsset> _imageLibraryConfig;
        private InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset> _modelLibraryConfig;

        #endregion

        #region In-Memory Caching Helpers

        /// <summary>
        /// Helper method to generate cache key from category list
        /// </summary>
        private static string GetCacheKey(List<string> categories)
        {
            return categories == null || categories.Count == 0
                ? "__all__"
                : string.Join("|", categories.OrderBy(c => c));
        }

        /// <summary>
        /// Helper method to wait for an all-categories fetch to complete and extract specific categories from the cache
        /// </summary>
        private static async UniTask<List<TInventoryItem>> WaitForAllCategoriesAndExtract<TInventoryItem>(
            UniTaskCompletionSource<List<TInventoryItem>> allCategoriesSource,
            List<string> requestedCategories,
            FetchState<TInventoryItem> state)
        {
            // Wait for the all categories fetch to complete
            await allCategoriesSource.Task.Preserve();

            // Extract the requested categories from the cache
            List<TInventoryItem> result = new();
            lock (state.SyncLock)
            {
                foreach (var category in requestedCategories)
                {
                    if (state.Caches != null && state.Caches.ContainsKey(category))
                    {
                        result.AddRange(state.Caches[category]);
                    }
                    // If category not in cache, it means 0 items for it (but it was fetched)
                }
            }

            return result;
        }

        #endregion

        #region Pagination Helpers

        /// <summary>
        /// Helper method to get the cursor for a given state based on its CurrentCategories
        /// </summary>
        private static string GetCursor<TInventoryItem>(FetchState<TInventoryItem> state)
        {
            if (state == null)
            {
                return null;
            }

            var cacheKey = GetCacheKey(state.CurrentCategories);
            return state.NextCursorsByCategory.TryGetValue(cacheKey, out var cursor) ? cursor : null;
        }

        /// <summary>
        /// Helper method to check if there's more data for a given state based on its CurrentCategories
        /// </summary>
        private static bool HasMoreData<TInventoryItem>(FetchState<TInventoryItem> state)
        {
            if (state == null)
            {
                return false;
            }

            var cacheKey = GetCacheKey(state.CurrentCategories);
            return state.HasMoreData(cacheKey);
        }

        #endregion

        #region Main Fetching Logic

        /// <summary>
        /// A generic method to call an endpoint and handle caching and concurrency.
        /// Used for calling all V2 Inventory endpoints
        /// </summary>
        /// <param name="config">Configuration that defines how to fetch and map this endpoint</param>
        /// <param name="limit">Number of items to fetch per page (null = no limit)</param>
        /// <param name="append">If true, append to existing cache; if false, replace cache</param>
        /// <param name="categories">Optional list of categories to filter by</param>
        /// <typeparam name="TResponse">The expected type to be returned from the fetch function</typeparam>
        /// <typeparam name="TInventoryItem">The type to map data from the fetch function into</typeparam>
        /// <returns>A list of the data mapped into <see cref="TInventoryItem"/> gathered from the fetch function</returns>
        private async UniTask<List<TInventoryItem>> FetchWithConfig<TResponse, TInventoryItem>(
            InventoryEndpointConfig<TResponse, TInventoryItem> config,
            int? limit = null,
            bool append = false,
            List<string> categories = null)
        {
            await AwaitApiInitialization();

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            if (config.State == null)
            {
                throw new NullReferenceException("The state of the config cannot be null");
            }

            var state = config.State;

            // Create a cache key for this category combination to check pagination state
            string cacheKey = GetCacheKey(categories);

            // Check disk cache first (only for initial fetch, not append/pagination)
            if (!append && _enableDiskCache && _diskCache != null)
            {
                List<DefaultInventoryAsset> defaultAssets = new();

                if (TryGetFromDiskCache(config, categories, limit, out var cachedItems, out var cachedCursor))
                {
                    // Populate in-memory cache from disk cache (skipping duplicates)
                    lock (state.SyncLock)
                    {
                        foreach (var item in cachedItems)
                        {
                            if (item is DefaultInventoryAsset asset)
                            {
                                state.TryAddToCache(item, asset.Category);
                                defaultAssets.Add(asset);
                            }
                        }

                        state.FetchedCategoryCombinations.Add(cacheKey);
                        if (!string.IsNullOrEmpty(cachedCursor))
                        {
                            state.NextCursorsByCategory[cacheKey] = cachedCursor;
                        }
                        else if (categories == null)
                        {
                            state.HasFetchedAllCategories = true;
                        }
                    }

                    // If we retrieved disk-cached items on app start, make sure we still hook into NAF events
                    await InvokeAssetsAddedEvent(defaultAssets);

                    return cachedItems;
                }
            }

            if (!append)
            {
                // Check if we've already fetched this exact category combination
                if (state.FetchedCategoryCombinations.Contains(cacheKey))
                {
                    List<TInventoryItem> cache = new();

                    // Return cached results
                    if (categories != null)
                    {
                        foreach (var category in categories)
                        {
                            if (state.Caches.ContainsKey(category))
                            {
                                cache.AddRange(state.Caches[category]);
                            }
                            // If category not in cache, it means 0 items for it (but it was fetched)
                        }
                    }
                    else // categories is null, return all
                    {
                        foreach (var categoryCache in state.Caches)
                        {
                            cache.AddRange(state.Caches[categoryCache.Key]);
                        }
                    }

                    return cache;
                }

                // Check if we've fetched all categories (with categories==null)
                if (categories != null && state.HasFetchedAllCategories)
                {
                    List<TInventoryItem> cache = new();
                    foreach (var category in categories)
                    {
                        if (state.Caches.ContainsKey(category))
                        {
                            cache.AddRange(state.Caches[category]);
                        }
                    }
                    return cache;
                }
            }

            UniTaskCompletionSource<List<TInventoryItem>> completionSource;
            const string allCategoriesKey = "__all__";
            UniTaskCompletionSource<List<TInventoryItem>> allCategoriesSourceToWait = null;

            lock (state.SyncLock)
            {
                // Check if there's already a task in flight for this exact category combination
                if (state.CompletionSourcesByCategory.TryGetValue(cacheKey, out var existingSource))
                {
                    completionSource = existingSource;
                }
                // Check if there's an inflight task for getting all categories when requesting specific categories
                else if (!append && categories != null && cacheKey != allCategoriesKey &&
                         state.CompletionSourcesByCategory.TryGetValue(allCategoriesKey, out var allSource))
                {
                    // Capture the source to wait for it outside the lock
                    allCategoriesSourceToWait = allSource;
                    completionSource = null; // Will be handled separately
                }
                else
                {
                    // Start a new fetch for this category combination
                    completionSource = new UniTaskCompletionSource<List<TInventoryItem>>();
                    state.CompletionSourcesByCategory[cacheKey] = completionSource;

                    // Start the fetch operation asynchronously
                    FetchInternal().Forget();
                }
            }

            // If we found an inflight all-categories fetch, wait for it and extract needed categories
            if (allCategoriesSourceToWait != null)
            {
                return await WaitForAllCategoriesAndExtract(allCategoriesSourceToWait, categories, state);
            }

            return await completionSource.Task.Preserve();

            async UniTask FetchInternal()
            {
                try
                {
                    // Get cursor for this specific category combination
                    string cursor = null;
                    if (append && state.NextCursorsByCategory.TryGetValue(cacheKey, out var existingCursor))
                    {
                        cursor = existingCursor;
                    }

                    List<string> categoriesToUse;

                    if (append)
                    {
                        // Use stored categories for LoadMore continuity
                        categoriesToUse = state.CurrentCategories;
                    }
                    else
                    {
                        // Store categories for future LoadMore calls
                        state.CurrentCategories = categories;
                        categoriesToUse = categories;
                    }

                    var response = await config.FetchFunc(limit, cursor, categoriesToUse);
                    if (response == null)
                    {
                        CrashReporter.LogError($"{config.ErrorContext}: response was null");
                        List<TInventoryItem> emptyList = new();

                        // Mark as fetched even on null response
                        lock (state.SyncLock)
                        {
                            state.FetchedCategoryCombinations.Add(cacheKey);
                        }

                        completionSource.TrySetResult(emptyList);
                        return;
                    }

                    var items = config.MapFunc(response) ?? new List<TInventoryItem>();

                    // Extract and store the next cursor for this category combination
                    if (config.ExtractNextCursor != null)
                    {
                        var nextCursor = config.ExtractNextCursor(response);
                        lock (state.SyncLock)
                        {
                            state.NextCursorsByCategory[cacheKey] = nextCursor;
                        }

                        // If we fetched with categories==null and there's no more data, mark as complete
                        if (categoriesToUse == null && string.IsNullOrEmpty(nextCursor))
                        {
                            state.HasFetchedAllCategories = true;
                        }
                    }

                    // Add items to their respective category cache (skipping duplicates)
                    List<DefaultInventoryAsset> assets = new();
                    foreach (var item in items)
                    {
                        if (item is DefaultInventoryAsset asset)
                        {
                            state.TryAddToCache(item, asset.Category);
                            assets.Add(asset);
                        }
                    }

                    // Ensure cache entries exist for all requested categories (even if empty)
                    if (categoriesToUse != null)
                    {
                        foreach (var category in categoriesToUse)
                        {
                            if (!state.Caches.ContainsKey(category))
                            {
                                state.Caches[category] = new List<TInventoryItem>();
                            }
                        }
                    }

                    // Mark this category combination as fetched
                    lock (state.SyncLock)
                    {
                        state.FetchedCategoryCombinations.Add(cacheKey);
                    }

                    // Save to disk cache
                    if (_enableDiskCache && _diskCache != null && !append)
                    {
                        SaveToDiskCache(config, categoriesToUse, limit, items, config.ExtractNextCursor?.Invoke(response));
                    }

                    await InvokeAssetsAddedEvent(assets);

                    completionSource.TrySetResult(items);
                }
                catch (Exception ex)
                {
                    CrashReporter.LogError($"{config.ErrorContext}: {ex.Message}");
                    var emptyList = new List<TInventoryItem>();

                    // Mark as fetched even on error to avoid infinite retry loops
                    lock (state.SyncLock)
                    {
                        state.FetchedCategoryCombinations.Add(cacheKey);
                    }

                    completionSource.TrySetResult(emptyList);
                }
                finally
                {
                    lock (state.SyncLock)
                    {
                        // Remove this category combination from in-flight tasks
                        state.CompletionSourcesByCategory.Remove(cacheKey);
                    }
                }
            }
        }

        #endregion

        #region Config Initialization

        /// <summary>
        /// Initializes endpoint configurations with their respective mapping logic
        /// </summary>
        private void InitializeConfigurations()
        {
            _defaultWearablesConfig ??= new InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset>
            {
                State = _defaultWearablesState,
                FetchFunc = (limit, cursor, categories) => FetchGearAsync(includeUser: false, includeDefault: true, limit: limit, cursor: cursor, categories: categories),
                MapFunc = MapGear,
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default gear assets"
            };

            _userWearablesConfig ??= new InventoryEndpointConfig<GetInventoryV2GearResponse, ColorTaggedInventoryAsset>
            {
                State = _userWearablesState,
                FetchFunc = (limit, cursor, categories) => FetchGearAsync(includeUser: true, includeDefault: false, limit: limit, cursor: cursor, categories: categories),
                MapFunc = MapGear,
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting user gear assets"
            };

            _avatarConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarResponse, ColorTaggedInventoryAsset>
            {
                State = _avatarState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.Avatar?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Avatar,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.ModelType,
                    Order = item.Asset?.Order ?? 0,
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar assets"
            };

            _avatarBaseConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarBaseResponse, DefaultAvatarBaseAsset>
            {
                State = _avatarBaseState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarBaseAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarBase?.Select(item => new DefaultAvatarBaseAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarBase,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    SubCategories = item.Asset?.Subcategories,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    Tags = item.Asset?.Tags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar base assets"
            };

            _animationLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2AnimationLibraryResponse, DefaultAnimationLibraryAsset>
            {
                State = _animationLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAnimationLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 500, nextCursor: cursor),
                MapFunc = response => response.AnimationLibrary?.Select(item => new DefaultAnimationLibraryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AnimationLibrary,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First()),
                    MoodsTag = item.Asset?.MoodsTag,
                    ChildAssets = item.Asset?.ChildAssets?
                        .Select(child => new DefaultAnimationChildAsset(child))
                        .ToList() ?? new List<DefaultAnimationChildAsset>()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default animation library assets"
            };

            _avatarEyesConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarEyesResponse, ColoredInventoryAsset>
            {
                State = _avatarEyesState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarEyesAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarEyes?.Select(item => new ColoredInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarEyes,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    Colors = item.Asset?.HexColors?.Select(c => c.ToUnityColor()).ToList()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar eyes assets"
            };

            _avatarFlairConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarFlairResponse, DefaultInventoryAsset>
            {
                State = _avatarFlairState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarFlairAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarFlair?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Flair,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = new List<string>(),
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First())

                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar flair assets"
            };

            _avatarMakeupConfig ??= new InventoryEndpointConfig<GetInventoryV2AvatarMakeupResponse, DefaultInventoryAsset>
            {
                State = _avatarMakeupState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultAvatarMakeupAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.AvatarMakeup?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.AvatarMakeup,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    SubCategories = item.Asset?.Subcategories,
                    PipelineData =  new PipelineData(item.Asset?.Pipeline.First())
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default avatar makeup assets"
            };

            _colorPresetsConfig ??= new InventoryEndpointConfig<GetInventoryV2ColorPresetsResponse, ColoredInventoryAsset>
            {
                // State is set dynamically per category in GetDefaultColorPresets
                State = null,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultColorPresetsAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 200, nextCursor: cursor),
                MapFunc = response => response.ColorPresets?.Select(item => new ColoredInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ColorPreset,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = item.Asset?.Subcategories,
                    Order = item.Asset?.Order ?? 0,
                    Colors = item.Asset?.HexColors?.Select(c => c.ToUnityColor()).ToList()
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default color preset assets"
            };

            _decorConfig ??= new InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset>
            {
                State = _decorState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultDecorAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.ModelLibrary?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.Decor,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    Order = item.Asset?.Order ?? 0,
                    PipelineData =  item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                        ? new PipelineData(item.Asset.Pipeline.First())
                        : null,
                    ColorTags = item.Asset?.ColorTags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default decor assets"
            };

            _imageLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2ImageLibraryResponse, DefaultInventoryAsset>
            {
                State = _imageLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultImageLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 500, nextCursor: cursor),
                MapFunc = response => response.ImageLibrary?.Select(item => new DefaultInventoryAsset
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ImageLibrary,
                    Name = item.Asset.Name,
                    Category = item.Asset.Category,
                    SubCategories = item.Asset.Subcategories,
                    Order = item.Asset.Order ?? 0,
                    PipelineData =  new PipelineData(item.Asset.Pipeline.First())
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default image library assets"
            };

            _modelLibraryConfig ??= new InventoryEndpointConfig<GetInventoryV2ModelLibraryResponse, ColorTaggedInventoryAsset>
            {
                State = _modelLibraryState,
                FetchFunc = (limit, cursor, categories) => _inventoryApi.GetDefaultModelLibraryAsync(orgId: _orgContext, appId: _appContext, category: categories, limit: limit ?? 100, nextCursor: cursor),
                MapFunc = response => response.ModelLibrary?.Select(item => new ColorTaggedInventoryAsset()
                {
                    AssetId = item.Asset?.AssetId,
                    AssetType = AssetType.ModelLibrary,
                    Name = item.Asset?.Name,
                    Category = item.Asset?.Category,
                    SubCategories = item.Asset?.Subcategories,
                    Order = item.Asset?.Order ?? 0,
                    ColorTags = item.Asset?.ColorTags
                }).ToList(),
                ExtractNextCursor = response => response?.NextCursor,
                ErrorContext = "Error getting default model library assets"
            };
        }

        #endregion

        #region Gear Fetching Helpers

        private async Task<GetInventoryV2GearResponse> FetchGearAsync(
            bool includeUser = true,
            bool includeDefault = true,
            int? limit = null,
            string cursor = null,
            List<string> categories = null)
        {
            Task<GetInventoryV2GearResponse> userTask = null;
            Task<GetInventoryV2GearResponse> defaultTask = null;

            if (includeUser)
            {
                userTask = _inventoryApi.GetInventoryV2GearAsync(await GeniesLoginSdk.GetUserIdAsync(),
                    category: categories, limit: limit ?? 500, nextCursor: cursor);
            }

            if (includeDefault)
            {
                defaultTask = _inventoryApi.GetDefaultGearAsync(orgId: _orgContext, appId: _appContext,
                    category: categories, limit: limit ?? 500, nextCursor: cursor);
            }

            var tasks = new List<Task<GetInventoryV2GearResponse>>();
            if (userTask != null)
            {
                tasks.Add(userTask);
            }

            if (defaultTask != null)
            {
                tasks.Add(defaultTask);
            }

            await Task.WhenAll(tasks);

            var combined = new GetInventoryV2GearResponse { Gear = new() };

            if (userTask?.Result?.Gear != null)
            {
                combined.Gear.AddRange(userTask.Result.Gear);
                combined.NextCursor = userTask.Result.NextCursor;
            }

            if (defaultTask?.Result?.Gear != null)
            {
                combined.Gear.AddRange(defaultTask.Result.Gear);
                // Use default's cursor if user task didn't have one
                if (string.IsNullOrEmpty(combined.NextCursor))
                {
                    combined.NextCursor = defaultTask.Result.NextCursor;
                }
            }

            return combined;
        }

        private List<ColorTaggedInventoryAsset> MapGear(GetInventoryV2GearResponse response)
        {
            return response.Gear?.Select(item => new ColorTaggedInventoryAsset()
            {
                AssetId = item.Asset?.AssetId,
                AssetType = AssetType.WardrobeGear,
                Name = item.Asset?.Name,
                Category = item.Asset?.Category,
                Order = item.Asset?.Order ?? 0,
                PipelineData = item.Asset?.Pipeline != null && item.Asset.Pipeline.Any()
                    ? new PipelineData(item.Asset.Pipeline.First())
                    : null,
                ColorTags = item.Asset?.ColorTags
            }).ToList();
        }

        #endregion

        #region Get Methods and Pagination

        /// <summary>
        /// Methods for outside users to call to get inventory data
        /// </summary>
        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultWearables(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_defaultWearablesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultWearables()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_defaultWearablesConfig, InventoryConstants.DefaultPageSize, append: true);
        }

        public bool HasMoreDefaultWearables() => HasMoreData(_defaultWearablesState);

        public async UniTask<List<ColorTaggedInventoryAsset>> GetUserWearables(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_userWearablesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreUserWearables()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_userWearablesConfig, append: true);
        }

        public bool HasMoreUserWearables() => HasMoreData(_userWearablesState);

        public async UniTask<List<ColorTaggedInventoryAsset>> GetAllWearables(int? limit = null, List<string> categories = null)
        {
            var userWearablesTask = GetUserWearables(limit, categories);
            var defaultWearablesTask = GetDefaultWearables(limit, categories);
            var (userWearables, defaultWearables) = await UniTask.WhenAll(userWearablesTask, defaultWearablesTask);

            // Create a new list to avoid modifying the cached lists
            var allWearables = new List<ColorTaggedInventoryAsset>(userWearables);
            allWearables.AddRange(defaultWearables);
            return allWearables;
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreAllWearables()
        {
            var userWearables = await LoadMoreUserWearables();
            var defaultWearables = await LoadMoreDefaultWearables();

            // Create a new list to avoid modifying the cached lists
            var allWearables = new List<ColorTaggedInventoryAsset>(userWearables);
            allWearables.AddRange(defaultWearables);
            return allWearables;
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultAvatar(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultAvatar()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarConfig, append: true);
        }

        public bool HasMoreDefaultAvatar() => HasMoreData(_avatarState);

        public async UniTask<List<DefaultAvatarBaseAsset>> GetDefaultAvatarBaseData(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarBaseConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultAvatarBaseAsset>> LoadMoreDefaultAvatarBaseData()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarBaseConfig, append: true);
        }

        public string NextDefaultWearablesCursor() => GetCursor(_defaultWearablesState);

        public string NextUserWearablesCursor() => GetCursor(_userWearablesState);

        public string NextDefaultAvatarCursor() => GetCursor(_avatarState);

        public string NextDefaultAvatarBaseCursor() => GetCursor(_avatarBaseState);

        public string NextDefaultAnimationLibraryCursor() => GetCursor(_animationLibraryState);

        public string NextDefaultAvatarEyesCursor() => GetCursor(_avatarEyesState);

        public string NextDefaultAvatarFlairCursor() => GetCursor(_avatarFlairState);

        public string NextDefaultAvatarMakeupCursor() => GetCursor(_avatarMakeupState);

        public string NextDefaultColorPresetsCursor() => GetCursor(_colorPresetsConfig?.State);

        public string NextDefaultDecorCursor() => GetCursor(_decorState);

        public string NextDefaultImageLibraryCursor() => GetCursor(_imageLibraryState);

        public string NextDefaultModelLibraryCursor() => GetCursor(_modelLibraryState);

        public async UniTask<List<DefaultAnimationLibraryAsset>> GetDefaultAnimationLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_animationLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultAnimationLibraryAsset>> LoadMoreDefaultAnimationLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_animationLibraryConfig, append: true);
        }

        public bool HasMoreDefaultAnimationLibrary() => HasMoreData(_animationLibraryState);

        public async UniTask<List<ColoredInventoryAsset>> GetDefaultAvatarEyes(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarEyesConfig, limit, categories: categories);
        }

        public async UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultAvatarEyes()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarEyesConfig, append: true);
        }

        public bool HasMoreDefaultAvatarEyes() => HasMoreData(_avatarEyesState);

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarFlair(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarFlairConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarFlair()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarFlairConfig, append: true);
        }

        public bool HasMoreDefaultAvatarFlair() => HasMoreData(_avatarFlairState);

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarMakeup(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarMakeupConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarMakeup()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_avatarMakeupConfig, append: true);
        }

        public bool HasMoreDefaultAvatarMakeup() => HasMoreData(_avatarMakeupState);

        public async UniTask<List<ColoredInventoryAsset>> GetDefaultColorPresets(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();

            // Get or create a state for this category
            string categoryKey = categories?.FirstOrDefault() ?? "default";

            if (_colorPresetsStatesByCategory.TryGetValue(categoryKey, out var state) is false)
            {
                state = new FetchState<ColoredInventoryAsset>();
                _colorPresetsStatesByCategory[categoryKey] = state;
            }

            _colorPresetsConfig.State = state;

            return await FetchWithConfig(_colorPresetsConfig, limit, categories: categories);
        }

        public async UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultColorPresets()
        {
            InitializeConfigurations();
            // State should already be set from the initial GetDefaultColorPresets call
            return await FetchWithConfig(_colorPresetsConfig, append: true);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultDecor(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_decorConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultDecor()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_decorConfig, append: true);
        }

        public bool HasMoreDefaultDecor() => HasMoreData(_decorState);

        public async UniTask<List<DefaultInventoryAsset>> GetDefaultImageLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_imageLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultImageLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_imageLibraryConfig, append: true);
        }

        public bool HasMoreDefaultImageLibrary() => HasMoreData(_imageLibraryState);

        public async UniTask<List<ColorTaggedInventoryAsset>> GetDefaultModelLibrary(int? limit = null, List<string> categories = null)
        {
            InitializeConfigurations();
            return await FetchWithConfig(_modelLibraryConfig, limit, categories: categories);
        }

        public async UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultModelLibrary()
        {
            InitializeConfigurations();
            return await FetchWithConfig(_modelLibraryConfig, append: true);
        }

        public bool HasMoreDefaultModelLibrary() => HasMoreData(_modelLibraryState);

        private void ClearUserWearablesCache()
        {
            // Clear user wearables cache
            if (_userWearablesState != null)
            {
                _userWearablesState.ClearCache();
            }

            // Clear all wearables cache since it combines user + default
            if (_allWearablesState != null)
            {
                _allWearablesState.ClearCache();
            }

            // Clear user asset disk cache entries
            _diskCache?.ClearUserCache();
        }

        #endregion

        #region Asset Minting and Resolving

        public async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                string error = "You must be logged in in order to have an asset gifted";
                CrashReporter.LogWarning(error);
                return (false, error);
            }

            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                string error = "Anonymous users cannot be gifted assets.";
                CrashReporter.LogWarning(error);
                return (false, error);
            }

            try
            {
                var userAssets = await GetUserWearables();
                var userAssetIds = userAssets.Select(u => u.AssetId).ToList();
                if (userAssetIds.Contains(assetId))
                {
                    string note = $"Asset with id {assetId} is already in the user's inventory";
                    CrashReporter.Log(note);
                    return (true, note);
                }

                var request = new MintAssetOnceRequest(assetId,  Guid.NewGuid());
                await _inventoryApi.MintAssetOnceAsync(request, await GeniesLoginSdk.GetUserIdAsync());

                ClearUserWearablesCache();
                var assets = await GetUserWearables();

                await InvokeAssetsAddedEvent(new List<DefaultInventoryAsset>{ assets.FirstOrDefault() });

                return (true, "Asset granted");
            }
            catch(Exception ex)
            {
                string error = $"Exception trying to mint asset to a user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }

        /// <summary>
        /// Resolves a list of asset IDs to their pipeline metadata information.
        /// This is designed for on-demand asset loading in NAF/Unity clients.
        /// </summary>
        /// <param name="assetIds">List of asset IDs to resolve</param>
        /// <returns>Dictionary mapping asset IDs to their AssetPipelineInfo</returns>
        public async UniTask<Dictionary<string, AssetPipelineInfo>> ResolvePipelineItemsAsync(List<string> assetIds)
        {
            await AwaitApiInitialization();

            if (assetIds == null || assetIds.Count == 0)
            {
                return new Dictionary<string, AssetPipelineInfo>();
            }

            // Normalize input
            var normalizedIds = assetIds
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return new Dictionary<string, AssetPipelineInfo>();
            }

            var result = new Dictionary<string, AssetPipelineInfo>(normalizedIds.Count);
            var idsToFetch = new List<string>();

            // 1) Try disk cache per asset
            if (_enableDiskCache && _diskCache != null)
            {
                foreach (var id in normalizedIds)
                {
                    try
                    {
                        if (_diskCache.TryGetResolvedPipelineItem(id, out var cachedInfo) && cachedInfo != null)
                        {
                            result[id] = cachedInfo;
                        }
                        else
                        {
                            idsToFetch.Add(id);
                        }
                    }
                    catch (Exception ex)
                    {
                        CrashReporter.LogHandledException(new Exception(
                            $"Error reading pipeline item {id} from disk cache: {ex.Message}", ex));
                        idsToFetch.Add(id);
                    }
                }
            }
            else
            {
                idsToFetch.AddRange(normalizedIds);
            }

            // If everything was cached, we’re done
            if (idsToFetch.Count == 0)
            {
                return result;
            }

            // 2) Fetch only cache misses
            try
            {
                var request = new ResolvePipelineItemsRequest(idsToFetch);
                var response = await _inventoryApi.ResolvePipelineItemsAsync(request);

                var fetched = response?.Assets ?? new Dictionary<string, AssetPipelineInfo>();

                // Merge + write-through cache per asset
                foreach (var kvp in fetched)
                {
                    var id = kvp.Key;
                    var info = kvp.Value;

                    if (info == null)
                    {
                        continue;
                    }

                    result[id] = info;

                    if (_enableDiskCache && _diskCache != null)
                    {
                        try
                        {
                            _diskCache.CacheResolvedPipelineItem(id, info);
                        }
                        catch (Exception ex)
                        {
                            CrashReporter.LogHandledException(new Exception(
                                $"Error saving pipeline item {id} to disk cache: {ex.Message}", ex));
                        }
                    }
                }
                
                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError(
                    $"Exception trying to resolve pipeline items for {idsToFetch.Count} assets: {ex.Message}");

                // Return whatever we got from cache (partial is still useful)
                return result;
            }
        }

        private async UniTask InvokeAssetsAddedEvent(List<DefaultInventoryAsset> assets)
        {
            if (AssetsAddedAsync != null)
            {
                var tasks = AssetsAddedAsync
                    .GetInvocationList()
                    .Cast<Func<List<DefaultInventoryAsset>, UniTask>>()
                    .Select(h => h(assets))
                    .ToArray();

                await UniTask.WhenAll(tasks);
            }
        }

        #endregion

        #region Custom Colors

        public async UniTask<string> CreateCustomColor(List<Color> colors, CreateCustomColorRequest.CategoryEnum category)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return null;
            }

            List<string> hexColors = new();
            foreach (var color in colors)
            {
                string hex = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                hexColors.Add(hex);
            }

            try
            {
                var request = new CreateCustomColorRequest(category, hexColors, null, _appContext, _orgContext);
                var response = await _inventoryApi.CreateCustomColorAsync(request, await GeniesLoginSdk.GetUserIdAsync());
                return response.InstanceId;
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
                return null;
            }
        }

        public async UniTask UpdateCustomColor(string instanceId, List<Color> colors)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return;
            }

            List<string> hexColors = new();
            foreach (var color in colors)
            {
                string hex = $"#{ColorUtility.ToHtmlStringRGBA(color)}";
                hexColors.Add(hex);
            }

            try
            {
                var request = new UpdateCustomColorRequest(hexColors);
                await _inventoryApi.UpdateCustomColorAsync(request, await GeniesLoginSdk.GetUserIdAsync(), instanceId);
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
            }
        }

        public async UniTask DeleteCustomColor(string instanceId, List<Color> colors)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to create a custom color");
                return;
            }

            try
            {
                await _inventoryApi.DeleteCustomColorAsync(await GeniesLoginSdk.GetUserIdAsync(), instanceId);
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to create a custom color: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets custom colors with full metadata including instance IDs for a specific category
        /// </summary>
        /// <param name="category">Category to filter by (hair, skin, flair)</param>
        /// <returns>List of custom color responses with instance IDs and metadata</returns>
        public async UniTask<List<CustomColorResponse>> GetCustomColors(string category = null)
        {
            await AwaitApiInitialization();

            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You must be logged in in order to get custom colors");
                return new();
            }

            try
            {
                var response = await _inventoryApi.ListCustomColorsAsync(await GeniesLoginSdk.GetUserIdAsync(), category, _appContext, _orgContext);
                return response.Colors ?? new List<CustomColorResponse>();
            }
            catch(Exception ex)
            {
                CrashReporter.LogError($"Exception trying to get custom colors: {ex.Message}");
                return new();
            }
        }

        #endregion

        #region Disk Cache

        /// <summary>
        /// Clear all disk caches
        /// </summary>
        public void ClearDiskCache()
        {
            if (_enableDiskCache && _diskCache != null)
            {
                _diskCache.ClearAll();
            }
        }

        /// <summary>
        /// Try to get data from disk cache based on config type.
        /// Currently only used for default endpoints (not user-specific data)
        /// </summary>
        private bool TryGetFromDiskCache<TResponse, TInventoryItem>(
            InventoryEndpointConfig<TResponse, TInventoryItem> config,
            List<string> categories,
            int? limit,
            out List<TInventoryItem> items,
            out string nextCursor)
        {
            items = null;
            nextCursor = null;

            try
            {
                // Determine endpoint type from config
                if (ReferenceEquals(config, _userWearablesConfig) && typeof(TInventoryItem) == typeof(ColorTaggedInventoryAsset))
                {
                    if (_diskCache.TryGetUserWearables(categories, limit, out var wearables, out nextCursor))
                    {
                        items = wearables as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _defaultWearablesConfig) && typeof(TInventoryItem) == typeof(ColorTaggedInventoryAsset))
                {
                    if (_diskCache.TryGetWearables(categories, limit, out var wearables, out nextCursor))
                    {
                        items = wearables as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _avatarBaseConfig) && typeof(TInventoryItem) == typeof(DefaultAvatarBaseAsset))
                {
                    if (_diskCache.TryGetAvatarBase(categories, limit, out var avatarBase, out nextCursor))
                    {
                        items = avatarBase as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _avatarEyesConfig) && typeof(TInventoryItem) == typeof(ColoredInventoryAsset))
                {
                    if (_diskCache.TryGetAvatarEyes(categories, limit, out var avatarEyes, out nextCursor))
                    {
                        items = avatarEyes as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _avatarFlairConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    if (_diskCache.TryGetAvatarFlair(categories, limit, out var avatarFlair, out nextCursor))
                    {
                        items = avatarFlair as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _avatarMakeupConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    if (_diskCache.TryGetAvatarMakeup(categories, limit, out var avatarMakeup, out nextCursor))
                    {
                        items = avatarMakeup as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _colorPresetsConfig) && typeof(TInventoryItem) == typeof(ColoredInventoryAsset))
                {
                    if (_diskCache.TryGetColorPresets(categories, limit, out var colorPresets, out nextCursor))
                    {
                        items = colorPresets as List<TInventoryItem>;
                        return true;
                    }
                }
                else if (ReferenceEquals(config, _imageLibraryConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    if (_diskCache.TryGetImageLibrary(categories, limit, out var imageLibrary, out nextCursor))
                    {
                        items = imageLibrary as List<TInventoryItem>;
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Error reading from disk cache: {ex.Message}", ex));
            }

            return false;
        }

        /// <summary>
        /// Save data to disk cache based on config type
        /// </summary>
        private void SaveToDiskCache<TResponse, TInventoryItem>(
            InventoryEndpointConfig<TResponse, TInventoryItem> config,
            List<string> categories,
            int? limit,
            List<TInventoryItem> items,
            string nextCursor)
        {
            try
            {
                // Determine endpoint type from config
                if (ReferenceEquals(config, _userWearablesConfig) && typeof(TInventoryItem) == typeof(ColorTaggedInventoryAsset))
                {
                    _diskCache.CacheUserWearables(categories, limit, items as List<ColorTaggedInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _defaultWearablesConfig) && typeof(TInventoryItem) == typeof(ColorTaggedInventoryAsset))
                {
                    _diskCache.CacheWearables(categories, limit, items as List<ColorTaggedInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _avatarBaseConfig) && typeof(TInventoryItem) == typeof(DefaultAvatarBaseAsset))
                {
                    _diskCache.CacheAvatarBase(categories, limit, items as List<DefaultAvatarBaseAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _avatarEyesConfig) && typeof(TInventoryItem) == typeof(ColoredInventoryAsset))
                {
                    _diskCache.CacheAvatarEyes(categories, limit, items as List<ColoredInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _avatarFlairConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    _diskCache.CacheAvatarFlair(categories, limit, items as List<DefaultInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _avatarMakeupConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    _diskCache.CacheAvatarMakeup(categories, limit, items as List<DefaultInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _colorPresetsConfig) && typeof(TInventoryItem) == typeof(ColoredInventoryAsset))
                {
                    _diskCache.CacheColorPresets(categories, limit, items as List<ColoredInventoryAsset>, nextCursor);
                }
                else if (ReferenceEquals(config, _imageLibraryConfig) && typeof(TInventoryItem) == typeof(DefaultInventoryAsset))
                {
                    _diskCache.CacheImageLibrary(categories, limit, items as List<DefaultInventoryAsset>, nextCursor);
                }

            }
            catch (Exception ex)
            {
                CrashReporter.LogHandledException(new Exception($"Error saving to disk cache: {ex.Message}", ex));
            }
        }

        #endregion
    }
}
