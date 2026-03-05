using System;
using System.Collections.Generic;
using System.Threading;
using CancellationToken = System.Threading.CancellationToken;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.Inventory;
using Genies.Naf;
using Genies.Refs;
using Genies.ServiceManagement;
using GnWrappers;
using UnityEngine;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Struct containing basic wearable asset information.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class WearableAssetInfo : IDisposable
#else
    public class WearableAssetInfo : IDisposable
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }
    }

    /// <summary>
    /// Contains tattoo asset information including thumbnail, for use with GetTattooAssetInfoListAsync.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TattooAssetInfo : IDisposable
#else
    public class TattooAssetInfo : IDisposable
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }
    }

    /// <summary>
    /// Wrapper for DefaultAvatarBaseAsset used by GetDefaultAvatarFeaturesByCategory and SetAvatarFeatureAsync.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarFeaturesInfo : IDisposable
#else
    public class AvatarFeaturesInfo : IDisposable
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> SubCategories { get; set; }
        public int Order { get; set; }
        public PipelineData PipelineData { get; set; }
        public List<string> Tags { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }

        /// <summary>
        /// Converts from the internal Inventory DefaultAvatarBaseAsset type.
        /// </summary>
        internal static AvatarFeaturesInfo FromInternal(DefaultAvatarBaseAsset internalAsset)
        {
            if (internalAsset == null)
            {
                return null;
            }

            return new AvatarFeaturesInfo
            {
                AssetId = internalAsset.AssetId,
                AssetType = internalAsset.AssetType,
                Name = internalAsset.Name,
                Category = internalAsset.Category,
                SubCategories = internalAsset.SubCategories,
                Order = internalAsset.Order,
                PipelineData = internalAsset.PipelineData,
                Tags = internalAsset.Tags
            };
        }

        /// <summary>
        /// Converts a list from internal DefaultAvatarBaseAsset to AvatarFeaturesInfo.
        /// </summary>
        internal static List<AvatarFeaturesInfo> FromInternalList(List<DefaultAvatarBaseAsset> internalList)
        {
            var result = new List<AvatarFeaturesInfo>();
            if (internalList == null)
            {
                return result;
            }

            foreach (var item in internalList)
            {
                var info = FromInternal(item);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts this wrapper to the internal Inventory DefaultAvatarBaseAsset type.
        /// </summary>
        internal static DefaultAvatarBaseAsset ToInternal(AvatarFeaturesInfo info)
        {
            if (info == null)
            {
                return null;
            }

            return new DefaultAvatarBaseAsset
            {
                AssetId = info.AssetId,
                AssetType = info.AssetType,
                Name = info.Name,
                Category = info.Category,
                SubCategories = info.SubCategories,
                Order = info.Order,
                PipelineData = info.PipelineData,
                Tags = info.Tags
            };
        }
    }

    /// <summary>
    /// Wrapper for DefaultInventoryAsset used by GetDefaultMakeupByCategoryAsync, GetDefaultTattoosAsync, EquipMakeupAsync, and UnEquipMakeupAsync.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarMakeupInfo
#else
    public class AvatarMakeupInfo
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> SubCategories { get; set; }
        public int Order { get; set; }
        public PipelineData PipelineData { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }

        /// <summary>
        /// Converts from the internal Inventory DefaultInventoryAsset type.
        /// </summary>
        internal static AvatarMakeupInfo FromInternal(DefaultInventoryAsset internalAsset)
        {
            if (internalAsset == null)
            {
                return null;
            }

            return new AvatarMakeupInfo
            {
                AssetId = internalAsset.AssetId,
                AssetType = internalAsset.AssetType,
                Name = internalAsset.Name,
                Category = internalAsset.Category,
                SubCategories = internalAsset.SubCategories,
                Order = internalAsset.Order,
                PipelineData = internalAsset.PipelineData
            };
        }

        /// <summary>
        /// Converts a list from internal DefaultInventoryAsset to AvatarItemInfo.
        /// </summary>
        internal static List<AvatarMakeupInfo> FromInternalList(List<DefaultInventoryAsset> internalList)
        {
            var result = new List<AvatarMakeupInfo>();
            if (internalList == null)
            {
                return result;
            }

            foreach (var item in internalList)
            {
                var info = FromInternal(item);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts this wrapper to the internal Inventory DefaultInventoryAsset type.
        /// </summary>
        internal static DefaultInventoryAsset ToInternal(AvatarMakeupInfo info)
        {
            if (info == null)
            {
                return null;
            }

            return new DefaultInventoryAsset
            {
                AssetId = info.AssetId,
                AssetType = info.AssetType,
                Name = info.Name,
                Category = info.Category,
                SubCategories = info.SubCategories,
                Order = info.Order,
                PipelineData = info.PipelineData
            };
        }
    }

    /// <summary>
    /// Wrapper for DefaultInventoryAsset used by GetDefaultMakeupByCategoryAsync, GetDefaultTattoosAsync, EquipMakeupAsync, and UnEquipMakeupAsync.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarTattooInfo : IDisposable
#else
    public class AvatarTattooInfo : IDisposable
#endif
    {
        public string AssetId { get; set; }
        public AssetType AssetType { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> SubCategories { get; set; }
        public int Order { get; set; }
        public PipelineData PipelineData { get; set; }
        public Ref<Sprite> Icon { get; set; }

        public void Dispose()
        {
            ServiceManager.Get<IAvatarEditorSdkService>()?.RemoveSpriteReference(Icon);
        }

        /// <summary>
        /// Converts from the internal Inventory DefaultInventoryAsset type.
        /// </summary>
        internal static AvatarTattooInfo FromInternal(DefaultInventoryAsset internalAsset)
        {
            if (internalAsset == null)
            {
                return null;
            }

            return new AvatarTattooInfo
            {
                AssetId = internalAsset.AssetId,
                AssetType = internalAsset.AssetType,
                Name = internalAsset.Name,
                Category = internalAsset.Category,
                SubCategories = internalAsset.SubCategories,
                Order = internalAsset.Order,
                PipelineData = internalAsset.PipelineData
            };
        }

        /// <summary>
        /// Converts a list from internal DefaultInventoryAsset to AvatarItemInfo.
        /// </summary>
        internal static List<AvatarTattooInfo> FromInternalList(List<DefaultInventoryAsset> internalList)
        {
            var result = new List<AvatarTattooInfo>();
            if (internalList == null)
            {
                return result;
            }

            foreach (var item in internalList)
            {
                var info = FromInternal(item);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts a list from internal DefaultInventoryAsset to AvatarItemInfo.
        /// </summary>
        internal static List<AvatarTattooInfo> FromService(List<DefaultInventoryAsset> internalList)
        {
            var result = new List<AvatarTattooInfo>();
            if (internalList == null)
            {
                return result;
            }
            foreach (var item in internalList)
            {
                var info = FromInternal(item);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts from TattooAssetInfo (preserves AssetId, AssetType, Name, Category, Icon). SubCategories, Order, PipelineData are left null/0.
        /// </summary>
        internal static AvatarTattooInfo From(TattooAssetInfo info)
        {
            if (info == null)
            {
                return null;
            }

            return new AvatarTattooInfo
            {
                AssetId = info.AssetId,
                AssetType = info.AssetType,
                Name = info.Name,
                Category = info.Category,
                SubCategories = null,
                Order = 0,
                PipelineData = null,
                Icon = info.Icon
            };
        }

        /// <summary>
        /// Converts a list of TattooAssetInfo to AvatarTattooInfo without losing any attribute (Icon is preserved).
        /// </summary>
        internal static List<AvatarTattooInfo> FromTattooAssetInfoList(List<TattooAssetInfo> list)
        {
            var result = new List<AvatarTattooInfo>();
            if (list == null)
            {
                return result;
            }

            foreach (var item in list)
            {
                var info = From(item);
                if (info != null)
                {
                    result.Add(info);
                }
            }
            return result;
        }

        /// <summary>
        /// Converts this wrapper to the internal Inventory DefaultInventoryAsset type.
        /// </summary>
        internal static DefaultInventoryAsset ToInternal(AvatarTattooInfo info)
        {
            if (info == null)
            {
                return null;
            }

            return new DefaultInventoryAsset
            {
                AssetId = info.AssetId,
                AssetType = info.AssetType,
                Name = info.Name,
                Category = info.Category,
                SubCategories = info.SubCategories,
                Order = info.Order,
                PipelineData = info.PipelineData
            };
        }
    }

    /// <summary>
    /// Core service interface for managing the Avatar Editor.
    /// Provides methods for opening and closing the avatar editor with customizable properties.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAvatarEditorSdkService
#else
    public interface IAvatarEditorSdkService
#endif
    {
        /// <summary>
        /// Opens the avatar editor with the specified avatar and camera.
        /// </summary>
        /// <param name="avatar">The avatar to edit. If null, loads the current user's avatar.</param>
        /// <param name="camera">The camera to use for the editor. If null, uses Camera.main.</param>
        /// <returns>A UniTask that completes when the editor is opened.</returns>
        public UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null);

        /// <summary>
        /// Closes the avatar editor and cleans up resources.
        /// </summary>
        /// <returns>A UniTask that completes when the editor is closed.</returns>
        /// <param name="revertAvatar">Whether the avatar should be reverted to its pre-edited version.</param>
        public UniTask CloseEditorAsync(bool revertAvatar);

        /// <summary>
        /// Gets the currently active avatar being edited in the editor.
        /// </summary>
        /// <returns>The currently active GeniesAvatar, or null if no avatar is currently being edited</returns>
        public GeniesAvatar GetCurrentActiveAvatar();

        /// <summary>
        /// Gets whether the editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public bool IsEditorOpen { get; }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public event Action EditorOpened;

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public event Action EditorClosed;

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of wearable asset information filtered by categories from the default inventory service.
        /// </summary>
        /// <param name="categories">List of WardrobeSubcategory enum values to filter by (e.g., WardrobeSubcategory.hoodie, WardrobeSubcategory.hair, etc.). If null or empty, returns all wearables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public UniTask<List<WearableAssetInfo>> GetDefaultWearableAssetsListByCategoriesAsync(List<WardrobeSubcategory> categories, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of user wearable asset information filtered by categories from the inventory service.
        /// </summary>
        /// <param name="categories">List of WardrobeSubcategory enum values to filter by (e.g., WardrobeSubcategory.hoodie, WardrobeSubcategory.hair, etc.). If null or empty, returns all user wearables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public UniTask<List<WearableAssetInfo>> GetUserWearableAssetsListByCategoriesAsync(List<WardrobeSubcategory> categories = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets default avatar makeup assets filtered by category from the default inventory service.
        /// </summary>
        /// <param name="categories">List of MakeupCategory (e.g. Stickers, Lipstick, Freckles, FaceGems, Eyeshadow, Blush). If null or empty, returns all default makeup.</param>
        /// <param name="limit">Optional limit on the number of items to return.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of DefaultInventoryAsset for the requested makeup categories.</returns>
        public UniTask<List<DefaultInventoryAsset>> GetDefaultMakeupByCategoryAsync(List<MakeupCategory> categories, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of makeup asset information (including thumbnails) for the given categories using DefaultAvatarMakeupConfig.
        /// </summary>
        /// <param name="categories">List of MakeupCategory to filter by. If null or empty, returns all default makeup.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarMakeupInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public UniTask<List<AvatarMakeupInfo>> GetMakeupAssetInfoListByCategoryAsync(List<MakeupCategory> categories, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets default avatar features data filtered by category from the default inventory service.
        /// </summary>
        /// <param name="category">The AvatarBaseCategory to filter by (e.g., Eyes, Jaw, Lips, Nose, Brow). None returns all avatar base data.</param>
        /// <param name="limit">Optional limit for pagination. If null, uses default limit.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing asset information, filtered by category if not None.</returns>
        public UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(AvatarBaseCategory category, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets default avatar features data filtered by category string. Use when calling from assemblies that cannot reference AvatarBaseCategory (e.g. SDK).
        /// </summary>
        /// <param name="categoryFilter">Category name to filter by (e.g. "Eyes", "Jaw", "Lips", "Nose", "Brow"), or null for all.</param>
        /// <param name="limit">Optional limit for pagination. If null, uses default limit.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing asset information.</returns>
        public UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(string categoryFilter, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of avatar feature asset information (including thumbnails) for the given category using DefaultAvatarBaseConfig.
        /// </summary>
        /// <param name="category">The AvatarBaseCategory to filter by (e.g., Eyes, Jaw, Lips, Nose, Brow). None returns all avatar base data.</param>
        /// <param name="limit">Optional limit for pagination. If null, uses default limit.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public UniTask<List<AvatarFeaturesInfo>> GetAvatarFeatureAssetInfoListByCategoryAsync(AvatarBaseCategory category, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of avatar feature asset information (including thumbnails) for the given category string using DefaultAvatarBaseConfig.
        /// </summary>
        /// <param name="categoryFilter">Category name to filter by (e.g. "Eyes", "Jaw", "Lips", "Nose", "Brow"), or null for all.</param>
        /// <param name="limit">Optional limit for pagination. If null, uses default limit.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public UniTask<List<AvatarFeaturesInfo>> GetAvatarFeatureAssetInfoListByCategoryAsync(string categoryFilter, int? limit = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a simplified list of tattoo asset information (including thumbnails) from the default image library (tattoo category) using DefaultImageLibraryConfig.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of TattooAssetInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public UniTask<List<TattooAssetInfo>> GetDefaultTattooAssetInfoListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets default (curated) color presets for the specified color type.
        /// </summary>
        /// <param name="colorType">The type of color to retrieve (Hair, FacialHair, Skin, Eyebrow, Eyelash, or Makeup).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IColor containing asset IDs and color values (default presets only).</returns>
        public UniTask<List<IColor>> GetDefaultColorsAsync(ColorType colorType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets user (custom) color presets for the specified color type. Only Hair, Eyebrow, and Eyelash support user colors.
        /// </summary>
        /// <param name="colorType">The type of user color to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IColor containing user-defined color presets.</returns>
        public UniTask<List<IColor>> GetUserColorsAsync(UserColorType colorType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current color from the avatar for the specified color kind (Hair, FacialHair, EyeBrows, EyeLash, Skin, Eyes, or Makeup).
        /// Returns an IColor instance of the corresponding type.
        /// </summary>
        /// <param name="avatar">The avatar to read the color from.</param>
        /// <param name="colorKind">Which IColor type to return.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask that completes with the corresponding IColor value, or null if avatar is null.</returns>
        public UniTask<IColor> GetColorAsync(GeniesAvatar avatar, AvatarColorKind colorKind, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips an avatar makeup asset on the specified avatar by running EquipNativeAvatarAssetCommand with the asset's AssetId.
        /// </summary>
        /// <param name="avatar">The avatar to equip the makeup on.</param>
        /// <param name="asset">The default inventory asset (e.g. makeup) to equip; its AssetId is passed to the command.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask representing the async operation.</returns>
        public UniTask EquipMakeupAsync(GeniesAvatar avatar, DefaultInventoryAsset asset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips an avatar makeup asset on the specified avatar by asset ID (e.g. from MakeupAssetInfo.AssetId).
        /// </summary>
        /// <param name="avatar">The avatar to equip the makeup on.</param>
        /// <param name="makeupAssetId">The ID of the makeup asset to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask representing the async operation.</returns>
        public UniTask EquipMakeupAsync(GeniesAvatar avatar, string makeupAssetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips a makeup asset from the specified avatar by asset ID (same pattern as UnEquipOutfitAsync).
        /// </summary>
        /// <param name="avatar">The avatar to unequip the makeup from.</param>
        /// <param name="makeupAssetId">The ID of the makeup asset to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask representing the async operation.</returns>
        public UniTask UnEquipMakeupAsync(GeniesAvatar avatar, string makeupAssetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies makeup colors for the given makeup category (e.g. Lipstick, Blush) by running EquipMakeupColorCommand.
        /// </summary>
        /// <param name="avatar">The avatar to apply the makeup colors on.</param>
        /// <param name="category">The makeup category (e.g. Stickers, Lipstick, Freckles, FaceGems, Eyeshadow, Blush).</param>
        /// <param name="colors">The colors to apply for that category.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask representing the async operation.</returns>
        public UniTask SetMakeupColorAsync(GeniesAvatar avatar, MakeupCategory category, Color[] colors, CancellationToken cancellationToken = default);

        /// <summary>
        /// Grants an asset to a user, adding it to their inventory
        /// </summary>
        /// <param name="assetId">Id of the asset</param>
        /// <returns>UniTask representing the async operation with a bool indicating success status
        /// and string for any failure reason</returns>
        public UniTask<(bool, string)> GiveAssetToUserAsync(string assetId);

        /// <summary>
        /// Equips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask EquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the asset from</param>
        /// <param name="wearableId">The ID of the wearable to unequip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask UnEquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips a skin color on the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to equip the skin color on</param>
        /// <param name="skinColor">The color to apply as skin color</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetSkinColorAsync(GeniesAvatar avatar, Color skinColor, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets hair colors on the specified avatar. Hair colors consist of four components: base, R, G, and B.
        /// </summary>
        /// <param name="avatar">The avatar to set the hair colors on</param>
        /// <param name="hairType">The type of hair to modify (Hair or FacialHair)</param>
        /// <param name="baseColor">The base hair color</param>
        /// <param name="colorR">The red component of the hair color gradient</param>
        /// <param name="colorG">The green component of the hair color gradient</param>
        /// <param name="colorB">The blue component of the hair color gradient</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask ModifyAvatarHairColorAsync(GeniesAvatar avatar, HairType hairType, Color baseColor, Color colorR, Color colorG, Color colorB, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets flair colors (eyebrows or eyelashes) on the specified avatar. Flair colors consist of four components: base, R, G, and B.
        /// </summary>
        /// <param name="avatar">The avatar to set the flair colors on</param>
        /// <param name="hairType">The type of flair to modify (HairType.Eyebrows or HairType.Eyelashes)</param>
        /// <param name="colors">The colors</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask ModifyAvatarFlairColorAsync(GeniesAvatar avatar, HairType hairType, Color[] colors, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips a hair style on the specified avatar. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The avatar to equip the hair style on</param>
        /// <param name="hairAssetId">The ID of the hair asset to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask EquipHairAsync(GeniesAvatar avatar, string hairAssetId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips a hair style from the specified avatar.
        /// Automatically finds the currently equipped hair asset and unequips it. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the hair style from</param>
        /// <param name="hairType">The type of hair to unequip (Hair or FacialHair)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask UnEquipHairAsync(GeniesAvatar avatar, HairType hairType, CancellationToken cancellationToken = default);

        /// <summary>
        /// Equips a tattoo on the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to equip the tattoo on</param>
        /// <param name="tattooId">The ID of the tattoo to equip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo should be placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask EquipTattooAsync(GeniesAvatar avatar, AvatarTattooInfo tattooId, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Unequips a tattoo from the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the tattoo from</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask that completes with the tattoo ID that was unequipped, or null if none was equipped or on error.</returns>
        public UniTask<string> UnEquipTattooAsync(GeniesAvatar avatar, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the body preset for the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to set the body preset on</param>
        /// <param name="preset">The GSkelModifierPreset to apply</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, GSkelModifierPreset preset, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the current native avatar body preset from the specified avatar's controller.
        /// </summary>
        /// <param name="avatar">The avatar to get the body preset from</param>
        /// <returns>The current GSkelModifierPreset, or null if avatar/controller is null</returns>
        public GSkelModifierPreset GetNativeAvatarBodyPreset(GeniesAvatar avatar);

        /// <summary>
        /// Gets current values for a given avatar feature stat type (e.g. all nose stats, all body stats).
        /// </summary>
        /// <param name="avatar">The avatar to read from.</param>
        /// <param name="statType">Which stat category to return (Body, EyeBrows, Eyes, Jaw, Lips, Nose).</param>
        /// <returns>Dictionary of AvatarFeatureStat to current value (typically -1.0 to 1.0). Empty if avatar/controller is null.</returns>
        public Dictionary<AvatarFeatureStat, float> GetAvatarFeatureStats(GeniesAvatar avatar, AvatarFeatureStatType statType);

        /// <summary>
        /// Modifies a single avatar feature stat. Value is clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The avatar to modify.</param>
        /// <param name="stat">The feature stat to set (e.g. AvatarFeatureStat.Nose_Width, AvatarFeatureStat.Body_NeckThickness).</param>
        /// <param name="value">The value to set (clamped between -1.0 and 1.0).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the update was applied, false if avatar/controller invalid or an error occurred.</returns>
        public bool ModifyAvatarFeatureStat(GeniesAvatar avatar, AvatarFeatureStat stat, float value, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The avatar to set the body type on</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous)</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default);

        /// <summary>
        /// Saves the current avatar definition to the cloud.
        /// </summary>
        /// <returns>A UniTask that completes when the save operation is finished.</returns>
        public UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar);

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The avatar to save</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public void SaveAvatarDefinitionLocally(GeniesAvatar avatar, string profileId = null);

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a JSON definition string from Device cache.
        /// (See <see cref="SaveAvatarDefinitionLocally"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile to load</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a ScriptableObject stored in local project resources.
        /// (See <see cref="SaveAvatarDefinitionLocally"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile to load</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Uploads an avatar image for the specified avatar.
        /// </summary>
        /// <param name="imageData">The image data as a byte array.</param>
        /// <param name="avatarId">The ID of the avatar to upload the image for.</param>
        /// <returns>A task that completes with the URL of the uploaded image.</returns>
        public UniTask UploadAvatarImageAsync(byte[] imageData, string avatarId);

        /// <summary>
        /// Sets the save option for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption);

        /// <summary>
        /// Sets the save option and profile ID for the avatar editor.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption, string profileId);

        /// <summary>
        /// Sets the save settings for the avatar editor.
        /// Settings persist across multiple editor sessions within the same play session.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetEditorSaveSettings(AvatarSaveSettings saveSettings);

        /// <summary>
        /// Remove a sprite from an internal managed cache so it can be garbage collected
        /// </summary>
        /// <param name="spriteRef">The ref to the sprite</param>
        public void RemoveSpriteReference(Ref<Sprite> spriteRef);

        /// <summary>
        /// Sets the Save and Exit ActionBarFlags on all BaseCustomizationControllers in the InventoryNavigationGraph.
        /// Excludes CustomHairColor_Controller, CustomEyelashColor_Controller, and CustomEyebrowColor_Controller
        /// (which always need it to exit their custom color editing screen)
        /// </summary>
        /// <param name="enableSaveButton">True to enable the save button, false to disable</param>
        /// <param name="enableExitButton">True to enable the exit button, false to disable</param>
        public void SetSaveAndExitButtonStatus(bool enableSaveButton, bool enableExitButton);

    }
}
