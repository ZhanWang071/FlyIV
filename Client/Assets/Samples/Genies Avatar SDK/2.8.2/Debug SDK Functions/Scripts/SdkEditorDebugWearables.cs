using System;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Wearables &amp; Assets category: Get Default (Builtin) or User (Custom) Wearables, Avatar Features, Hair, Give Asset to User. Independent component; assign avatar or use same GameObject as SdkFunctionsDebugger for auto-sync.
    /// </summary>
    internal class SdkEditorDebugWearables : MonoBehaviour
    {
        [Header("Avatar Debug Options")]
        [Tooltip("The subject avatar for the debug methods")]
        [SerializeField] private ManagedAvatarComponent _avatarToDebug;

        public void SetAvatarToDebug(ManagedAvatarComponent c) { _avatarToDebug = c; }

        private ManagedAvatarComponent AvatarToDebug => _avatarToDebug;

        [Header("Default (Builtin) Wearables By Wearables Category")]
        [Tooltip("Wearable categories for Default Wearables (non-hair: Hoodie, Shirt, Pants, etc.).")]
        [SerializeField] private WearablesCategory _defaultWearableCategories = WearablesCategory.Hoodie;

        [Header("User Specific (Custom) Wearables By Wearables Category")]
        [Tooltip("User wearable categories for Get User Wearables (uses UserWearablesCategory).")]
        [SerializeField] private UserWearablesCategory _userWearableCategories = UserWearablesCategory.Hoodie;


        [Header("For Default Hair Assets by Hair Type")]
        [Tooltip("Hair types to fetch default assets for (e.g. Hair, FacialHair, Eyebrows, Eyelashes).")]
        [SerializeField] private HairType _hairTypesForDefaultAssets = HairType.Hair;

        [Header("For Equip / Unequip Tattoos")]
        [SerializeField] private MegaSkinTattooSlot _tattooSlot = MegaSkinTattooSlot.LeftTopForearm;

        [Header("For Give Asset to User")]
        [Tooltip("Asset ID to grant to the user (leave empty to use first available asset).")]
        [SerializeField] private string _assetIdToGrant = "";

        [InspectorButton("===== Wearables & Asset Methods =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderWearablesAssets() { }

        [InspectorButton("\nGet Default Wearables By Category\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultWearablesByCategoryAsyncTest()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Wearables By Category", "Log in first!");
                return;
            }

            try
            {
                var wearableAssets = await AvatarSdk.GetDefaultWearablesByCategoryAsync(_defaultWearableCategories);

                if (wearableAssets == null || wearableAssets.Count == 0)
                {
                    var categoriesDisplayMsg = string.Join("", _defaultWearableCategories);
                    ShowPopUp("Get Default Wearables By Category", $"No Default wearable assets found for {categoriesDisplayMsg} category");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultWearablesListWindow.Show(wearableAssets, avatar, _defaultWearableCategories.ToString());
#else
                var categoriesDisplay = string.Join("", _defaultWearableCategories);
                var message = $"Found {wearableAssets.Count} wearable assets. Click an item in the list to equip (Editor only).";
                ShowPopUp($"Get Default Wearables By Category - [{categoriesDisplay}]", message);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get default wearables by category: {ex.Message}");
                ShowPopUp("⚠️ Get Default Wearables By Category", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet User Wearables By Category\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetUserWearablesByCategoryAsyncTest()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get User Wearables By Category", "Log in first!");
                return;
            }

            try
            {
                var wearableAssets = await AvatarSdk.GetUserWearablesByCategoryAsync(_userWearableCategories);
                if (wearableAssets == null || wearableAssets.Count == 0)
                {
                    var categoriesDisplayMsg = string.Join("", _userWearableCategories);
                    ShowPopUp("Get User Wearables By Category", $"No wearable assets found for [{categoriesDisplayMsg}] (Source: User).");
                    return;
                }

                var message = $"Found {wearableAssets.Count} user specific assets in user's inventory:\n\n";
                for (int i = 0; i < Math.Min(wearableAssets.Count, 10); i++)
                {
                    var asset = wearableAssets[i];
                    message += $"{i + 1}. {asset.Name}\n" +
                              $"   ID: {asset.AssetId}\n\n";
                }

                if (wearableAssets.Count > 10)
                {
                    message += $"... and {wearableAssets.Count - 10} more assets.\n";
                }

                Debug.Log($"Get User Specific Wearables By Category User:\n{message}");
                ShowPopUp($"Get User Specific Wearables By Category", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get user wearables by category: {ex.Message}");
                ShowPopUp("⚠️ Get User Wearables By Category", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet and Set Default Hair Assets By Hair Type\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultHairAssetsTest()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Hair Assets", "Log in first!");
                return;
            }

            try
            {
                var hairAssets = await AvatarSdk.GetDefaultHairAssets(_hairTypesForDefaultAssets);

                if (hairAssets == null || hairAssets.Count == 0)
                {
                    var typesDisplayMsg = _hairTypesForDefaultAssets.ToString();
                    ShowPopUp("Get Default Hair Assets", $"No default hair assets found for {typesDisplayMsg}.");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultHairListWindow.Show(hairAssets, avatar, _hairTypesForDefaultAssets);
#else
                ShowPopUp($"Get Default Hair Assets - [{_hairTypesForDefaultAssets}]", $"Found {hairAssets.Count} hair assets. Click an item in the list to equip (Editor only).");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get default hair assets: {ex.Message}");
                ShowPopUp("⚠️ Get Default Hair Assets", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("Unequip Hair By Hair Type", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void UnEquipHair()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Unequip Hair", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                await AvatarSdk.UnEquipHairAsync(AvatarToDebug.ManagedAvatar, _hairTypesForDefaultAssets);

                var message = $"Successfully unequipped {_hairTypesForDefaultAssets}.";
                Debug.Log(message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to unequip hair: {ex.Message}");
                ShowPopUp("⚠️ Unequip Hair", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet and Set Default Tattoos on Tattoo slot\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultTattoosTest()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Tattoos", "Log in first!");
                return;
            }

            try
            {
                var tattooAssets = await AvatarSdk.GetDefaultTattoosAsync();

                if (tattooAssets == null || tattooAssets.Count == 0)
                {
                    ShowPopUp("Get Default Tattoos", "No default tattoos found.");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultTattoosListWindow.Show(tattooAssets, avatar, _tattooSlot);
#else
                ShowPopUp("Get Default Tattoos", $"Found {tattooAssets.Count} tattoo assets. Click an item in the list to equip (Editor only).");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get default tattoos: {ex.Message}");
                ShowPopUp("⚠️ Get Default Tattoos", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("Unequip Tattoo by Slot", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void UnEquipTattooTest()
        {
            if (AvatarToDebug?.ManagedAvatar == null)
            {
                ShowPopUp("⚠️ Unequip Tattoo", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.");
                return;
            }

            try
            {
                string unequippedId = await AvatarSdk.UnEquipTattooAsync(AvatarToDebug.ManagedAvatar, _tattooSlot);

                var message = string.IsNullOrEmpty(unequippedId)
                    ? $"No tattoo was equipped on slot {_tattooSlot}."
                    : $"Successfully unequipped tattoo:\nSlot: {_tattooSlot}\nUnequipped ID: {unequippedId}";
                Debug.Log(message);
                ShowPopUp("✅ Unequip Tattoo", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to unequip tattoo: {ex.Message}");
                ShowPopUp("⚠️ Unequip Tattoo", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("Give Asset to User", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GiveAssetToUser()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Give Asset to User", "Log in first!");
                return;
            }

            try
            {
                string assetId = _assetIdToGrant;

                // If no asset ID specified, get the first available asset
                if (string.IsNullOrEmpty(assetId))
                {
                    ShowPopUp("⚠️ Give Asset to User", "No asset ID specified!");
                    return;
                }


                var result = await AvatarSdk.GiveAssetToUserAsync(assetId);
                if (result.Item1)
                {
                    Debug.Log($"Successfully granted asset to user: {assetId}");
                    ShowPopUp("✅ Give Asset to User", $"Successfully granted asset: {assetId}");
                }
                else
                {
                    Debug.LogError($"Failed to give asset to user");
                    ShowPopUp("⚠️ Give Asset to User", $"Failed to give asset: {result.Item2}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to give asset to user: {ex.Message}");
                ShowPopUp("⚠️ Give Asset to User", $"Error: {ex.Message}");
            }
        }

        private void ShowPopUp(string title, string message)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog(title, message, "OK");
#endif
            Debug.LogWarning($"{title}: {message}");
        }
    }
}
