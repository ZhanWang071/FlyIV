using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.AvatarEditor.Core;
using UnityEngine;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Provides events for SDK notifications.
        /// </summary>
        public static partial class Events
        {
            /// <summary>
            /// Event raised when the avatar editor is opened.
            /// </summary>
            public static event Action AvatarEditorOpened
            {
                add => AvatarEditorSDK.EditorOpened += value;
                remove => AvatarEditorSDK.EditorOpened -= value;
            }

            /// <summary>
            /// Event raised when the avatar editor is closed.
            /// </summary>
            public static event Action AvatarEditorClosed
            {
                add => AvatarEditorSDK.EditorClosed += value;
                remove => AvatarEditorSDK.EditorClosed -= value;
            }
        }

        /// <summary>
        /// Gets whether the avatar editor is currently open.
        /// </summary>
        /// <returns>True if the editor is open and active, false otherwise</returns>
        public static bool IsAvatarEditorOpen => AvatarEditorSDK.IsEditorOpen;

        /// <summary>
        /// Opens the Avatar Editor with the specified avatar and camera.
        /// </summary>
        /// <param name="avatar">The avatar to edit. If null, loads the current user's avatar.</param>
        /// <param name="camera">The camera to use for the editor. If null, uses Camera.main.</param>
        /// <returns>A UniTask that completes when the editor is opened.</returns>
        public static async UniTask OpenAvatarEditorAsync(ManagedAvatar avatar, Camera camera = null)
        {
            var geniesAvatar = avatar?.GeniesAvatar;
            if (geniesAvatar is null)
            {
                Debug.LogWarning("The Avatar Editor requires a valid Avatar instance to be opened.");
                return;
            }

            await AvatarEditorSDK.OpenEditorAsync(geniesAvatar, camera);
        }

        /// <summary>
        /// Closes the Avatar Editor and cleans up resources.
        /// </summary>
        /// /// <param name="revertAvatar">Whether the avatar should be reverted to it's pre-edited self.</param>
        /// <returns>A UniTask that completes when the editor is closed.</returns>
        public static async UniTask CloseAvatarEditorAsync(bool revertAvatar) => await AvatarEditorSDK.CloseEditorAsync(revertAvatar);

        /// <summary>
        /// Gets the active avatar being edited in the Avatar Editor.
        /// </summary>
        /// <returns>The currently active ManagedAvatar, or null if no avatar is currently being edited.</returns>
        public static ManagedAvatar GetAvatarEditorAvatar()
        {
            var geniesAvatar = AvatarEditorSDK.GetCurrentActiveAvatar();
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Equips a Wearable by wearable ID on the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the outfit on.</param>
        /// <param name="wearableId">The ID of the wearable to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipWearableByWearableIdAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.EquipWearableByWearableIdAsync(avatar.GeniesAvatar, wearableId, cancellationToken);
            }
        }

        /// <summary>
        /// Equips a Wearable by wearable asset info on the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the outfit on.</param>
        /// <param name="wearableId">The ID of the wearable to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipWearableAsync(ManagedAvatar avatar, WearableAssetInfo wearableAssetInfo, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && !string.IsNullOrEmpty(wearableAssetInfo?.AssetId))
            {
                await AvatarEditorSDK.EquipWearableByWearableIdAsync(avatar.GeniesAvatar, wearableAssetInfo.AssetId, cancellationToken);
            }
        }

        /// <summary>
        /// Unequips a Wearable by wearable ID from the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the outfit from.</param>
        /// <param name="wearableId">The ID of the wearable to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipWearableAsync(ManagedAvatar avatar, WearableAssetInfo wearableAssetInfo, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && !string.IsNullOrEmpty(wearableAssetInfo?.AssetId))
            {
                await AvatarEditorSDK.UnEquipWearableByWearableIdAsync(avatar.GeniesAvatar, wearableAssetInfo.AssetId, cancellationToken);
            }
        }

        /// <summary>
        /// Unequips a Wearable by wearable ID from the specified avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the outfit from.</param>
        /// <param name="wearableId">The ID of the wearable to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipWearableByWearableIdAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.UnEquipWearableByWearableIdAsync(avatar.GeniesAvatar, wearableId, cancellationToken);
            }
        }

        /// <summary>
        /// Sets avatar makeup by equipping the asset (e.g. from GetDefaultMakeupByCategoryAsync). Uses the asset's AssetId.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the makeup on.</param>
        /// <param name="asset">The avatar item (e.g. makeup from GetDefaultMakeupByCategoryAsync) to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipMakeupAsync(ManagedAvatar avatar, AvatarMakeupInfo asset, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && asset != null && !string.IsNullOrEmpty(asset.AssetId))
            {
                await AvatarEditorSDK.EquipMakeupAsync(avatar.GeniesAvatar, AvatarMakeupInfo.ToInternal(asset), cancellationToken);
            }
        }

        /// <summary>
        /// Unequips a makeup asset from the specified avatar using the given AvatarItemInfo's AssetId.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the makeup from.</param>
        /// <param name="asset">The avatar item (e.g. makeup) to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipMakeupAsync(ManagedAvatar avatar, AvatarMakeupInfo asset, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && asset != null && !string.IsNullOrEmpty(asset.AssetId))
            {
                await AvatarEditorSDK.UnEquipMakeupAsync(avatar.GeniesAvatar, AvatarMakeupInfo.ToInternal(asset), cancellationToken);
            }
        }

        /// <summary>
        /// Equips a hair style on the specified avatar. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the hair style on.</param>
        /// <param name="hairAssetId">The ID of the hair asset to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipHairAsync(ManagedAvatar avatar, WearableAssetInfo hairAsset, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && !string.IsNullOrEmpty(hairAsset?.AssetId))
            {
                await AvatarEditorSDK.EquipHairByHairAssetIdAsync(avatar.GeniesAvatar, hairAsset.AssetId, cancellationToken);
            }
        }

        /// <summary>
        /// Equips a hair style on the specified avatar. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the hair style on.</param>
        /// <param name="hairAssetId">The ID of the hair asset to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipHairByHairAssetIdAsync(ManagedAvatar avatar, string hairAssetId, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && !string.IsNullOrEmpty(hairAssetId))
            {
                await AvatarEditorSDK.EquipHairByHairAssetIdAsync(avatar.GeniesAvatar, hairAssetId, cancellationToken);
            }
        }

        /// <summary>
        /// Unequips a hair style from the specified avatar.
        /// Automatically finds the currently equipped hair asset and unequips it. Supports both regular hair and facial hair.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the hair style from.</param>
        /// <param name="hairType">The type of hair to unequip (Hair or FacialHair)</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipHairAsync(ManagedAvatar avatar, HairType hairType, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.UnEquipHairAsync(avatar.GeniesAvatar, EnumMapper.ToInternal(hairType), cancellationToken);
            }
        }

        /// <summary>
        /// Equips a tattoo on the specified avatar at the given slot using AvatarItemInfo (e.g. from GetDefaultTattoosAsync).
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the tattoo on.</param>
        /// <param name="asset">The avatar item (e.g. tattoo from GetDefaultTattoosAsync) to equip.</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo should be placed.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipTattooAsync(ManagedAvatar avatar, AvatarTattooInfo asset, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && asset != null && !string.IsNullOrEmpty(asset.AssetId))
            {
                await AvatarEditorSDK.EquipTattooAsync(avatar.GeniesAvatar, AvatarTattooInfo.ToInternal(asset),
                    MegaSkinTattooSlotExtensions.ToInternal(tattooSlot), cancellationToken);
            }
        }

        /// <summary>
        /// Unequips a tattoo from the specified avatar at the given slot.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the tattoo from.</param>
        /// <param name="tattooSlot">The MegaSkinTattooSlot where the tattoo is placed.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask that completes with the tattoo ID that was unequipped, or null if none was equipped or on error.</returns>
        public static async UniTask<string> UnEquipTattooAsync(ManagedAvatar avatar, MegaSkinTattooSlot tattooSlot, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                return await AvatarEditorSDK.UnEquipTattooAsync(avatar.GeniesAvatar, MegaSkinTattooSlotExtensions.ToInternal(tattooSlot), cancellationToken);
            }
            return null;
        }

        /// <summary>
        /// Creates a HairColor instance for use with SetColorAsync.
        /// </summary>
        /// <param name="baseColor">The base hair color.</param>
        /// <param name="colorR">The red component of the hair color gradient.</param>
        /// <param name="colorG">The green component of the hair color gradient.</param>
        /// <param name="colorB">The blue component of the hair color gradient.</param>
        /// <returns>A HairColor instance that can be passed to SetColorAsync.</returns>
        public static HairColor CreateHairColor(Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            return new HairColor(baseColor, colorR, colorG, colorB);
        }

        /// <summary>
        /// Creates a FacialHairColor instance for use with SetColorAsync.
        /// </summary>
        public static FacialHairColor CreateFacialHairColor(Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            return new FacialHairColor(baseColor, colorR, colorG, colorB);
        }

        /// <summary>
        /// Creates an EyeBrowsColor instance for use with SetColorAsync.
        /// </summary>
        public static EyeBrowsColor CreateEyeBrowsColor(Color baseColor, Color baseColor2)
        {
            return new EyeBrowsColor(baseColor, baseColor2);
        }

        /// <summary>
        /// Creates an EyeLashColor instance for use with SetColorAsync.
        /// </summary>
        public static EyeLashColor CreateEyeLashColor(Color baseColor, Color baseColor2)
        {
            return new EyeLashColor(baseColor, baseColor2);
        }

        /// <summary>
        /// Creates a SkinColor instance for use with SetColorAsync.
        /// </summary>
        public static SkinColor CreateSkinColor(Color skinColor)
        {
            return new SkinColor(skinColor);
        }

        /// <summary>
        /// Creates a MakeupColor instance for use with SetColorAsync.
        /// </summary>
        public static MakeupColor CreateMakeupColor(AvatarMakeupCategory makeupCategory, Color baseColor, Color colorR, Color colorG, Color colorB)
        {
            return new MakeupColor(makeupCategory, baseColor, colorR, colorG, colorB);
        }

        /// <summary>
        /// Modifies a single avatar feature stat. Value is clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to modify.</param>
        /// <param name="stat">AvatarFeatureStat value to set.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if all updates were applied (or stats was null/empty), false otherwise.</returns>
        public static bool ModifyAvatarFeatureStat(ManagedAvatar avatar, AvatarFeatureStat stat, float value, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller == null)
            {
                return false;
            }

            return AvatarEditorSDK.ModifyAvatarFeatureStat(avatar.GeniesAvatar, EnumMapper.ToInternal(stat), value, cancellationToken);
        }

        /// <summary>
        /// Modifies multiple avatar feature stats at once. Values are clamped to -1.0..1.0.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to modify.</param>
        /// <param name="stats">Dictionary of AvatarFeatureStat to value. Null or empty is a no-op and returns true.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if all updates were applied (or stats was null/empty), false otherwise.</returns>
        public static bool ModifyAvatarFeatureStats(ManagedAvatar avatar, Dictionary<AvatarFeatureStat, float> stats, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller == null)
            {
                return false;
            }

            if (stats == null || stats.Count == 0)
            {
                return true;
            }

            var coreStats = new Dictionary<Genies.AvatarEditor.Core.AvatarFeatureStat, float>(stats.Count);
            foreach (var kvp in stats)
            {
                coreStats[EnumMapper.ToInternal(kvp.Key)] = kvp.Value;
            }
            return AvatarEditorSDK.ModifyAvatarFeatureStats(avatar.GeniesAvatar, coreStats, cancellationToken);
        }

        /// <summary>
        /// Gets current values for a given avatar feature stat type (e.g. all nose stats, all body stats).
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to read from.</param>
        /// <param name="statType">Which stat category to return (Body, EyeBrows, Eyes, Jaw, Lips, Nose).</param>
        /// <returns>Dictionary of AvatarFeatureStat to current value (typically -1.0 to 1.0). Empty if avatar/controller is null.</returns>
        public static Dictionary<AvatarFeatureStat, float> GetAvatarFeatureStats(ManagedAvatar avatar, AvatarFeatureStatType statType)
        {
            if (avatar?.Controller == null)
            {
                return new Dictionary<AvatarFeatureStat, float>();
            }
            var coreResult = AvatarEditorSDK.GetAvatarFeatureStats(avatar.GeniesAvatar, EnumMapper.ToInternal(statType));
            var result = new Dictionary<AvatarFeatureStat, float>(coreResult.Count);
            foreach (var kvp in coreResult)
            {
                result[EnumMapper.FromInternal(kvp.Key)] = kvp.Value;
            }
            return result;
        }

        /// <summary>
        /// Sets hair, eyebrow, eyelash, skin, or eye color on the avatar. Use CreateHairColor, CreateSkinColor, etc. to build the color.
        /// </summary>
        /// <param name="color">The color to apply (IAvatarColor from Create* or GetColorAsync).</param>
        /// <param name="avatar">The ManagedAvatar to modify.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the color was successfully set, false otherwise.</returns>
        public static async UniTask<bool> SetColorAsync(ManagedAvatar avatar, IAvatarColor color, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && color != null)
            {
                return await AvatarEditorSDK.SetColorAsync(avatar.GeniesAvatar, EnumMapper.ToIColor(color), cancellationToken);
            }
            return false;
        }

        /// <summary>
        /// Gets the current color from the avatar for the specified color kind (Hair, FacialHair, EyeBrows, EyeLash, Skin, Eyes, Makeup categories).
        /// Note: For certain categories, this method will only work reliably after a SetColor call has been call on the avatar once
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to read the color from.</param>
        /// <param name="colorKind">Which color type to return (Hair, FacialHair, EyeBrows, EyeLash, Skin, Eyes).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>UniTask that completes with the corresponding IAvatarColor value, or null if avatar/controller is null or on error.</returns>
        public static async UniTask<IAvatarColor> GetColorAsync(ManagedAvatar avatar, AvatarColorKind colorKind, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller == null)
            {
                return null;
            }

            var (hexes, assetId) = await AvatarEditorSDK.GetColorDataAsync(avatar.GeniesAvatar, EnumMapper.ToInternal(colorKind), cancellationToken);
            if (hexes == null && string.IsNullOrEmpty(assetId))
            {
                return null;
            }
            switch (colorKind)
            {
                case AvatarColorKind.Hair:
                {
                    return hexes != null && hexes.Length >= 4
                        ? new HairColor(hexes[0], hexes[1], hexes[2], hexes[3])
                        : null;
                }
                case AvatarColorKind.FacialHair:
                {
                    return hexes != null && hexes.Length >= 4
                        ? new FacialHairColor(hexes[0], hexes[1], hexes[2], hexes[3])
                        : null;
                }
                case AvatarColorKind.EyeBrows:
                {
                    return hexes != null && hexes.Length >= 2
                        ? new EyeBrowsColor(hexes[0], hexes[1])
                        : null;
                }
                case AvatarColorKind.EyeLash:
                {
                    return hexes != null && hexes.Length >= 2
                        ? new EyeLashColor(hexes[0], hexes[1])
                        : null;
                }
                case AvatarColorKind.Skin:
                {
                    return hexes != null && hexes.Length >= 1 ? new SkinColor(hexes[0]) : null;
                }
                case AvatarColorKind.Eyes:
                {
                    return hexes != null && hexes.Length >= 1 ? new EyeColor(assetId ?? string.Empty, hexes[0], hexes[1]) : null;
                }
                case AvatarColorKind.MakeupStickers:
                case AvatarColorKind.MakeupLipstick:
                case AvatarColorKind.MakeupFreckles:
                case AvatarColorKind.MakeupFaceGems:
                case AvatarColorKind.MakeupEyeshadow:
                case AvatarColorKind.MakeupBlush:
                {
                return hexes != null && hexes.Length >= 4
                    ? new MakeupColor(AvatarColorKindMakeupCategoryMapper.ToMakeupCategory(colorKind),
                        hexes[0], hexes[1], hexes[2], hexes[3])
                    : null;
                }
                default:
                    return null;
            }
        }

        /// <summary>
        /// Sets an avatar feature by equipping the specified asset. Provides a unified interface for modifying various facial features.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to modify.</param>
        /// <param name="featureType">The type of feature to modify (Eyes, Jawline, Lips, Nose, EyeBrows, EyeLashes, etc.).</param>
        /// <param name="assetId">The ID of the asset to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if the operation succeeded, false otherwise.</returns>
        public static async UniTask<bool> SetAvatarFeatureAsync(ManagedAvatar avatar, AvatarFeaturesInfo feature, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null && feature != null)
            {
                return await AvatarEditorSDK.SetAvatarFeatureAsync(avatar.GeniesAvatar, AvatarFeaturesInfo.ToInternal(feature), cancellationToken);
            }
            return false;
        }

        /// <summary>
        /// Gets default wearable assets for the given wearable categories (non-hair). Maps each WearablesCategory to WardrobeSubcategory and returns default assets only.
        /// </summary>
        /// <param name="wearableCategory">Wearable category to fetch default assets for (e.g. Hoodie, Shirt, Pants)</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for the requested wearable categories (default assets only).</returns>
        public static async UniTask<List<WearableAssetInfo>> GetDefaultWearablesByCategoryAsync(WearablesCategory wearableCategory, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultWearablesByCategoryAsync)) is false)
            {
                return null;
            }


            var internalCategory = EnumMapper.ToInternal(wearableCategory);
            var internalList = await AvatarEditorSDK.GetDefaultWearablesByCategoryAsync(new  List<Genies.AvatarEditor.Core.WearablesCategory>(){internalCategory}, cancellationToken);
            return WearableAssetInfo.FromInternalList(internalList);
        }

        /// <summary>
        /// Gets user wearable assets for the given wearable categories (non-hair). Maps each UserWearablesCategory to WardrobeSubcategory and returns user assets only.
        /// </summary>
        /// <param name="userWearableCategory">User wearable category to fetch user assets for (e.g. Hoodie, Shirt, Pants)</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for the requested wearable categories (user assets only).</returns>
        public static async UniTask<List<WearableAssetInfo>> GetUserWearablesByCategoryAsync(UserWearablesCategory userWearableCategory, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetUserWearablesByCategoryAsync)) is false)
            {
                return null;
            }

            var internalCategory = EnumMapper.ToInternal(userWearableCategory);
            var internalList = await AvatarEditorSDK.GetUserWearablesByCategoryAsync(new  List<Genies.AvatarEditor.Core.UserWearablesCategory>(){internalCategory}, cancellationToken);
            return WearableAssetInfo.FromInternalList(internalList);
        }

        /// <summary>
        /// Gets default hair wearable assets for the given hair type
        /// </summary>
        /// <param name="hairType">Hair type to fetch default assets for (e.g. Hair, FacialHair, Eyebrows, Eyelashes)</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo for default hair assets in the requested categories.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetDefaultHairAssets(HairType hairType, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultHairAssets)) is false)
            {
                return null;
            }

            var internalList = await AvatarEditorSDK.GetDefaultHairAssets(EnumMapper.ToInternal(hairType), cancellationToken);
            return WearableAssetInfo.FromInternalList(internalList);
        }

        /// <summary>
        /// Gets default avatar feature data filtered by category from the default inventory service.
        /// </summary>
        /// <param name="category">Category to filter by (e.g. Eyes, Jaw, Lips, Nose, Brow). Use None to return all avatar base data.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarFeaturesInfo containing asset information, filtered by category if not None.</returns>
        public static async UniTask<List<AvatarFeaturesInfo>> GetDefaultAvatarFeaturesByCategory(AvatarFeatureCategory category, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultAvatarFeaturesByCategory)) is false)
            {
                return null;
            }

            var resultList = await AvatarEditorSDK.GetDefaultAvatarFeaturesByCategory(EnumMapper.ToInternal(category), cancellationToken);
            return AvatarFeaturesInfo.FromInternalList(resultList);
        }

        /// <summary>
        /// Grants an asset to a user, adding it to their inventory.
        /// </summary>
        /// <param name="assetId">Id of the asset.</param>
        /// <returns>A UniTask representing the async operation with a bool indicating success status.</returns>
        public static async UniTask<(bool, string)> GiveAssetToUserAsync(string assetId)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GiveAssetToUserAsync)) is false)
            {
                return new ValueTuple<bool, string>(false, "Gifting requires a logged-in user.");
            }

            return await AvatarEditorSDK.GiveAssetToUserAsync(assetId);
        }

        /// <summary>
        /// Sets the avatar body type with specified gender and body size.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to set the body type on.</param>
        /// <param name="genderType">The gender type (Male, Female, Androgynous).</param>
        /// <param name="bodySize">The body size (Skinny, Medium, Heavy).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetAvatarBodyTypeAsync(ManagedAvatar avatar, GenderType genderType, BodySize bodySize, CancellationToken cancellationToken = default)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SetAvatarBodyTypeAsync(avatar.GeniesAvatar, EnumMapper.ToInternal(genderType), EnumMapper.ToInternal(bodySize), cancellationToken);
            }
        }

        /// <summary>
        /// Saves the current avatar definition locally only.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to save.</param>
        /// <param name="profileId">The profile ID to save the avatar as. If null, uses the default template name.</param>
        /// <returns>A UniTask that completes when the local save operation is finished.</returns>
        public static async UniTask SaveAvatarDefinitionLocallyAsync(ManagedAvatar avatar, string profileId = null)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SaveAvatarDefinitionLocallyAsync(avatar.GeniesAvatar, profileId);
            }
        }

        /// <summary>
        /// Saves the current avatar definition to the user's avatar.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to save.</param>
        /// <returns>A UniTask that completes when the save operation is finished.</returns>
        public static async UniTask SaveUserAvatarDefinitionAsync(ManagedAvatar avatar)
        {
            if (avatar?.Controller != null)
            {
                await AvatarEditorSDK.SaveAvatarDefinitionAsync(avatar.GeniesAvatar);
            }
        }

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a JSON definition string from Device cache.
        /// (See <see cref="SaveAvatarDefinitionLocallyAsync"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile to load.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask that completes with the loaded ManagedAvatar, or null if failed.</returns>
        public static async UniTask<ManagedAvatar> LoadFromLocalAvatarDefinitionAsync(string profileId, CancellationToken cancellationToken = default)
        {
            var geniesAvatar = await AvatarEditorSDK.LoadFromLocalAvatarDefinitionAsync(profileId, cancellationToken);
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Given a profile ID string, loads an Avatar from a ScriptableObject stored in local project resources.
        /// (See <see cref="SaveAvatarDefinitionLocallyAsync"/> for how to save locally)
        /// </summary>
        /// <param name="profileId">The profile to load.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask that completes with the loaded ManagedAvatar, or null if failed.</returns>
        public static async UniTask<ManagedAvatar> LoadFromLocalGameObjectAsync(string profileId, CancellationToken cancellationToken = default)
        {
            var geniesAvatar = await AvatarEditorSDK.LoadFromLocalGameObjectAsync(profileId, cancellationToken);
            return geniesAvatar != null ? new ManagedAvatar(geniesAvatar) : null;
        }

        /// <summary>
        /// Sets the avatar editor to save locally and continue editing.
        /// </summary>
        /// <param name="profileId">The profile ID to use when saving locally. If null, uses the default template name.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveLocallyAndContinueAsync(string profileId) =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveLocallyAndContinue, profileId);

        /// <summary>
        /// Sets the avatar editor to save locally and exit the editor.
        /// </summary>
        /// <param name="profileId">The profile ID to use when saving locally. If null, uses the default template name.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveLocallyAndExitAsync(string profileId) =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveLocallyAndExit, profileId);

        /// <summary>
        /// Sets the avatar editor to save to the cloud and continue editing.
        /// </summary>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveRemotelyAndContinueAsync() =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveRemotelyAndContinue);

        /// <summary>
        /// Sets the avatar editor to save to the cloud and exit the editor.
        /// </summary>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask SetEditorSaveRemotelyAndExitAsync() =>
            await AvatarEditorSDK.SetEditorSaveOptionAsync(Genies.AvatarEditor.Core.AvatarSaveOption.SaveRemotelyAndExit);

        /// <summary>
        /// Gets default (curated) color presets for the specified color type.
        /// </summary>
        /// <param name="colorType">The type of color to retrieve (Hair, FacialHair, Skin, Eyebrow, Eyelash, or Makeup).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IColor containing default color presets.</returns>
        public static async UniTask<List<IAvatarColor>> GetDefaultColorsAsync(ColorType colorType, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultColorsAsync)) is false)
            {
                return null;
            }

            var internalList = await AvatarEditorSDK.GetDefaultColorsAsync(EnumMapper.ToInternal(colorType), cancellationToken);
            return EnumMapper.FromIColors(internalList);
        }

        /// <summary>
        /// Gets user (custom) color presets for the specified color type. Only Hair and Skin support custom colors.
        /// </summary>
        /// <param name="colorType">The type of color to retrieve (Hair or Skin).</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of IAvatarColor containing user-defined color presets.</returns>
        public static async UniTask<List<IAvatarColor>> GetUserColorsAsync(UserColorType colorType, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetUserColorsAsync)) is false)
            {
                return null;
            }

            var internalList = await AvatarEditorSDK.GetUserColorsAsync(EnumMapper.ToInternal(colorType), cancellationToken);
            return EnumMapper.FromIColors(internalList);
        }

        /// <summary>
        /// Gets default avatar makeup assets filtered by category from the default inventory service.
        /// </summary>
        /// <param name="category">AvatarMakeupCategory (e.g. Stickers, Lipstick, Freckles, FaceGems, Eyeshadow, Blush)</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarMakeupInfo for the requested makeup categories.</returns>
        public static async UniTask<List<AvatarMakeupInfo>> GetDefaultMakeupByCategoryAsync(AvatarMakeupCategory category, CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultMakeupByCategoryAsync)) is false)
            {
                return null;
            }

            var resultList = await AvatarEditorSDK.GetDefaultMakeupByCategoryAsync(EnumMapper.ToInternal(category), cancellationToken);
            return AvatarMakeupInfo.FromInternalList(resultList);
        }

        /// <summary>
        /// Gets default tattoo assets from the default inventory service (image library category "Tattoos").
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of AvatarItemInfo for default tattoos.</returns>
        public static async UniTask<List<AvatarTattooInfo>> GetDefaultTattoosAsync(CancellationToken cancellationToken = default)
        {
            if (await EnsureInitializedAndLoggedInAsync(nameof(GetDefaultTattoosAsync)) is false)
            {
                return null;
            }

            var resultList = await AvatarEditorSDK.GetDefaultTattoosAsync(cancellationToken);
            return AvatarTattooInfo.FromInternalList(resultList);
        }

        #region DEPRECATING

        /// <summary>
        /// Gets a simplified list of wearable asset information from the default inventory service.
        /// DEPRECATED: This API is deprecated and will be removed in the next major version. Use GetDefaultWearablesByCategoryAsync or GetUserWearablesByCategoryAsync instead.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetWearableAssetInfoListAsync(CancellationToken cancellationToken = default)
        {
            Debug.LogWarning("GetWearableAssetInfoListAsync is deprecated and will be removed in the next major version. Use GetDefaultWearablesByCategoryAsync or GetUserWearablesByCategoryAsync instead.");
            var result = new List<WearableAssetInfo>();
            foreach (WearablesCategory cat in Enum.GetValues(typeof(WearablesCategory)))
            {
                result.AddRange(await GetDefaultWearablesByCategoryAsync(cat, cancellationToken));
            }
            return result;
        }

        /// <summary>
        /// Equips an outfit by wearable ID on the specified avatar.
        /// DEPRECATED: This API is deprecated and will be removed in the next major version. Use EquipWearableByWearableIdAsync instead.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to equip the outfit on.</param>
        /// <param name="wearableId">The ID of the wearable to equip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask EquipOutfitAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            Debug.LogWarning("EquipOutfitAsync is deprecated and will be removed in the next major version. Use EquipWearableByWearableIdAsync instead.");
            await EquipWearableByWearableIdAsync(avatar, wearableId, cancellationToken);
        }

        /// <summary>
        /// Unequips an outfit by wearable ID from the specified avatar.
        /// DEPRECATED: This API is deprecated and will be removed in the next major version. Use UnEquipWearableByWearableIdAsync instead.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to unequip the outfit from.</param>
        /// <param name="wearableId">The ID of the wearable to unequip.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>
        public static async UniTask UnEquipOutfitAsync(ManagedAvatar avatar, string wearableId, CancellationToken cancellationToken = default)
        {
            Debug.LogWarning("UnEquipOutfitAsync is deprecated and will be removed in the next major version. Use UnEquipWearableByWearableIdAsync instead.");
            await UnEquipWearableByWearableIdAsync(avatar, wearableId, cancellationToken);
        }

        /// <summary>
        /// Sets a skin color on the specified avatar.
        /// DEPRECATED: This API is deprecated and will be removed in the next major version. Use SetColorAsync with CreateSkinColor instead.
        /// </summary>
        /// <param name="avatar">The ManagedAvatar to set the skin color on.</param>
        /// <param name="skinColor">The color to apply as skin color.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>A UniTask representing the async operation.</returns>

        public static async UniTask SetSkinColorAsync(ManagedAvatar avatar, Color skinColor, CancellationToken cancellationToken = default)
        {
            Debug.LogWarning("SetSkinColorAsync is deprecated and will be removed in the next major version. Use SetColorAsync with CreateSkinColor instead.");
            await SetColorAsync(avatar, CreateSkinColor(skinColor), cancellationToken);
        }

        /// <summary>
        /// Gets the assets of the logged in user.
        /// DEPRECATED: This API is deprecated and will be removed in the next major version. Use GetUserWearablesByCategoryAsync instead.
        /// </summary>
        /// <returns>A list of WearableAssetInfo structs containing AssetId, AssetType, Name, and Category.</returns>
        public static async UniTask<List<WearableAssetInfo>> GetUsersAssetsAsync()
        {
            Debug.LogWarning("GetUsersAssetsAsync is deprecated and will be removed in the next major version. Use GetUserWearablesByCategoryAsync instead.");
            var result = new List<WearableAssetInfo>();
            foreach (UserWearablesCategory cat in Enum.GetValues(typeof(UserWearablesCategory)))
            {
                result.AddRange(await GetUserWearablesByCategoryAsync(cat));
            }
            return result;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ensures the SDK is initialized and the user is logged in
        /// </summary>
        private static async UniTask<bool> EnsureInitializedAndLoggedInAsync(string callerName)
        {
            await Instance.InitializeInternalAsync();

            if (IsLoggedIn is false)
            {
                Debug.LogWarning($"{callerName} requires a logged-in user.");
                return false;
            }

            return true;
        }

        #endregion
    }
}
