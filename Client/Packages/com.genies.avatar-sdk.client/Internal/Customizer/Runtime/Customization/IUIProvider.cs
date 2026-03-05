using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Customization.Framework
{
    // Base interface for provider class of UI data
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IUIProvider : IDisposable
#else
    public interface IUIProvider : IDisposable
#endif
    {
        bool HasMoreData { get; }
        bool IsLoadingMore { get; }

        UniTask<List<IAssetUiData>> LoadMoreAsync(List<string> categories = null, string subcategory = null);
        UniTask<List<string>> GetAllAssetIds(List<string> categories = null, string subcategory = null, int? pageSize = null);
        UniTask<List<string>> GetAllAssetIds(List<string> categories, int? pageSize = null);
        UniTask<IAssetUiData> GetDataForAssetId(string assetId);
    }

    // Base interface for all UI data
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAssetUiData
#else
    public interface IAssetUiData
#endif
    {
        public string AssetId { get; }
        public string DisplayName { get; }
        public string Category { get; }
        public string SubCategory { get; }
        public int Order { get; }
    }
}
