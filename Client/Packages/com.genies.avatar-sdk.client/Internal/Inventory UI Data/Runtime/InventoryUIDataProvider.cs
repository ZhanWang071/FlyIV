using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using UnityEngine;
using Genies.Assets.Services;
using Genies.Customization.Framework;
using Genies.Naf.Content;
using Genies.ServiceManagement;

namespace Genies.Inventory.UIData
{
    /// <summary>
    /// Config that handles how data for a particular asset type should be loaded,
    /// what should be compared to for categories and subcategories, how the data should be sorted,
    /// and how it should be converted into the UI type
    /// </summary>
    /// <typeparam name="TAsset">The type of asset to make into UI data</typeparam>
    /// <typeparam name="TUI">The UI data type</typeparam>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class InventoryUIDataProviderConfig<TAsset, TUI> : IInventoryUIProviderConfig
#else
    public class InventoryUIDataProviderConfig<TAsset, TUI> : IInventoryUIProviderConfig
#endif
        where TUI : IAssetUiData
    {
        public Func<List<string>, string, int?, UniTask<PagedResult<TAsset>>>  DataGetter { get; set; }  // Accepts categories list, subcategory, and page size for server-side filtering
        public Func<UniTask<PagedResult<TAsset>>>  LoadMoreGetter { get; set; }  // For pagination
        public Func<TAsset, string> CategorySelector { get; set; }
        public Func<TAsset, string> SubcategorySelector { get; set; }
        public Func<TAsset, object> Sort { get; set; }
        public Func<TAsset, TUI> DataConverter { get; set; }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal record PagedResult<TAsset>
#else
    public record PagedResult<TAsset>
#endif
    {
        public List<TAsset> Data { get; set; }
        public string NextCursor { get; set; }
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IInventoryUIProviderConfig
#else
    public interface IInventoryUIProviderConfig
#endif
    {
        // Concrete interface to get UI Provider configs from without having to use generic types
    }

    /// <summary>
    /// Base provider class that handles loading and organizing data from the inventory for UI display
    /// </summary>
    /// <typeparam name="TAsset">The asset type being used</typeparam>
    /// <typeparam name="TUI">The UI type being used</typeparam>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class InventoryUIDataProvider<TAsset, TUI> : IUIProvider
#else
    public class InventoryUIDataProvider<TAsset, TUI> : IUIProvider
#endif
        where TUI : IAssetUiData
    {
        private readonly InventoryUIDataProviderConfig<TAsset, TUI> _dataConfig;
        private readonly Dictionary<string, TUI> _cache;
        private readonly IAssetsService _assetsService;

        public string NextCursor => _nextCursor;
        private string _nextCursor;
        private bool _initialized;
        private UniTaskCompletionSource<bool> _initializationCompletionSource;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);
        private List<string> _lastUsedCategories;


        // Pagination support
        private bool _isLoadingMore;
        public bool HasMoreData => !string.IsNullOrEmpty(_nextCursor) || _uiPaginationIndex < _allFetchedData.Count;
        public bool IsLoadingMore => _isLoadingMore;

        // UI-level pagination (independent of service caching)
        private readonly List<TUI> _allFetchedData = new();
        private int _uiPaginationIndex = 0;

        public InventoryUIDataProvider(InventoryUIDataProviderConfig<TAsset, TUI> dataConfig, IAssetsService assetsService)
        {
            _dataConfig = dataConfig;
            _assetsService = assetsService;
            _cache = new();
        }

        public async UniTask ReloadAsync()
        {
            if (_initializationCompletionSource != null)
            {
                await _initializationCompletionSource.Task;
            }

            _nextCursor = null;
            _initialized = false;
            _uiPaginationIndex = 0;
            _allFetchedData.Clear();
            await Initialize();
        }

        private async UniTask<bool> Initialize(List<string> categories = null, string subCategory = null, int? pageSize = null)
        {
            // Check if already initialized with the same categories
            bool categoriesMatch = false;
            if (_lastUsedCategories == null && categories == null)
            {
                categoriesMatch = true;
            }
            else if (_lastUsedCategories != null && categories != null &&
                     _lastUsedCategories.Count == categories.Count &&
                     _lastUsedCategories.All(categories.Contains))
            {
                categoriesMatch = true;
            }

            // If already initialized with matching categories, don't re-initialize
            if (_initialized && categoriesMatch)
            {
                return true;
            }

            if (_initializationCompletionSource != null)
            {
                return await _initializationCompletionSource.Task;
            }

            _initializationCompletionSource = new UniTaskCompletionSource<bool>();
            _initialized = false;
            _nextCursor = null;
            _uiPaginationIndex = 0;
            _allFetchedData?.Clear();

            _lastUsedCategories = categories;

            try
            {
                await LoadUIData(categories, subCategory, pageSize);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Exception initializing UI data: {ex.Message}");
                _initializationCompletionSource.TrySetResult(false);
                return false;
            }

            _initialized = true;
            _initializationCompletionSource.TrySetResult(true);
            return true;
        }



        public async UniTask<List<TUI>> LoadUIData(List<string> categories = null, string subcategory = null, int? pageSize = null)
        {
            var pagedResult = await _dataConfig.DataGetter(categories, subcategory, pageSize);
            _nextCursor = pagedResult.NextCursor;

            List<TUI> allResults;

            await _cacheLock.WaitAsync();
            try
            {
                allResults = pagedResult.Data
                    .Where(asset => asset != null)
                    .OrderBy(asset => _dataConfig.Sort?.Invoke(asset) ?? 0)
                    .Select(asset => _dataConfig.DataConverter(asset))
                    .ToList();

                _allFetchedData.AddRange(allResults);

                var pageToCache = _allFetchedData
                    .Skip(_uiPaginationIndex)
                    .Take(pageSize ?? _allFetchedData.Count)
                    .ToList();

                foreach (var uiData in pageToCache)
                {
                    if (!string.IsNullOrEmpty(uiData.AssetId))
                    {
                        _cache[uiData.AssetId] = uiData;
                    }
                }

                _uiPaginationIndex += pageToCache.Count;
            }
            finally
            {
                _cacheLock.Release();
            }

            if (_cache.Count > 0 && _cache.Values.First() is BasicInventoryUiData)
            {
                await LoadThumbnailsAsync(_assetsService);
            }

            return _cache.Values.ToList();
        }

        /// <summary>
        /// Load more data incrementally for pagination (UI-level pagination)
        /// </summary>
        public async UniTask<List<TUI>> LoadMoreAsync(List<string> categories = null, string subcategory = null)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (_isLoadingMore)
                {
                    return new List<TUI>();
                }

                _isLoadingMore = true;
            }
            finally
            {
                _cacheLock.Release();
            }

            try
            {
                List<TUI> pageToCache;

                await _cacheLock.WaitAsync();
                try
                {
                    // Load from staging if available
                    if (_uiPaginationIndex < _allFetchedData.Count)
                    {
                        pageToCache = _allFetchedData
                            .Skip(_uiPaginationIndex)
                            .Take(InventoryConstants.DefaultPageSize)
                            .ToList();

                        foreach (var uiData in pageToCache)
                        {
                            _cache[uiData.AssetId] = uiData;
                        }

                        _uiPaginationIndex += pageToCache.Count;
                    }
                    // Fetch more from backend if staging is exhausted
                    else if (!string.IsNullOrEmpty(_nextCursor) && _dataConfig.LoadMoreGetter != null)
                    {
                        _cacheLock.Release(); // release before async fetch
                        var pagedResult = await _dataConfig.LoadMoreGetter();
                        await _cacheLock.WaitAsync();

                        _nextCursor = pagedResult.NextCursor;

                        var newResults = pagedResult.Data
                            .Where(asset => asset != null)
                            .OrderBy(asset => _dataConfig.Sort?.Invoke(asset) ?? 0)
                            .Select(asset => _dataConfig.DataConverter(asset))
                            .ToList();

                        _allFetchedData.AddRange(newResults);

                        pageToCache = _allFetchedData
                            .Skip(_uiPaginationIndex)
                            .Take(InventoryConstants.DefaultPageSize)
                            .ToList();

                        foreach (var uiData in pageToCache)
                        {
                            _cache[uiData.AssetId] = uiData;
                        }

                        _uiPaginationIndex += pageToCache.Count;
                    }
                    else
                    {
                        return new List<TUI>();
                    }
                }
                finally
                {
                    if (_cacheLock.CurrentCount == 0)
                    {
                        _cacheLock.Release();
                    }
                }

                if (_cache.Count > 0 && _cache.Values.First() is BasicInventoryUiData)
                {
                    await LoadThumbnailsAsync(_assetsService);
                }

                return pageToCache.ToList();
            }
            finally
            {
                _isLoadingMore = false;
            }
        }


        public async UniTask<bool> HasDataForAssetId(string assetId)
        {
            await Initialize();

            await _cacheLock.WaitAsync();
            try
            {
                return _cache.ContainsKey(assetId);
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        public async UniTask<List<string>> GetAllAssetIds(List<string> categories = null, string subcategory = null, int? pageSize = null)
        {
            if (!_initialized)
            {
                await Initialize(categories, subcategory, pageSize);
            }

            await _cacheLock.WaitAsync();
            try
            {
                return _cache.Values
                    .Where(data => data != null &&
                                   (categories == null || categories.Contains(data.Category)) &&
                                   (string.IsNullOrEmpty(subcategory) || data.SubCategory == subcategory))
                    .Select(data => data.AssetId)
                    .Take(pageSize ?? _cache.Count)
                    .ToList();
            }
            finally
            {
                _cacheLock.Release();
            }
        }


        public async UniTask<TUI> GetDataForAssetId(string assetId)
        {
            await Initialize();

            await _cacheLock.WaitAsync();
            try
            {
                if (!_cache.TryGetValue(assetId, out var uiData))
                {
                    throw new KeyNotFoundException($"Asset with id {assetId} not found.");
                }

                return uiData;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        private async UniTask LoadThumbnailsAsync(IAssetsService assetsService)
        {
            List<BasicInventoryUiData> itemsNeedingThumbnails;

            await _cacheLock.WaitAsync();
            try
            {
                itemsNeedingThumbnails = _cache.Values
                    .OfType<BasicInventoryUiData>()
                    .Where(uiData => uiData is not SimpleColorUiData && !uiData.Thumbnail.IsAlive)
                    .ToList();
            }
            finally
            {
                _cacheLock.Release();
            }

            // Load thumbnails without holding the lock
            var tasks = itemsNeedingThumbnails
                .Select(uiData => LoadThumbnailForUiDataAsync(uiData, assetsService))
                .ToList();

            await UniTask.WhenAll(tasks);
        }

        private async UniTask LoadThumbnailForUiDataAsync(BasicInventoryUiData uiData, IAssetsService assetsService, int retry = 0)
        {
            if (string.IsNullOrEmpty(uiData.AssetId))
            {
                return;
            }

            var locations = await assetsService.LoadResourceLocationsAsync<Sprite>(uiData.AssetId);

            if (locations == null || locations.Count == 0)
            {
                if (retry < 2)
                {
                    await LoadThumbnailForUiDataAsync(uiData, assetsService, retry + 1);
                }

                return;
            }

            uiData.Thumbnail = await assetsService.LoadAssetAsync<Sprite>(locations[0]);
        }

        #region IInventoryUIDataProvider Explicit Implementation

        // Explicit interface implementations to avoid dynamic types (IL2CPP/iOS compatible)
        bool IUIProvider.HasMoreData => HasMoreData;
        bool IUIProvider.IsLoadingMore => IsLoadingMore;

        async UniTask<List<IAssetUiData>> IUIProvider.LoadMoreAsync(List<string> categories, string subcategory)
        {
            var result = await LoadMoreAsync(categories, subcategory);
            return result.Cast<IAssetUiData>().ToList();
        }

        UniTask<List<string>> IUIProvider.GetAllAssetIds(List<string> categories, string subcategory, int? pageSize)
        {
            return GetAllAssetIds(categories, subcategory, pageSize);
        }

        UniTask<List<string>> IUIProvider.GetAllAssetIds(List<string> categories, int? pageSize)
        {
            return GetAllAssetIds(categories, pageSize: pageSize);
        }

        async UniTask<IAssetUiData> IUIProvider.GetDataForAssetId(string assetId)
        {
            var result = await GetDataForAssetId(assetId);
            return result;
        }

        #endregion

        public void Dispose()
        {
            if (_cache?.Count > 0)
            {
                foreach (var data in _cache.Values)
                {
                    if (data is BasicInventoryUiData basicUiData)
                    {
                        basicUiData.Dispose();
                    }
                }

                _cache.Clear();
            }

            if (_allFetchedData?.Count > 0)
            {
                foreach (var data in _allFetchedData)
                {
                    if (data is BasicInventoryUiData basicUiData)
                    {
                        basicUiData.Dispose();
                    }
                }

                _allFetchedData.Clear();
            }
        }
    }
}
