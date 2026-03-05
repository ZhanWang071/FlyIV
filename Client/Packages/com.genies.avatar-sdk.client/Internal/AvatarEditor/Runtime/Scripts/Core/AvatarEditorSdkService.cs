using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using CancellationToken = System.Threading.CancellationToken;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Avatars;
using Genies.Avatars.Sdk;
using Genies.Avatars.Services;
using Genies.CrashReporting;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Actions;
using Genies.Customization.Framework.Navigation;
using Genies.Customization.MegaEditor;
using Genies.Inventory;
using Genies.Inventory.UIData;
using Genies.Login.Native;
using Genies.Looks.Customization.Commands;
using Genies.Naf;
using Genies.Naf.Content;
using Genies.Refs;
using GnWrappers;
using Genies.ServiceManagement;
using Genies.Utilities;
using Genies.Ugc;
using Genies.Ugc.CustomHair;
using Genies.VirtualCamera;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Genies.AvatarEditor.Core
{
    /// <summary>
    /// Implementation of IAvatarEditorSdkService providing avatar editor functionality.
    /// Handles opening and closing the editor with proper initialization and cleanup.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarEditorSdkService : IAvatarEditorSdkService, IDisposable
#else
    public class AvatarEditorSdkService : IAvatarEditorSdkService, IDisposable
#endif
    {
        private const string _avatarEditorPath = "Prefabs/AvatarEditor";
        private static GameObject _avatarEditorPrefab, _avatarEditorInstance;
        private static Camera _currentCamera;
        private static UniTaskCompletionSource _editorOpenedSource, _editorClosedSource;
        private readonly object _editorOpenedLock = new(), _editorClosedLock = new();
        private const string _headTransformPath = "Root/Hips/Spine/Spine1/Spine2/Neck/Head";
        private GeniesAvatar _currentActiveAvatar;
        private AvatarSaveSettings? _pendingSaveSettings;

        // Static persistent save settings that survive across editor sessions
        private static AvatarSaveSettings _persistentSaveSettings = new(AvatarSaveOption.SaveRemotelyAndExit);
        private static bool _hasInitializedPersistentSettings = false;

        // Pending Save and Exit flag setting
        private bool? _pendingSaveButtonSetting = null, _pendingExitButtonSetting = null;

        private readonly HashSet<Ref<Sprite>> _spritesGivenToUser = new();

        /// <summary>
        /// Gets the current persistent save settings, initializing with defaults if needed.
        /// These settings persist within the same play session due to static variable behavior.
        /// </summary>
        private static AvatarSaveSettings GetPersistentSaveSettings()
        {
            if (!_hasInitializedPersistentSettings)
            {
                _persistentSaveSettings = new AvatarSaveSettings(AvatarSaveOption.SaveRemotelyAndContinue);
                _hasInitializedPersistentSettings = true;
            }

            return _persistentSaveSettings;
        }

        /// <summary>
        /// Opens the avatar editor with the specified avatar and camera.
        /// If camera is null, attempts to get the camera with tag 'MainCamera' (Camera.main).
        /// </summary>
        public async UniTask OpenEditorAsync(GeniesAvatar avatar, Camera camera = null)
        {
            if (!GeniesLoginSdk.IsUserSignedIn())
            {
                CrashReporter.LogError("You need to be logged in to initialize the avatar editor");
                return;
            }

            if (_editorOpenedSource != null)
            {
                await _editorOpenedSource.Task;
                return;
            }

            lock (_editorOpenedLock)
            {
                if (_editorOpenedSource == null)
                {
                    _editorOpenedSource = new UniTaskCompletionSource();
                }
            }

            try
            {
                if (avatar == null)
                {
                    throw new NullReferenceException("An avatar is required in order to open the editor.");
                }

                if (camera == null)
                {
                    camera = Camera.main;
                    if (camera == null)
                    {
                        throw new NullReferenceException("A valid camera must be passed or a camera with tag 'MainCamera' must exist in the scene.");
                    }
                }

                PreloadSpecificAssetData();

                if (_avatarEditorInstance != null)
                {
                    if (!_avatarEditorInstance.activeInHierarchy)
                    {
                        _avatarEditorInstance.SetActive(true);
                    }

                    _currentActiveAvatar = avatar;
                    await InitializeEditing(avatar, camera);
                    EditorOpened?.Invoke();
                    return;
                }

                if (_avatarEditorPrefab == null)
                {
                    _avatarEditorPrefab = Resources.Load<GameObject>(_avatarEditorPath);
                }

                if (_avatarEditorPrefab == null)
                {
                    CrashReporter.LogError($"AvatarEditor prefab not found at path: {_avatarEditorPath}");
                    return;
                }

                _currentActiveAvatar = avatar;
                _avatarEditorInstance = Object.Instantiate(_avatarEditorPrefab);

                await InitializeEditing(avatar, camera);

                EditorOpened?.Invoke();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to open avatar editor: {ex.Message}");
            }
            finally
            {
                FinishEditorOpenedSource();
            }
        }


        /// <summary>
        /// Opens the avatar editor with the specified camera and loads the current user's avatar.
        /// </summary>
        public async UniTask OpenEditorAsync(Camera camera)
        {
            await OpenEditorAsync(null, camera);
        }

        /// <summary>
        /// Closes the avatar editor and cleans up resources.
        /// </summary>
        public async UniTask CloseEditorAsync(bool revertAvatar)
        {
            _currentActiveAvatar = null;

            if (_editorClosedSource != null)
            {
                await _editorClosedSource.Task;
                return;
            }

            lock (_editorClosedLock)
            {
                if (_editorClosedSource == null)
                {
                    _editorClosedSource = new UniTaskCompletionSource();
                }
            }

            if (_avatarEditorInstance == null)
            {
                return;
            }

            try
            {
                // - Return avatar definition to what it was before editing
                // - Return camera to previous position before editing
                var avatarEditingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
                if (avatarEditingScreen != null && avatarEditingScreen.EditingBehaviour is not null)
                {
                    await avatarEditingScreen.EditingBehaviour.EndEditing(revertAvatar);
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogWarning($"Exception caught while trying to clean up the avatar editor instance. This is usually ok since we're destroying the entire object instance anyway.\nException: {ex.Message}");
            }
            finally
            {
                if (_avatarEditorInstance != null)
                {
                    GameObject.Destroy(_avatarEditorInstance);
                    _avatarEditorInstance = null;
                }

                EditorClosed?.Invoke();
                FinishEditorClosedSource();

                // Clear any pending save settings when editor is closed (but keep persistent settings)
                _pendingSaveSettings = null;
            }
        }

        public void Dispose()
        {
            _ = CloseEditorAsync(true);
        }

        /// <summary>
        /// Gets the currently active avatar being edited in the editor.
        /// </summary>
        /// <returns>The currently active GeniesAvatar, or null if no avatar is currently being edited</returns>
        public GeniesAvatar GetCurrentActiveAvatar()
        {
            return _currentActiveAvatar;
        }

        public async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            try
            {
                if (string.IsNullOrEmpty(assetId))
                {
                    string error = "Asset id cannot be null or empty";
                    CrashReporter.LogError(error);
                    return (false, error);
                }

                if (!GeniesLoginSdk.IsUserSignedIn())
                {
                    string error = "You need to be logged in to give an asset to a user";
                    CrashReporter.LogError(error);
                    return (false, error);
                }

                var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
                return await defaultInventoryService.GiveAssetToUserAsync(assetId);
            }
            catch (Exception ex)
            {
                string error = $"Failed to give asset to user: {ex.Message}";
                CrashReporter.LogError(error);
                return (false, error);
            }
        }

        /// <summary>
        /// Gets a simplified list of user wearable asset information filtered by categories from the inventory service.
        /// </summary>
        /// <param name="categories">List of WardrobeSubcategory enum values to filter by (e.g., WardrobeSubcategory.hoodie, WardrobeSubcategory.hair, etc.). If null or empty, returns all user wearables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public async UniTask<List<WearableAssetInfo>> GetUserWearableAssetsListByCategoriesAsync(List<WardrobeSubcategory> categories = null, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!GeniesLoginSdk.IsUserSignedIn())
                {
                    CrashReporter.LogError("You need to be logged in to get a user's assets");
                    return new();
                }

                // Convert enum categories to strings for the inventory service
                List<string> categoryStrings = null;

                if (categories != null && categories.Count > 0)
                {
                    categoryStrings = categories
                        .Where(c => c != WardrobeSubcategory.none && c != WardrobeSubcategory.all)
                        .Select(c => c.ToString().ToLower())
                        .ToList();
                }

                var defaultInventoryService = ServiceManager.Get<IDefaultInventoryService>();
                var wearables = await defaultInventoryService.GetUserWearables(categories: categoryStrings);

                if (wearables == null || !wearables.Any())
                {
                    CrashReporter.LogWarning($"No user wearables found in inventory service for categories: {(categoryStrings != null && categoryStrings.Count > 0 ? string.Join(", ", categoryStrings) : "all")}");
                    return new List<WearableAssetInfo>();
                }

                var provider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.UserWearablesConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                var wearableAssetInfoList = await UniTask.WhenAll(
                    wearables.Select(async wearable =>
                    {
                        var data = await provider.GetDataForAssetId(wearable.AssetId);

                        var info = new WearableAssetInfo
                        {
                            AssetId = wearable.AssetId,
                            AssetType = wearable.AssetType,
                            Name = wearable.Name,
                            Category = wearable.Category,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return wearableAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get user wearable asset info list by categories: {ex.Message}");
                return new();
            }
        }

        /// <summary>
        /// Gets whether the editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public bool IsEditorOpen
        {
            get => _avatarEditorInstance != null && _avatarEditorInstance.activeInHierarchy;
        }

        /// <summary>
        /// Event raised when the editor is opened.
        /// </summary>
        public event Action EditorOpened;

        /// <summary>
        /// Event raised when the editor is closed.
        /// </summary>
        public event Action EditorClosed;

        /// <summary>
        /// Equips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to equip the asset on</param>
        /// <param name="wearableId">The ID of the wearable to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask EquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If avatar is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to equip an asset.");
                    return;
                }

                // If controller is already equipped with the asset, return
                if (avatar.Controller.IsAssetEquipped(wearableId))
                {
                    CrashReporter.LogWarning("Asset is already equipped.");
                    return;
                }

                // Create and execute the equip command
                var command = new EquipNativeAvatarAssetCommand(wearableId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip outfit with ID '{wearableId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Equips an avatar makeup asset on the specified avatar.
        /// </summary>
        public async UniTask EquipMakeupAsync(GeniesAvatar avatar, DefaultInventoryAsset asset, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set avatar makeup.");
                    return;
                }

                if (asset == null || string.IsNullOrEmpty(asset.AssetId))
                {
                    CrashReporter.LogError("A valid asset with a non-empty AssetId is required to set avatar makeup.");
                    return;
                }

                var command = new EquipNativeAvatarAssetCommand(asset.AssetId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar makeup: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips an avatar makeup asset on the specified avatar by asset ID (e.g. from MakeupAssetInfo.AssetId).
        /// </summary>
        public async UniTask EquipMakeupAsync(GeniesAvatar avatar, string makeupAssetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set avatar makeup.");
                    return;
                }

                if (string.IsNullOrEmpty(makeupAssetId))
                {
                    CrashReporter.LogError("A non-empty makeup asset ID is required to set avatar makeup.");
                    return;
                }

                var command = new EquipNativeAvatarAssetCommand(makeupAssetId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar makeup: {ex.Message}");
            }
        }

        /// <summary>
        /// Set makeup colors for the given makeup category (e.g. Lipstick, Blush) by running EquipMakeupColorCommand.
        /// </summary>
        public async UniTask SetMakeupColorAsync(GeniesAvatar avatar, MakeupCategory category, Color[] colors, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required to modify makeup color.");
                    return;
                }

                if (avatar.Controller == null)
                {
                    CrashReporter.LogError("Avatar controller is null; cannot modify makeup color.");
                    return;
                }

                var presetCategoryInt = MakeupCategoryMapper.ToMakeupPresetCategoryInt(category);
                var command = new EquipMakeupColorCommand(presetCategoryInt, colors ?? Array.Empty<Color>(), avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to modify makeup color: {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a makeup asset from the specified avatar by asset ID using UnequipNativeAvatarAssetCommand.
        /// </summary>
        public async UniTask UnEquipMakeupAsync(GeniesAvatar avatar, string makeupAssetId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip makeup.");
                    return;
                }

                if (string.IsNullOrEmpty(makeupAssetId))
                {
                    CrashReporter.LogError("A non-empty makeup asset ID is required to unequip makeup.");
                    return;
                }

                if (!avatar.Controller.IsAssetEquipped(makeupAssetId))
                {
                    CrashReporter.LogError("Makeup asset is not equipped.");
                    return;
                }

                var command = new UnequipNativeAvatarAssetCommand(makeupAssetId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip makeup with ID '{makeupAssetId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips an outfit by wearable ID using the default inventory service.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the asset from</param>
        /// <param name="wearableId">The ID of the wearable to unequip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask UnEquipOutfitAsync(GeniesAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip an asset.");
                    return;
                }

                // If controller is not equipped with the asset, return
                if (!avatar.Controller.IsAssetEquipped(wearableId))
                {
                    CrashReporter.LogError("Asset is already not equipped.");
                    return;
                }

                // Create and execute the unequip command
                var command = new UnequipNativeAvatarAssetCommand(wearableId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip outfit with ID '{wearableId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a skin color on the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to equip the skin color on</param>
        /// <param name="skinColor">The color to apply as skin color</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetSkinColorAsync(GeniesAvatar avatar, Color skinColor, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a skin color.");
                    return;
                }

                // Create and execute the equip skin color command
                var command = new EquipSkinColorCommand(skinColor, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip skin color: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets hair colors on the specified avatar.
        /// </summary>
        /// <param name="avatar">The avatar to set the hair colors on</param>
        /// <param name="baseColor">The base hair color</param>
        /// <param name="colorR">The red component of the hair color gradient</param>
        /// <param name="colorG">The green component of the hair color gradient</param>
        /// <param name="colorB">The blue component of the hair color gradient</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask ModifyAvatarHairColorAsync(GeniesAvatar avatar, HairType hairType, Color baseColor, Color colorR, Color colorG, Color colorB, CancellationToken cancellationToken = default)
        {
            try
            {
                if (hairType == HairType.Eyebrows || hairType == HairType.Eyelashes)
                {
                    var colors = new Color[] { baseColor, colorR };
                    await ModifyAvatarFlairColorAsync(avatar, hairType, colors, cancellationToken);
                    return;
                }

                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set hair colors.");
                    return;
                }

                // Create GenieColorEntry array for all four hair color components based on hair type
                GenieColorEntry[] hairColors;
                if (hairType == HairType.FacialHair)
                {
                    hairColors = new GenieColorEntry[]
                    {
                        new GenieColorEntry { ColorId = GenieColor.FacialhairBase, Value = baseColor },
                        new GenieColorEntry { ColorId = GenieColor.FacialhairR, Value = colorR },
                        new GenieColorEntry { ColorId = GenieColor.FacialhairG, Value = colorG },
                        new GenieColorEntry { ColorId = GenieColor.FacialhairB, Value = colorB }
                    };
                }
                else
                {
                    hairColors = new GenieColorEntry[]
                    {
                        new GenieColorEntry { ColorId = GenieColor.HairBase, Value = baseColor },
                        new GenieColorEntry { ColorId = GenieColor.HairR, Value = colorR },
                        new GenieColorEntry { ColorId = GenieColor.HairG, Value = colorG },
                        new GenieColorEntry { ColorId = GenieColor.HairB, Value = colorB }
                    };
                }

                // Create and execute the set hair colors command
                var command = new SetNativeAvatarColorsCommand(hairColors, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set hair color: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets flair colors (eyebrows or eyelashes) on the specified avatar. Flair colors consist of four components: base, R, G, and B.
        /// </summary>
        /// <param name="avatar">The avatar to set the flair colors on</param>
        /// <param name="hairType">The type of flair to modify (Eyebrows or Eyelashes)</param>
        /// <param name="colors">The flair colors</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask ModifyAvatarFlairColorAsync(GeniesAvatar avatar, HairType hairType, Color[] colors, CancellationToken cancellationToken = default)
        {
            try
            {
                if (hairType != HairType.Eyebrows && hairType != HairType.Eyelashes)
                {
                    CrashReporter.LogError("ModifyAvatarFlairColorAsync supports only HairType.Eyebrows or HairType.Eyelashes.");
                    return;
                }

                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set flair colors.");
                    return;
                }

                if (colors == null ||  colors.Length < 2)
                {
                    CrashReporter.LogError("Insufficient flair colors.");
                    return;
                }

                // Create GenieColorEntry array for all four flair color components based on flair type
                GenieColorEntry[] flairColors;
                if (hairType == HairType.Eyelashes)
                {
                    flairColors = new GenieColorEntry[]
                    {
                        new GenieColorEntry { ColorId = GenieColor.EyelashesBase, Value = colors[0] },
                        new GenieColorEntry { ColorId = GenieColor.EyelashesR, Value = colors[1] },
                        new GenieColorEntry { ColorId = GenieColor.EyelashesG, Value = colors[1] },
                        new GenieColorEntry { ColorId = GenieColor.EyelashesB, Value = colors[1] }
                    };
                }
                else
                {
                    flairColors = new GenieColorEntry[]
                    {
                        new GenieColorEntry { ColorId = GenieColor.EyebrowsBase, Value = colors[0] },
                        new GenieColorEntry { ColorId = GenieColor.EyebrowsR, Value = colors[1] },
                        new GenieColorEntry { ColorId = GenieColor.EyebrowsG, Value = colors[1] },
                        new GenieColorEntry { ColorId = GenieColor.EyebrowsB, Value = colors[1] }
                    };
                }

                // Create and execute the set flair colors command
                var command = new SetNativeAvatarColorsCommand(flairColors, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set flair color: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a hair style on the specified avatar. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The avatar to equip the hair style on</param>
        /// <param name="hairAssetId">The ID of the hair asset to equip</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask EquipHairAsync(GeniesAvatar avatar, string hairAssetId, CancellationToken cancellationToken = default)
        {
            try
            {
                // If avatar is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to equip a hair style.");
                    return;
                }

                // If controller is already equipped with the asset, return
                if (avatar.Controller.IsAssetEquipped(hairAssetId))
                {
                    CrashReporter.LogWarning($"{hairAssetId} asset is already equipped.");
                    return;
                }

                // Create and execute the equip command
                var command = new EquipNativeAvatarAssetCommand(hairAssetId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip hair style with ID '{hairAssetId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a hair style from the specified avatar.
        /// Automatically finds the currently equipped hair asset and unequips it. Supports Hair and FacialHair.
        /// Eyebrows and Eyelashes are color-only; no-op for those types.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the hair style from</param>
        /// <param name="hairType">The type of hair to unequip (Hair, FacialHair, Eyebrows, or Eyelashes—Eyebrows/Eyelashes are no-op)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask UnEquipHairAsync(GeniesAvatar avatar, HairType hairType, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip a hair style.");
                    return;
                }

                // Get all equipped asset IDs from the avatar
                var equippedIds = avatar.Controller.GetEquippedAssetIds();
                if (equippedIds == null || !equippedIds.Any())
                {
                    CrashReporter.LogWarning("No assets are currently equipped on the avatar.");
                    return;
                }

                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return;
                }

                // Eyebrows and Eyelashes: "unequip" by equipping the "none" asset from faceblendshape.
                if (hairType == HairType.Eyebrows || hairType == HairType.Eyelashes)
                {
                    if (avatar?.Controller == null)
                    {
                        CrashReporter.LogError("An avatar is required in order to unequip eyebrows/eyelashes.");
                        return;
                    }

                    string subcategory = hairType == HairType.Eyebrows ? "eyebrows" : "eyelashes";
                    List<string> categories = null;
                    if (!string.IsNullOrEmpty(subcategory))
                    {
                        categories = new List<string> { subcategory };
                    }

                    var allData = await defaultInventoryService.GetDefaultAvatarFlair(limit: null, categories);
                    var noneAsset = allData.FirstOrDefault(a =>
                        string.Equals(a?.Name, "none", StringComparison.OrdinalIgnoreCase));
                    if (noneAsset == null || string.IsNullOrEmpty(noneAsset.AssetId))
                    {
                        CrashReporter.LogWarning($"No 'none' asset found for subcategory '{subcategory}'.");
                        return;
                    }

                    var eyecommand = new EquipNativeAvatarAssetCommand(noneAsset.AssetId, avatar.Controller);
                    await eyecommand.ExecuteAsync(cancellationToken);
                    return;
                }

                // Determine category string based on hair type
                string category = hairType == HairType.FacialHair ? "facialhair" : "hair";

                // Get all hair assets from the inventory service filtered by category
                var allHairAssets = await defaultInventoryService.GetDefaultWearables(categories: new List<string> { category });

                if (allHairAssets == null || !allHairAssets.Any())
                {
                    CrashReporter.LogWarning($"No hair assets found in inventory service.");
                    return;
                }

                // Get hair asset IDs
                var hairAssetIds = allHairAssets.Select(w => w.AssetId).ToList();

                // Convert hair asset IDs to universal IDs using IAssetIdConverter
                var converter = ServiceManager.GetService<IAssetIdConverter>(null);
                if (converter == null)
                {
                    CrashReporter.LogError("IAssetIdConverter service not found.");
                    return;
                }

                var convertedIds = await converter.ConvertToUniversalIdsAsync(hairAssetIds);
                if (convertedIds == null || convertedIds.Count == 0)
                {
                    CrashReporter.LogWarning("Failed to convert hair asset IDs to universal IDs.");
                    return;
                }

                // Faster lookup than convertedIds.Values.Contains(...) in a loop
                var convertedSet = convertedIds.Values.ToHashSet();

                // Find the first match between equipped IDs and converted hair IDs
                var matchingHairId = equippedIds.FirstOrDefault(id => convertedSet.Contains(id));

                if (string.IsNullOrEmpty(matchingHairId))
                {
                    CrashReporter.LogWarning($"No hair asset is currently equipped on the avatar.");
                    return;
                }

                // Create and execute the unequip command
                var command = new UnequipNativeAvatarAssetCommand(matchingHairId, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip hair style: {ex.Message}");
            }
        }

        /// <summary>
        /// Equips a tattoo on the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to equip the tattoo on</param>
        /// <param name="tattooInfo">The tattoo to equip</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo should be placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask EquipTattooAsync(GeniesAvatar avatar, AvatarTattooInfo tattooInfo, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to equip a tattoo.");
                    return;
                }

                // If tattooId is null or empty, return
                if (string.IsNullOrEmpty(tattooInfo.AssetId))
                {
                    CrashReporter.LogError("Tattoo ID cannot be null or empty");
                    return;
                }

                // Create and execute the equip tattoo command
                var command = new EquipNativeAvatarTattooCommand(tattooInfo.AssetId, tattooSlot, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to equip tattoo with ID '{tattooInfo.AssetId}' at slot '{tattooSlot}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unequips a tattoo from the specified controller at the given slot.
        /// </summary>
        /// <param name="avatar">The avatar to unequip the tattoo from</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask that completes with the tattoo ID that was unequipped, or null if none was equipped or on error.</returns>
        public async UniTask<string> UnEquipTattooAsync(GeniesAvatar avatar, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to unequip a tattoo.");
                    return null;
                }

                var command = new UnequipNativeAvatarTattooCommand(tattooSlot, avatar.Controller);
                string unequippedTattooId = command.GetPreviousTattooGuid();
                await command.ExecuteAsync(cancellationToken);
                return unequippedTattooId;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to unequip tattoo at slot '{tattooSlot}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Sets the body preset for the specified controller.
        /// </summary>
        /// <param name="avatar">The avatar to set the body preset on</param>
        /// <param name="preset">The GSkelModifierPreset to apply</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetNativeAvatarBodyPresetAsync(GeniesAvatar avatar, GSkelModifierPreset preset, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a body preset.");
                    return;
                }

                // If preset is null, return
                if (preset == null)
                {
                    CrashReporter.LogError("Body preset cannot be null");
                    return;
                }

                // Create and execute the set body preset command
                var command = new SetNativeAvatarBodyPresetCommand(preset, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set body preset: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the current native avatar body preset from the specified avatar's controller.
        /// </summary>
        /// <param name="avatar">The avatar to get the body preset from</param>
        /// <returns>The current GSkelModifierPreset, or null if avatar/controller is null</returns>
        public GSkelModifierPreset GetNativeAvatarBodyPreset(GeniesAvatar avatar)
        {
            if (avatar?.Controller == null)
            {
                return null;
            }

            return avatar.Controller.GetBodyPreset();
        }

        /// <summary>
        /// Gets current values for a given avatar feature stat type (e.g. all nose stats, all body stats).
        /// </summary>
        /// <param name="avatar">The avatar to read from.</param>
        /// <param name="statType">Which stat category to return (Body, EyeBrows, Eyes, Jaw, Lips, Nose).</param>
        /// <returns>Dictionary of AvatarFeatureStat to current value (typically -1.0 to 1.0). Empty if avatar/controller is null.</returns>
        public Dictionary<AvatarFeatureStat, float> GetAvatarFeatureStats(GeniesAvatar avatar, AvatarFeatureStatType statType)
        {
            var result = new Dictionary<AvatarFeatureStat, float>();
            if (avatar?.Controller == null)
            {
                return result;
            }

            if (statType == AvatarFeatureStatType.Body || statType == AvatarFeatureStatType.All)
            {
                foreach (BodyStats s in Enum.GetValues(typeof(BodyStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            if (statType == AvatarFeatureStatType.EyeBrows || statType == AvatarFeatureStatType.All)
            {
                foreach (EyeBrowsStats s in Enum.GetValues(typeof(EyeBrowsStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(
                        AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            if (statType == AvatarFeatureStatType.Eyes || statType == AvatarFeatureStatType.All)
            {
                foreach (EyeStats s in Enum.GetValues(typeof(EyeStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            if (statType == AvatarFeatureStatType.Jaw || statType == AvatarFeatureStatType.All)
            {
                foreach (JawStats s in Enum.GetValues(typeof(JawStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            if (statType == AvatarFeatureStatType.Lips || statType == AvatarFeatureStatType.All)
            {
                foreach (LipsStats s in Enum.GetValues(typeof(LipsStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            if (statType == AvatarFeatureStatType.Nose || statType == AvatarFeatureStatType.All)
            {
                foreach (NoseStats s in Enum.GetValues(typeof(NoseStats)))
                {
                    result[AvatarFeatureStatMapping.ToAvatarFeatureStat(s)] = avatar.GetBodyAttribute(AvatarFeatureStatMapping.GetAttributeId(AvatarFeatureStatMapping.ToAvatarFeatureStat(s)));
                }
            }

            return result;
        }

        /// <summary>
        /// Modifies a single avatar feature stat. Value is clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The avatar to modify.</param>
        /// <param name="stat">The feature stat to set (e.g. AvatarFeatureStat.Nose_Width, AvatarFeatureStat.Body_NeckThickness).</param>
        /// <param name="value">The value to set (clamped between -1.0 and 1.0).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the update was applied, false if avatar/controller invalid or an error occurred.</returns>
        public bool ModifyAvatarFeatureStat(GeniesAvatar avatar, AvatarFeatureStat stat, float value, CancellationToken cancellationToken = default)
        {
            try
            {
                if (avatar?.Controller == null)
                {
                    CrashReporter.LogError("Avatar and controller are required to modify avatar feature stats");
                    return false;
                }

                cancellationToken.ThrowIfCancellationRequested();
                string attributeId = AvatarFeatureStatMapping.GetAttributeId(stat);
                avatar.Controller.SetBodyAttribute(attributeId, Mathf.Clamp(value, -1.0f, 1.0f));
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to modify avatar feature stat: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The avatar to set the body type on</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous)</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy)</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>UniTask representing the async operation</returns>
        public async UniTask SetAvatarBodyTypeAsync(GeniesAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            try
            {
                // If controller is not a valid object, return
                if (avatar == null)
                {
                    CrashReporter.LogError("An avatar is required in order to set a body type.");
                    return;
                }

                // Determine preset name based on gender and body size combination
                string presetName = GetPresetName(genderType, bodySize);

                // Load the corresponding preset from Resources
                var bodyPreset = AssetPath.Load<GSkelModifierPreset>(presetName);

                if (bodyPreset == null)
                {
                    CrashReporter.LogError($"Failed to load body preset: {presetName}");
                    return;
                }

                // Create and execute the set body preset command
                var command = new SetNativeAvatarBodyPresetCommand(bodyPreset, avatar.Controller);
                await command.ExecuteAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to set avatar body type: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the preset name based on gender type and body size combination.
        /// </summary>
        /// <param name="genderType">The gender type</param>
        /// <param name="bodySize">The body size</param>
        /// <returns>The preset name string</returns>
        private string GetPresetName(GenderType genderType, BodySize bodySize)
        {
            return (genderType, bodySize) switch
            {
                (GenderType.Male, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/maleSkinny_gSkelModifierPreset",
                (GenderType.Male, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/maleMedium_gSkelModifierPreset",
                (GenderType.Male, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/maleHeavy_gSkelModifierPreset",
                (GenderType.Female, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/femaleSkinny_gSkelModifierPreset",
                (GenderType.Female, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/femaleMedium_gSkelModifierPreset",
                (GenderType.Female, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/femaleHeavy_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Skinny) => "Resources/Body/gSkelModifierPresets/androgynousSkinny_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Medium) => "Resources/Body/gSkelModifierPresets/androgynousMedium_gSkelModifierPreset",
                (GenderType.Androgynous, BodySize.Heavy) => "Resources/Body/gSkelModifierPresets/androgynousHeavy_gSkelModifierPreset",
                _ => throw new ArgumentException($"Invalid combination: {genderType}, {bodySize}")
            };
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public async UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                // Get the list of default wearables from the inventory service
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                var defaultWearables = await defaultInventoryService.GetDefaultWearables();

                if (defaultWearables == null || !defaultWearables.Any())
                {
                    CrashReporter.LogError("No default wearables found in inventory service");
                    return new List<WearableAssetInfo>();
                }

                var provider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.DefaultWearablesConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                var wearableAssetInfoList = await UniTask.WhenAll(
                    defaultWearables.Select(async wearable =>
                    {
                        var data = await provider.GetDataForAssetId(wearable.AssetId);

                        var info = new WearableAssetInfo
                        {
                            AssetId = wearable.AssetId,
                            AssetType = wearable.AssetType,
                            Name = wearable.Name,
                            Category = wearable.Category,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return wearableAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable asset info list: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Gets a simplified list of wearable asset information filtered by categories from the default inventory service.
        /// </summary>
        /// <param name="categories">List of WardrobeSubcategory enum values to filter by (e.g., WardrobeSubcategory.hoodie, WardrobeSubcategory.hair, etc.). If null or empty, returns all wearables.</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category</returns>
        public async UniTask<List<WearableAssetInfo>> GetDefaultWearableAssetsListByCategoriesAsync(List<WardrobeSubcategory> categories, CancellationToken cancellationToken = default)
        {
            try
            {
                // Convert enum categories to strings for the inventory service
                List<string> categoryStrings = null;
                if (categories != null && categories.Count > 0)
                {
                    categoryStrings = categories
                        .Where(c => c != WardrobeSubcategory.none && c != WardrobeSubcategory.all)
                        .Select(c => c.ToString().ToLower())
                        .ToList();
                }

                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<WearableAssetInfo>();
                }

                // Split into flair categories (eyebrows, eyelashes) and wearable categories
                List<string> flairCategoryStrings = null;
                List<string> wearableCategoryStrings = null;
                if (categoryStrings != null && categoryStrings.Count > 0)
                {
                    var flairCategories = new[] { "eyebrows", "eyelashes" };
                    flairCategoryStrings = categoryStrings.Where(c => flairCategories.Contains(c)).ToList();
                    if (flairCategoryStrings.Count == 0)
                    {
                        flairCategoryStrings = null;
                    }

                    wearableCategoryStrings = categoryStrings.Where(c => !flairCategories.Contains(c)).ToList();
                    if (wearableCategoryStrings.Count == 0)
                    {
                        wearableCategoryStrings = null;
                    }
                }

                var result = new List<WearableAssetInfo>();

                // Get default wearables for non-flair categories (when categories is null/empty, get all wearables)
                if (flairCategoryStrings == null || wearableCategoryStrings != null || categoryStrings == null)
                {
                    var defaultWearables = await defaultInventoryService.GetDefaultWearables(categories: wearableCategoryStrings ?? categoryStrings);
                    if (defaultWearables != null && defaultWearables.Any())
                    {
                        var wearablesProvider = new InventoryUIDataProvider<ColorTaggedInventoryAsset, BasicInventoryUiData>(
                            UIDataProviderConfigs.DefaultWearablesConfig,
                            ServiceManager.Get<IAssetsService>()
                        );

                        var wearableInfos = await UniTask.WhenAll(
                            defaultWearables.Select(async wearable =>
                            {
                                var data = await wearablesProvider.GetDataForAssetId(wearable.AssetId);
                                var info = new WearableAssetInfo
                                {
                                    AssetId = wearable.AssetId,
                                    AssetType = wearable.AssetType,
                                    Name = wearable.Name,
                                    Category = wearable.Category,
                                    Icon = data.Thumbnail
                                };
                                KeepSpriteReference(data.Thumbnail);
                                return info;
                            })
                        );
                        result.AddRange(wearableInfos);
                    }
                }

                // When categories include eyelash or eyebrow, also get default avatar flair
                if (flairCategoryStrings != null && flairCategoryStrings.Count > 0)
                {
                    var defaultFlair = await defaultInventoryService.GetDefaultAvatarFlair(limit: null, categories: flairCategoryStrings);
                    if (defaultFlair != null && defaultFlair.Any())
                    {
                        var flairProvider = new InventoryUIDataProvider<DefaultInventoryAsset, BasicInventoryUiData>(
                            UIDataProviderConfigs.DefaultAvatarFlairConfig,
                            ServiceManager.Get<IAssetsService>()
                        );
                        await flairProvider.LoadUIData(flairCategoryStrings, null, defaultFlair.Count);

                        var flairInfos = await UniTask.WhenAll(
                            defaultFlair.Select(async flair =>
                            {
                                var data = await flairProvider.GetDataForAssetId(flair.AssetId);
                                var info = new WearableAssetInfo
                                {
                                    AssetId = flair.AssetId,
                                    AssetType = flair.AssetType,
                                    Name = flair.Name,
                                    Category = flair.Category,
                                    Icon = data.Thumbnail
                                };
                                KeepSpriteReference(data.Thumbnail);
                                return info;
                            })
                        );
                        result.AddRange(flairInfos);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get wearable asset info list by categories: {ex.Message}");
                return new List<WearableAssetInfo>();
            }
        }

        /// <summary>
        /// Gets default avatar makeup assets filtered by category from the default inventory service.
        /// </summary>
        public async UniTask<List<DefaultInventoryAsset>> GetDefaultMakeupByCategoryAsync(List<MakeupCategory> categories, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<DefaultInventoryAsset>();
                }

                List<string> categoriesLower = null;
                if (categories != null && categories.Count > 0)
                {
                    categoriesLower = categories
                        .Where(c => c != MakeupCategory.None)
                        .Select(MakeupCategoryMapper.ToInternal)
                        .ToList();
                    if (categoriesLower.Count == 0)
                    {
                        categoriesLower = null;
                    }
                }

                return await defaultInventoryService.GetDefaultAvatarMakeup(limit: limit, categories: categoriesLower) ?? new List<DefaultInventoryAsset>();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default makeup by category: {ex.Message}");
                return new List<DefaultInventoryAsset>();
            }
        }

        /// <summary>
        /// Gets a simplified list of makeup asset information (including thumbnails) for the given categories using DefaultAvatarMakeupConfig.
        /// </summary>
        /// <param name="categories">List of MakeupCategory to filter by. If null or empty, returns all default makeup.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarMakeupInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public async UniTask<List<AvatarMakeupInfo>> GetMakeupAssetInfoListByCategoryAsync(List<MakeupCategory> categories, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<AvatarMakeupInfo>();
                }

                List<string> categoriesLower = null;
                if (categories != null && categories.Count > 0)
                {
                    categoriesLower = categories
                        .Where(c => c != MakeupCategory.None)
                        .Select(MakeupCategoryMapper.ToInternal)
                        .ToList();
                    if (categoriesLower.Count == 0)
                    {
                        categoriesLower = null;
                    }
                }

                var defaultMakeup = await defaultInventoryService.GetDefaultAvatarMakeup(limit: null, categories: categoriesLower) ?? new List<DefaultInventoryAsset>();
                if (defaultMakeup == null || !defaultMakeup.Any())
                {
                    return new List<AvatarMakeupInfo>();
                }

                var provider = new InventoryUIDataProvider<DefaultInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.DefaultAvatarMakeupConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                await provider.LoadUIData(categoriesLower, null, defaultMakeup.Count);

                var makeupAssetInfoList = await UniTask.WhenAll(
                    defaultMakeup.Select(async makeup =>
                    {
                        var data = await provider.GetDataForAssetId(makeup.AssetId);

                        var info = new AvatarMakeupInfo
                        {
                            AssetId = makeup.AssetId,
                            AssetType = makeup.AssetType,
                            Name = makeup.Name,
                            Category = makeup.Category,
                            SubCategories = makeup.SubCategories,
                            Order = makeup.Order,
                            PipelineData = makeup.PipelineData,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return makeupAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get makeup asset info list by category: {ex.Message}");
                return new List<AvatarMakeupInfo>();
            }
        }

        /// <summary>
        /// Gets default avatar features data filtered by category from the default inventory service.
        /// </summary>
        public async UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(AvatarBaseCategory category, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<AvatarFeaturesInfo>();
                }

                var allData = await defaultInventoryService.GetDefaultAvatarBaseData(limit,
                    new List<string> { "faceblendshape" });

                // Filter by category if provided (use string in lower case for comparison)
                string categoryFilter = category == AvatarBaseCategory.None
                    ? null
                    : category.ToString();
                if (!string.IsNullOrEmpty(categoryFilter) && allData != null)
                {
                    allData = allData.Where(asset =>
                        asset.SubCategories != null &&
                        asset.SubCategories.Any(sub =>
                            string.Equals(sub, categoryFilter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return AvatarFeaturesInfo.FromInternalList(allData ?? new List<DefaultAvatarBaseAsset>());
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default avatar features data: {ex.Message}");
                return new List<AvatarFeaturesInfo>();
            }
        }

        /// <summary>
        /// Gets default avatar features data filtered by category string (e.g. for SDK callers).
        /// </summary>
        public async UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(string categoryFilter, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<AvatarFeaturesInfo>();
                }

                var allData = await defaultInventoryService.GetDefaultAvatarBaseData(limit,
                    new List<string> { "faceblendshape" });

                if (!string.IsNullOrEmpty(categoryFilter) && allData != null)
                {
                    allData = allData.Where(asset =>
                        asset.SubCategories != null &&
                        asset.SubCategories.Any(sub =>
                            string.Equals(sub, categoryFilter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                return AvatarFeaturesInfo.FromInternalList(allData ?? new List<DefaultAvatarBaseAsset>());
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default avatar features data: {ex.Message}");
                return new List<AvatarFeaturesInfo>();
            }
        }

        /// <summary>
        /// Gets a simplified list of avatar feature asset information (including thumbnails) for the given category using DefaultAvatarBaseConfig.
        /// </summary>
        public async UniTask<List<AvatarFeaturesInfo>> GetAvatarFeatureAssetInfoListByCategoryAsync(AvatarBaseCategory category, int? limit = null, CancellationToken cancellationToken = default)
        {
            string categoryFilter = category == AvatarBaseCategory.None ? null : category.ToString();
            return await GetAvatarFeatureAssetInfoListByCategoryAsync(categoryFilter, limit, cancellationToken);
        }

        /// <summary>
        /// Gets a simplified list of avatar feature asset information (including thumbnails) for the given category string using DefaultAvatarBaseConfig.
        /// </summary>
        public async UniTask<List<AvatarFeaturesInfo>> GetAvatarFeatureAssetInfoListByCategoryAsync(string categoryFilter, int? limit = null, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<AvatarFeaturesInfo>();
                }

                var allData = await defaultInventoryService.GetDefaultAvatarBaseData(limit, new List<string> { "faceblendshape" });
                if (allData == null || !allData.Any())
                {
                    return new List<AvatarFeaturesInfo>();
                }

                if (!string.IsNullOrEmpty(categoryFilter))
                {
                    allData = allData.Where(asset =>
                        asset.SubCategories != null &&
                        asset.SubCategories.Any(sub =>
                            string.Equals(sub, categoryFilter, StringComparison.OrdinalIgnoreCase))
                    ).ToList();
                }

                var provider = new InventoryUIDataProvider<DefaultAvatarBaseAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.DefaultAvatarBaseConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                await provider.LoadUIData(new List<string> { "faceblendshape" }, null, allData.Count);

                var featureAssetInfoList = await UniTask.WhenAll(
                    allData.Select(async asset =>
                    {
                        var data = await provider.GetDataForAssetId(asset.AssetId);

                        var info = new AvatarFeaturesInfo
                        {
                            AssetId = asset.AssetId,
                            AssetType = asset.AssetType,
                            Name = asset.Name,
                            Category = asset.Category,
                            SubCategories = asset.SubCategories,
                            Order = asset.Order,
                            PipelineData = asset.PipelineData,
                            Tags = asset.Tags,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return featureAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get avatar feature asset info list by category: {ex.Message}");
                return new List<AvatarFeaturesInfo>();
            }
        }

        /// <summary>
        /// Gets a simplified list of tattoo asset information (including thumbnails) from the default image library (tattoo category) using DefaultImageLibraryConfig.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of TattooAssetInfo containing AssetId, AssetType, Name, Category, and Icon.</returns>
        public async UniTask<List<TattooAssetInfo>> GetDefaultTattooAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    return new List<TattooAssetInfo>();
                }

                var defaultTattoos = await defaultInventoryService.GetDefaultImageLibrary(null, new List<string> { "tattoo" });
                if (defaultTattoos == null || !defaultTattoos.Any())
                {
                    CrashReporter.LogError("No default tattoos found in image library");
                    return new List<TattooAssetInfo>();
                }

                var provider = new InventoryUIDataProvider<DefaultInventoryAsset, BasicInventoryUiData>(
                    UIDataProviderConfigs.DefaultImageLibraryConfig,
                    ServiceManager.Get<IAssetsService>()
                );

                await provider.LoadUIData(new List<string> { "tattoo" }, null, defaultTattoos.Count);
                var tattooAssetInfoList = await UniTask.WhenAll(
                    defaultTattoos.Select(async tattoo =>
                    {
                        var data = await provider.GetDataForAssetId(tattoo.AssetId);

                        var info = new TattooAssetInfo
                        {
                            AssetId = tattoo.AssetId,
                            AssetType = tattoo.AssetType,
                            Name = tattoo.Name,
                            Category = tattoo.Category,
                            Icon = data.Thumbnail
                        };

                        KeepSpriteReference(data.Thumbnail);

                        return info;
                    })
                );

                return tattooAssetInfoList.ToList();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get tattoo asset info list: {ex.Message}");
                return new List<TattooAssetInfo>();
            }
        }

        /// <summary>
        /// Gets default (curated) color presets for the specified color type.
        /// </summary>
        /// <param name="colorType">The type of color to retrieve (Hair, FacialHair, Skin, Eyebrow, Eyelash, or Makeup).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IColor containing asset IDs and color values (default presets only).</returns>
        public async UniTask<List<IColor>> GetDefaultColorsAsync(ColorType colorType, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    var emptyColors = new List<Color>();
                    return new List<IColor> { ToIColorValueInternal(colorType, emptyColors) };
                }

                string category = GetColorTypeCategory(colorType);
                List<ColoredInventoryAsset> defaultColors;
                if (colorType == ColorType.Eyes)
                {
                    defaultColors = await defaultInventoryService.GetDefaultAvatarEyes(limit: null, categories: null);
                }
                else
                {
                    defaultColors = await defaultInventoryService.GetDefaultColorPresets(categories: new List<string> { category });
                }

                var result = new List<IColor>();
                if (defaultColors != null)
                {
                    if (colorType == ColorType.MakeupStickers || colorType == ColorType.MakeupLipstick || colorType == ColorType.MakeupFreckles || colorType == ColorType.MakeupFaceGems || colorType == ColorType.MakeupEyeshadow || colorType == ColorType.MakeupBlush)
                    {
                        string makeupSubcategory = MakeupCategoryMapper.ToInternal(GetMakeupCategoryFromColorType(colorType));
                        MakeupCategory makeupCategory = GetMakeupCategoryFromColorType(colorType);
                        var filteredMakeupList = defaultColors.Where(c =>
                            string.Equals(c.Category, "makeup", StringComparison.OrdinalIgnoreCase)
                            && c.SubCategories != null
                            && c.SubCategories.Any(sc => string.Equals(sc, makeupSubcategory, StringComparison.OrdinalIgnoreCase))).ToList();

                        foreach (var colorAsset in filteredMakeupList)
                        {
                            var colors = colorAsset.Colors;
                            bool isEmpty = colors == null || colors.Count == 0;
                            Color clear = Color.clear;
                            Color c0 = isEmpty ? clear : colors[0];
                            Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                            Color c2 = (colors != null && colors.Count > 2) ? colors[2] : c0;
                            Color c3 = (colors != null && colors.Count > 3) ? colors[3] : c0;
                            var iColor = new MakeupColor(makeupCategory, c0, c1, c2, c3);
                            iColor.Name = colorAsset.Name;
                            iColor.IsCustom = false;
                            iColor.Order = colorAsset.Order;
                            result.Add(iColor);
                        }

                        return result;
                    }

                    foreach (var colorAsset in defaultColors)
                    {
                        var iColor = ToIColorValueInternal(colorType, colorAsset.Colors ?? new List<Color>(),
                            colorAsset.AssetId);
                        iColor.IsCustom = false;
                        result.Add(iColor);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get default {colorType} colors: {ex.Message}");
                return new List<IColor>();
            }
        }

        private static string GetColorTypeCategory(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.Eyes => "eyes",
                ColorType.Hair => "hair",
                ColorType.FacialHair => "facialhair",
                ColorType.Skin => "skin",
                ColorType.Eyebrow => "flaireyebrow",
                ColorType.Eyelash => "flaireyelash",
                ColorType.MakeupStickers => "makeup",
                ColorType.MakeupLipstick => "makeup",
                ColorType.MakeupFreckles => "makeup",
                ColorType.MakeupFaceGems => "makeup",
                ColorType.MakeupEyeshadow => "makeup",
                ColorType.MakeupBlush => "makeup",
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Invalid color type")
            };
        }

        private static MakeupCategory GetMakeupCategoryFromColorType(ColorType colorType)
        {
            return colorType switch
            {
                ColorType.MakeupStickers => MakeupCategory.Stickers,
                ColorType.MakeupLipstick => MakeupCategory.Lipstick,
                ColorType.MakeupFreckles => MakeupCategory.Freckles,
                ColorType.MakeupFaceGems => MakeupCategory.FaceGems,
                ColorType.MakeupEyeshadow => MakeupCategory.Eyeshadow,
                ColorType.MakeupBlush => MakeupCategory.Blush,
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Not a makeup ColorType")
            };
        }

        /// <summary>
        /// Converts ColorType and color data to Core IColor for GetDefaultColorsAsync.
        /// </summary>
        private static IColor ToIColorValueInternal(ColorType colorType, List<Color> colors, string assetId = null)
        {
            bool isEmpty = colors == null || colors.Count == 0;
            Color clear = Color.clear;

            switch (colorType)
            {
                case ColorType.Skin:
                    return new SkinColor(isEmpty ? clear : colors[0]);

                case ColorType.Hair:
                case ColorType.FacialHair:
                case ColorType.MakeupStickers:
                case ColorType.MakeupLipstick:
                case ColorType.MakeupFreckles:
                case ColorType.MakeupFaceGems:
                case ColorType.MakeupEyeshadow:
                case ColorType.MakeupBlush:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        Color c2 = (colors != null && colors.Count > 2) ? colors[2] : c0;
                        Color c3 = (colors != null && colors.Count > 3) ? colors[3] : c0;
                        if (colorType == ColorType.Hair)
                        {
                            return new HairColor(c0, c1, c2, c3);
                        }

                        if (colorType == ColorType.FacialHair)
                        {
                            return new FacialHairColor(c0, c1, c2, c3);
                        }

                        return new MakeupColor(c0, c1, c2, c3);
                    }
                case ColorType.Eyebrow:
                case ColorType.Eyelash:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        if (colorType == ColorType.Eyebrow)
                        {
                            return new EyeBrowsColor(c0, c1);
                        }

                        return new EyeLashColor(c0, c1);
                    }
                case ColorType.Eyes:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        return new EyeColor(assetId ?? string.Empty, c0, c1);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Unsupported ColorType.");
            }
        }

        /// <summary>
        /// Gets user (custom) color presets for the specified color type. Only Hair, Eyebrow, and Eyelash support user colors.
        /// </summary>
        /// <param name="colorType">The type of user color to retrieve.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IColor containing user-defined color presets.</returns>
        public async UniTask<List<IColor>> GetUserColorsAsync(UserColorType colorType, CancellationToken cancellationToken = default)
        {
            try
            {
                IDefaultInventoryService defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
                if (defaultInventoryService == null)
                {
                    CrashReporter.LogError("DefaultInventoryService not found");
                    var emptyColors = new List<Color>();
                    return new List<IColor> { ToIColorValueInternalUser(colorType, emptyColors) };
                }

                var result = new List<IColor>();
                string category = GetUserColorTypeCategory(colorType);
                var customColors = await defaultInventoryService.GetCustomColors(category);
                if (customColors != null && customColors.Any())
                {
                    foreach (var customColor in customColors)
                    {
                        var colors = new List<Color>();
                        if (customColor.ColorsHex != null)
                        {
                            foreach (var hex in customColor.ColorsHex)
                            {
                                if (ColorUtility.TryParseHtmlString(hex, out Color color))
                                {
                                    colors.Add(color);
                                }
                            }
                        }

                        var iColor = ToIColorValueInternalUser(colorType, colors);
                        iColor.IsCustom = true;
                        result.Add(iColor);
                    }
                }
                else if (colorType == UserColorType.Hair)
                {
                    var hairColorService = ServiceManager.GetService<HairColorService>(null);
                    if (hairColorService != null)
                    {
                        var customHairIds = await hairColorService.GetAllCustomHairIdsAsync();
                        if (customHairIds != null && customHairIds.Count > 0)
                        {
                            foreach (var id in customHairIds)
                            {
                                var customHairData = await hairColorService.CustomColorDataAsync(id);
                                if (customHairData != null)
                                {
                                    var iColor = ToIColorValueInternalUser(colorType, new List<Color> { customHairData.ColorBase, customHairData.ColorR, customHairData.ColorG, customHairData.ColorB });
                                    iColor.IsCustom = true;
                                    result.Add(iColor);
                                }
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get user {colorType} colors: {ex.Message}");
                return new List<IColor>();
            }
        }

        private static string GetUserColorTypeCategory(UserColorType colorType)
        {
            return colorType switch
            {
                UserColorType.Hair => "hair",
                UserColorType.Eyebrow => "eyebrow",
                UserColorType.Eyelash => "eyelash",
                _ => throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Invalid user color type")
            };
        }

        /// <summary>
        /// Converts UserColorType and color data to Core IColor for GetUserColorsAsync.
        /// </summary>
        private static IColor ToIColorValueInternalUser(UserColorType colorType, List<Color> colors, string assetId = null)
        {
            bool isEmpty = colors == null || colors.Count == 0;
            Color clear = Color.clear;

            switch (colorType)
            {
                case UserColorType.Hair:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        Color c2 = (colors != null && colors.Count > 2) ? colors[2] : c0;
                        Color c3 = (colors != null && colors.Count > 3) ? colors[3] : c0;
                        return new HairColor(c0, c1, c2, c3);
                    }
                case UserColorType.Eyebrow:
                case UserColorType.Eyelash:
                    {
                        Color c0 = isEmpty ? clear : colors[0];
                        Color c1 = (colors != null && colors.Count > 1) ? colors[1] : c0;
                        if (colorType == UserColorType.Eyebrow)
                        {
                            return new EyeBrowsColor(c0, c1);
                        }

                        return new EyeLashColor(c0, c1);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorType), colorType, "Unsupported UserColorType.");
            }
        }

        /// <summary>
        /// Gets the current color from the avatar for the specified color kind (Hair, FacialHair, EyeBrows, EyeLash, Skin, Eyes, or Makeup).
        /// Returns an IColor instance of the corresponding type.
        /// </summary>
        /// <param name="avatar">The avatar to read the color from.</param>
        /// <param name="colorKind">Which IColor type to return.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask that completes with the corresponding IColor value, or null if avatar is null.</returns>
        public async UniTask<IColor> GetColorAsync(GeniesAvatar avatar, AvatarColorKind colorKind, CancellationToken cancellationToken = default)
        {
            if (avatar == null)
            {
                CrashReporter.LogError("Avatar cannot be null");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();
            await UniTask.Yield();

            switch (colorKind)
            {
                case AvatarColorKind.Hair:
                    {
                        var bc = avatar.Controller.GetColor(GenieColor.HairBase);
                        var r = avatar.Controller.GetColor(GenieColor.HairR);
                        var g = avatar.Controller.GetColor(GenieColor.HairG);
                        var b = avatar.Controller.GetColor(GenieColor.HairB);

                        if (bc == null ||  r == null || g == null || b == null)
                        {
                            return null;
                        }
                        return new HairColor(bc.Value, r.Value, g.Value, b.Value);
                    }
                case AvatarColorKind.FacialHair:
                    {
                        var bc = avatar.Controller.GetColor(GenieColor.FacialhairBase);
                        var r = avatar.Controller.GetColor(GenieColor.FacialhairR);
                        var g = avatar.Controller.GetColor(GenieColor.FacialhairG);
                        var b = avatar.Controller.GetColor(GenieColor.FacialhairB);

                        if (bc == null ||  r == null || g == null || b == null)
                        {
                            return null;
                        }
                        return new FacialHairColor(bc.Value, r.Value, g.Value, b.Value);
                    }
                case AvatarColorKind.EyeBrows:
                    {
                        var bc = avatar.Controller.GetColor(GenieColor.EyebrowsBase);
                        var r = avatar.Controller.GetColor(GenieColor.EyebrowsR);

                        if (bc == null ||  r == null)
                        {
                            return null;
                        }
                        return new EyeBrowsColor(bc.Value, r.Value);
                    }
                case AvatarColorKind.EyeLash:
                    {
                        var bc = avatar.Controller.GetColor(GenieColor.EyelashesBase);
                        var r = avatar.Controller.GetColor(GenieColor.EyelashesR);

                        if (bc == null ||  r == null)
                        {
                            return null;
                        }
                        return new EyeLashColor(bc.Value, r.Value);
                    }
                case AvatarColorKind.Skin:
                    {
                        var previousSkin = avatar.Controller.GetColor(GenieColor.Skin);
                        if (previousSkin == null)
                        {
                            return null;
                        }
                        return new SkinColor(previousSkin.Value);
                    }
                case AvatarColorKind.Eyes:
                    {
                        return await GetEyeColorAsync(avatar);
                    }
                case AvatarColorKind.MakeupStickers:
                case AvatarColorKind.MakeupLipstick:
                case AvatarColorKind.MakeupFreckles:
                case AvatarColorKind.MakeupFaceGems:
                case AvatarColorKind.MakeupEyeshadow:
                case AvatarColorKind.MakeupBlush:
                    {
                        var clearColors = new Color[] { Color.clear, Color.clear, Color.clear, Color.clear };
                        MakeupCategory category = GetMakeupCategoryFromAvatarColorKind(colorKind);

                        var makeupCommand = new EquipMakeupColorCommand((int)category, clearColors, avatar.Controller);
                        var prev = makeupCommand.PreviousColors;
                        if (prev == null)
                        {
                            return null;
                        }
                        Color c0 = prev != null && prev.Length > 0 ? (prev[0].Value ?? Color.clear) : Color.clear;
                        Color c1 = prev != null && prev.Length > 1 ? (prev[1].Value ?? Color.clear) : Color.clear;
                        Color c2 = prev != null && prev.Length > 2 ? (prev[2].Value ?? Color.clear) : Color.clear;
                        Color c3 = prev != null && prev.Length > 3 ? (prev[3].Value ?? Color.clear) : Color.clear;
                        return new MakeupColor(category, c0, c1, c2, c3);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorKind), colorKind, "Unsupported avatar color kind");
            }
        }

        private static MakeupCategory GetMakeupCategoryFromAvatarColorKind(AvatarColorKind colorKind)
        {
            return colorKind switch
            {
                AvatarColorKind.MakeupStickers => MakeupCategory.Stickers,
                AvatarColorKind.MakeupLipstick => MakeupCategory.Lipstick,
                AvatarColorKind.MakeupFreckles => MakeupCategory.Freckles,
                AvatarColorKind.MakeupFaceGems => MakeupCategory.FaceGems,
                AvatarColorKind.MakeupEyeshadow => MakeupCategory.Eyeshadow,
                AvatarColorKind.MakeupBlush => MakeupCategory.Blush,
                _ => throw new ArgumentOutOfRangeException(nameof(colorKind), colorKind, "Not a makeup AvatarColorKind")
            };
        }

        private async UniTask<IColor> GetEyeColorAsync(GeniesAvatar avatar)
        {
            var equippedIds = avatar.Controller?.GetEquippedAssetIds();
            if (equippedIds == null || equippedIds.Count == 0)
            {
                return null;
            }

            var defaultInventoryService = ServiceManager.GetService<IDefaultInventoryService>(null);
            if (defaultInventoryService == null)
            {
                return null;
            }

            var eyeAssets = await defaultInventoryService.GetDefaultAvatarEyes(limit: null, categories: null);
            if (eyeAssets == null || eyeAssets.Count == 0)
            {
                return null;
            }

            var eyeAssetIds = eyeAssets
                .Select(a => a.AssetId)
                .Where(id => !string.IsNullOrEmpty(id))
                .ToHashSet();

            var eyeAssetIdToColors = eyeAssets
                .Where(a => !string.IsNullOrEmpty(a.AssetId))
                .ToDictionary(a => a.AssetId, a => a.Colors);

            string eyeAssetId = null;
            Dictionary<string, List<Color>> universalIdToColors = null;
            var converter = ServiceManager.Get<IAssetIdConverter>();
            if (converter != null)
            {
                var convertedIds = await converter.ConvertToUniversalIdsAsync(eyeAssetIds.ToList());
                if (convertedIds != null && convertedIds.Count > 0)
                {
                    universalIdToColors = new Dictionary<string, List<Color>>();
                    foreach (var kvp in convertedIds)
                    {
                        if (eyeAssetIdToColors.TryGetValue(kvp.Key, out var cols))
                        {
                            universalIdToColors[kvp.Value] = cols;
                        }
                    }
                    var universalEyeSet = convertedIds.Values.ToHashSet();
                    eyeAssetId = equippedIds.FirstOrDefault(id => universalEyeSet.Contains(id));
                }
            }

            if (string.IsNullOrEmpty(eyeAssetId))
            {
                eyeAssetId = equippedIds.FirstOrDefault(id => eyeAssetIds.Contains(id));
            }

            var c1 = Color.clear;
            var c2 = Color.clear;
            List<Color> colors = null;
            if (!string.IsNullOrEmpty(eyeAssetId))
            {
                eyeAssetIdToColors.TryGetValue(eyeAssetId, out colors);
            }

            if (colors == null && !string.IsNullOrEmpty(eyeAssetId) && universalIdToColors != null)
            {
                universalIdToColors.TryGetValue(eyeAssetId, out colors);
            }
            if (colors != null && colors.Count > 0)
            {
                c1 = colors[0];
                c2 = colors.Count > 1 ? colors[1] : Color.clear;
            }

            if ((colors == null || colors.Count == 0) && string.IsNullOrEmpty(eyeAssetId))
            {
                return null;
            }

            return new EyeColor(eyeAssetId, c1, c2);
        }

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The avatar to save locally.</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public void SaveAvatarDefinitionLocally(GeniesAvatar avatar, string profileId)
        {
            try
            {
                var headshotPath = CapturePNG(avatar.Controller, profileId);
                LocalAvatarProcessor.SaveOrUpdate(profileId, avatar.Controller.GetDefinitionType(), headshotPath);

            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition locally: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current avatar definition to the cloud.
        /// </summary>
        /// <returns>A UniTask that completes when the cloud save operation is finished.</returns>
        public async UniTask SaveAvatarDefinitionAsync(GeniesAvatar avatar)
        {
            try
            {
                if (GeniesLoginSdk.IsUserSignedInAnonymously())
                {
                    CrashReporter.LogWarning("Cannot write avatar info when user is anonymous");
                    return;
                }

                var avatarService = this.GetService<IAvatarService>();
                if (avatarService == null)
                {
                    CrashReporter.LogError("AvatarService not found. Cannot save avatar definition.");
                    return;
                }

                var avatarDefinition = avatar.Controller.GetDefinitionType();

                var genieRoot = avatar.Controller.Genie.Root;
                var head = genieRoot.transform.Find(_headTransformPath);

                var imageData = AvatarPngCapture.CaptureHeadshotPNGDefaultSettings(genieRoot, head);


                // If no avatar is currently spawned, LoadedAvatar will be null
                if (avatarService.LoadedAvatar != null)
                {
                    _ = await avatarService.UploadAvatarImageAsync(imageData, avatarService.LoadedAvatar.AvatarId);
                }

                await avatarService.UpdateAvatarAsync(avatarDefinition);

            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to cloud: {ex.Message}");
            }
        }

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a JSON definition string from Device cache.
        /// (See <see cref="SaveAvatarDefinitionLocally"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile ID to load the avatar definition from</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public async UniTask<GeniesAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(profileId))
                {
                    CrashReporter.LogError("Profile ID cannot be null or empty");
                    return null;
                }

                // Load the avatar definition string from the profile ID
                var avatarDefinitionString = LocalAvatarProcessor.LoadFromJson(profileId);

                // Parse the avatar definition string into an AvatarDefinition object
                var avatarDefinition = avatarDefinitionString.Definition;

                if (avatarDefinition == null)
                {
                    CrashReporter.LogError($"Failed to parse avatar definition for profile ID: {profileId}");
                    return null;
                }

                // Return the avatar controller with the parsed definition
                return await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(avatarDefinition);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition from profile ID '{profileId}': {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a ScriptableObject stored in local project resources.
        /// (See <see cref="SaveAvatarDefinitionLocally"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile ID to load the avatar definition from</param>
        /// <param name="cancellationToken">Cancellation token for the operation</param>
        /// <returns>A UniTask that completes when the avatar definition is loaded and editing starts</returns>
        public async UniTask<GeniesAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(profileId))
                {
                    CrashReporter.LogError("Profile ID cannot be null or empty");
                    return null;
                }

                // Load the avatar definition string from the profile ID
                var avatarDefinitionString = LocalAvatarProcessor.LoadFromResources(profileId, null);

                // Parse the avatar definition string into an AvatarDefinition object
                var avatarDefinition = avatarDefinitionString.Definition;

                if (avatarDefinition == null)
                {
                    CrashReporter.LogError($"Failed to parse avatar definition for profile ID: {profileId}");
                    return null;
                }

                // Return the avatar controller with the parsed definition
                return await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(avatarDefinition);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar definition from profile ID '{profileId}': {ex.Message}");
            }

            return null;
        }

        public async UniTask UploadAvatarImageAsync(byte[] imageData, string avatarId)
        {
            try
            {
                if (_avatarEditorInstance == null)
                {
                    CrashReporter.LogError("Avatar editor is not open. Cannot save avatar definition to cloud.");
                    return;
                }

                var avatarService = this.GetService<IAvatarService>();
                if (avatarService == null)
                {
                    CrashReporter.LogError("AvatarService not found. Cannot save avatar definition.");
                    return;
                }

                await avatarService.UploadAvatarImageAsync(imageData, avatarId);

            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to cloud: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets the save option for the avatar editor.
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption)
        {
            SetEditorSaveSettings(new AvatarSaveSettings(saveOption));
        }

        /// <summary>
        /// Sets the save option and profile ID for the avatar editor.
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveOption">The save option to use when saving the avatar</param>
        /// <param name="profileId">The profile ID to use when saving locally</param>
        public void SetEditorSaveOption(AvatarSaveOption saveOption, string profileId)
        {
            SetEditorSaveSettings(new AvatarSaveSettings(saveOption, profileId));
        }

        /// <summary>
        /// Sets the save settings for the avatar editor.
        /// These settings persist across multiple editor sessions within the same play session.
        /// Settings are automatically reset when play mode exits
        /// Can be called before or after the editor is opened.
        /// </summary>
        /// <param name="saveSettings">The save settings to use when saving the avatar</param>
        public void SetEditorSaveSettings(AvatarSaveSettings saveSettings)
        {
            // Store settings both for immediate use and persistent session storage
            _pendingSaveSettings = saveSettings;
            _persistentSaveSettings = saveSettings;
            _hasInitializedPersistentSettings = true;

            // If editor is already open, apply the save settings immediately
            if (_avatarEditorInstance != null)
            {
                ApplySaveSettings(saveSettings);
            }
        }

        #region Helpers

        /// <summary>
        /// Calls some endpoints from inventory to begin fetching data early
        /// </summary>
        private void PreloadSpecificAssetData()
        {
            try
            {
                var defaultInventory = ServiceManager.Get<IDefaultInventoryService>();
                defaultInventory.GetDefaultWearables(null, new List<string> { "hair" }).Forget();
                defaultInventory.GetUserWearables().Forget();
                defaultInventory.GetDefaultAvatarBaseData().Forget();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to preload inventory assets when opening avatar editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies the save settings to the avatar editing screen if the editor is currently open.
        /// If the editor is not open, the settings will be applied when the editor is initialized.
        /// </summary>
        /// <param name="saveSettings">The save settings to apply</param>
        private void ApplySaveSettings(AvatarSaveSettings saveSettings)
        {
            // If the editor is not currently open, the settings are already stored in _pendingSaveSettings
            // and will be applied when the editor is opened in InitializeEditing()
            if (_avatarEditorInstance == null)
            {
                return; // This is not an error - the editor is simply not open yet
            }

            var avatarEditingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
            if (avatarEditingScreen == null)
            {
                CrashReporter.LogError("AvatarEditingScreen not found. Cannot apply save settings to open editor.");
                return;
            }

            avatarEditingScreen.SetSaveSettings(saveSettings);
        }

        private async UniTask InitializeEditing(GeniesAvatar avatar, Camera camera)
        {
            var virtualCameraManager = _avatarEditorInstance.GetComponentInChildren<VirtualCameraManager>();
            Assert.IsNotNull(virtualCameraManager);

            // We set the rotation here to Quaternion.identity for the camera system to behave correctly.
            // The genie is later also rotated to Quaternion.identity
            _avatarEditorInstance.transform.SetPositionAndRotation(avatar.Root.transform.position, Quaternion.identity);

            var editingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
            Assert.IsNotNull(editingScreen);

            // Apply Save and Exit flag setting if one was set before editor opened
            if (_pendingSaveButtonSetting.HasValue && _pendingExitButtonSetting.HasValue)
            {
                ApplySaveAndExitFlagSetting(_pendingSaveButtonSetting.Value, _pendingExitButtonSetting.Value);
            }

            await editingScreen.Initialize(avatar, camera, virtualCameraManager);

            // Apply save settings after initialization - use pending first, then persistent, then default
            AvatarSaveSettings settingsToApply;
            if (_pendingSaveSettings.HasValue)
            {
                settingsToApply = _pendingSaveSettings.Value;
            }
            else
            {
                settingsToApply = GetPersistentSaveSettings();
            }

            ApplySaveSettings(settingsToApply);
        }

        private static void FinishEditorOpenedSource()
        {
            var source = _editorOpenedSource;
            source?.TrySetResult();
            _editorOpenedSource = null;
        }

        private static void FinishEditorClosedSource()
        {
            var source = _editorClosedSource;
            _editorClosedSource = null;
            source?.TrySetResult();
        }

        private void KeepSpriteReference(Ref<Sprite> spriteRef)
        {
            _spritesGivenToUser.Add(spriteRef);
        }

        public void RemoveSpriteReference(Ref<Sprite> spriteRef)
        {
            spriteRef.Dispose();

            if (_spritesGivenToUser.Contains(spriteRef))
            {
                _spritesGivenToUser.Remove(spriteRef);
            }
        }

        private string CapturePNG(NativeUnifiedGenieController currentCustomizedAvatar, string profileId = null)
        {
            // Use profile ID in filename if provided, otherwise use default
            var filename = string.IsNullOrEmpty(profileId) ? "avatar-headshot.png" : $"{profileId}-headshot.png";

            // Ensure the headshot directory exists
            if (!System.IO.Directory.Exists(LocalAvatarProcessor.HeadshotPath))
            {
                System.IO.Directory.CreateDirectory(LocalAvatarProcessor.HeadshotPath);
            }

            var headShotPath = System.IO.Path.Combine(LocalAvatarProcessor.HeadshotPath, filename);
            GameObject genieRoot = currentCustomizedAvatar.Genie.Root;
            var head = genieRoot.transform.Find(_headTransformPath);

            AvatarPngCapture.CaptureHeadshotPNG(genieRoot, head,
                width: 512,
                height: 512,
                savePath: headShotPath, // writes the file here
                transparentBackground: true,
                msaa: 8,
                fieldOfView: 25f,
                headRadiusMeters: 0.23f, // tweak per your scale
                forwardDistance: 0.8f, // how tight you want it before FOV fit
                cameraUpOffset: new Vector3(0f, 0.05f, 0f));

            return headShotPath;
        }

        /// <summary>
        /// Sets the Save and Exit ActionBarFlags on all BaseCustomizationControllers in the InventoryNavigationGraph.
        /// Excludes CustomHairColor_Controller, CustomEyelashColor_Controller, and CustomEyebrowColor_Controller
        /// (which always need it to exit their custom color editing screen)
        /// </summary>
        /// <param name="enableSaveButton">True to enable the save button, false to disable</param>
        /// <param name="enableExitButton">True to enable the exit button, false to disable</param>
        public void SetSaveAndExitButtonStatus(bool enableSaveButton, bool enableExitButton)
        {
            // Store the pending setting
            _pendingSaveButtonSetting = enableSaveButton;
            _pendingExitButtonSetting = enableExitButton;

            // If editor is already open, apply the setting immediately
            if (_avatarEditorInstance != null)
            {
                ApplySaveAndExitFlagSetting(enableSaveButton, enableExitButton);
            }
            // Note: If editor is not open, the setting will be applied during InitializeEditing
            // (when the editor is opened)
        }

        /// <summary>
        /// Applies the Save and Exit ActionBarFlags setting to all BaseCustomizationControllers in the InventoryNavigationGraph
        /// </summary>
        private void ApplySaveAndExitFlagSetting(bool enableSaveButton, bool enableExitButton)
        {
            try
            {
                if (_avatarEditorInstance == null)
                {
                    CrashReporter.LogWarning("Cannot apply Save and Exit flag setting - editor instance not found");
                    return;
                }

                NavigationGraph navigationGraph = null;

                var avatarEditingScreen = _avatarEditorInstance.GetComponentInChildren<AvatarEditingScreen>();
                if (avatarEditingScreen != null)
                {
                    navigationGraph = avatarEditingScreen.NavGraph;
                }

                if (navigationGraph == null)
                {
                    CrashReporter.LogError("NavigationGraph not found in AvatarEditingScreen");
                    return;
                }

                // Controllers to exclude
                var excludedControllers = new HashSet<Type>
                {
                    typeof(CustomHairColorCustomizationController),
                    typeof(CustomFlairColorCustomizationController)
                };

                // Get all nodes from the navigation graph
                var allControllers = new List<BaseCustomizationController>();
                CollectAllControllers(navigationGraph.GetRootNode(), allControllers, excludedControllers);

                // Update the ActionBarFlags for each controller
                foreach (var controller in allControllers)
                {
                    if (controller != null && controller.CustomizerViewConfig != null)
                    {
                        if (enableSaveButton)
                        {
                            controller.CustomizerViewConfig.actionBarFlags |= ActionBarFlags.Save;
                        }
                        else
                        {
                            controller.CustomizerViewConfig.actionBarFlags &= ~ActionBarFlags.Save;
                        }

                        if (enableExitButton)
                        {
                            controller.CustomizerViewConfig.actionBarFlags |= ActionBarFlags.Exit;
                        }
                        else
                        {
                            controller.CustomizerViewConfig.actionBarFlags &= ~ActionBarFlags.Exit;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to apply Save and Exit ActionBarFlags: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively collects all BaseCustomizationControllers from the navigation graph,
        /// excluding specified controllers by type.
        /// </summary>
        private void CollectAllControllers(
            INavigationNode node,
            List<BaseCustomizationController> controllers,
            HashSet<Type> excludedTypes)
        {
            if (node == null)
            {
                return;
            }

            // Get the controller from this node
            if (node.Controller is BaseCustomizationController controller)
            {
                // Check if this controller should be excluded
                if (!excludedTypes.Any(t => t.IsAssignableFrom(controller.GetType())))
                {
                    controllers.Add(controller);
                }
            }

            // Recursively process child nodes
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    CollectAllControllers(child, controllers, excludedTypes);
                }
            }

            // Also check EditItemNode and CreateItemNode
            if (node.EditItemNode != null)
            {
                CollectAllControllers(node.EditItemNode, controllers, excludedTypes);
            }

            if (node.CreateItemNode != null)
            {
                CollectAllControllers(node.CreateItemNode, controllers, excludedTypes);
            }
        }

        #endregion
    }
}
