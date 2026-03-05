using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Model;
using UnityEngine;

namespace Genies.Inventory
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IDefaultInventoryService
#else
    public interface IDefaultInventoryService
#endif
    {
        // Initial fetch methods with pagination support
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultWearables(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetUserWearables(int? limit = null, List<string> categories = null);

        /// <summary>
        /// Gets both user and default wearables. Applies the limit and categories filters to both separately
        /// </summary>
        /// <param name="limit">The amount of items to pull (this amount is doubled since its a separate limit on user and default assets)</param>
        /// <param name="categories">The categories to return</param>
        /// <returns>A list of assets with additional information about color tags</returns>
        public UniTask<List<ColorTaggedInventoryAsset>> GetAllWearables(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultAvatar(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultAvatarBaseAsset>> GetDefaultAvatarBaseData(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultAnimationLibraryAsset>> GetDefaultAnimationLibrary(int? limit = null, List<string> categories = null);
        public UniTask<List<ColoredInventoryAsset>> GetDefaultAvatarEyes(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarFlair(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultAvatarMakeup(int? limit = null, List<string> categories = null);
        public UniTask<List<ColoredInventoryAsset>> GetDefaultColorPresets(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultDecor(int? limit = null, List<string> categories = null);
        public UniTask<List<DefaultInventoryAsset>> GetDefaultImageLibrary(int? limit = null, List<string> categories = null);
        public UniTask<List<ColorTaggedInventoryAsset>> GetDefaultModelLibrary(int? limit = null, List<string> categories = null);

        // Load more methods for pagination
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreUserWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreAllWearables();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultAvatar();
        public UniTask<List<DefaultAvatarBaseAsset>> LoadMoreDefaultAvatarBaseData();
        public UniTask<List<DefaultAnimationLibraryAsset>> LoadMoreDefaultAnimationLibrary();
        public UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultAvatarEyes();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarFlair();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultAvatarMakeup();
        public UniTask<List<ColoredInventoryAsset>> LoadMoreDefaultColorPresets();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultDecor();
        public UniTask<List<DefaultInventoryAsset>> LoadMoreDefaultImageLibrary();
        public UniTask<List<ColorTaggedInventoryAsset>> LoadMoreDefaultModelLibrary();

        // Check if more data is available for pagination
        public string NextDefaultWearablesCursor();
        public string NextUserWearablesCursor();
        public string NextDefaultAvatarCursor();
        public string NextDefaultAvatarBaseCursor();
        public string NextDefaultAnimationLibraryCursor();
        public string NextDefaultAvatarEyesCursor();
        public string NextDefaultAvatarFlairCursor();
        public string NextDefaultAvatarMakeupCursor();
        public string NextDefaultColorPresetsCursor();
        public string NextDefaultDecorCursor();
        public string NextDefaultImageLibraryCursor();
        public string NextDefaultModelLibraryCursor();

        public UniTask<(bool, string)> GiveAssetToUserAsync(string assetId);

        public UniTask<List<CustomColorResponse>> GetCustomColors(string category = null);
        public UniTask<string> CreateCustomColor(List<Color> colors, CreateCustomColorRequest.CategoryEnum category);

        public UniTask UpdateCustomColor(string instanceId, List<Color> colors);
        public UniTask DeleteCustomColor(string instanceId, List<Color> colors);

        // Pipeline resolution for on-demand asset loading
        public UniTask<Dictionary<string, AssetPipelineInfo>> ResolvePipelineItemsAsync(List<string> assetIds);

        /// <summary>
        /// Clear all disk caches. Disk cache is currently only used for the default methods
        /// used by the avatar editor
        /// </summary>
        public void ClearDiskCache();

        public event Func<List<DefaultInventoryAsset>, UniTask> AssetsAddedAsync;
    }
}
