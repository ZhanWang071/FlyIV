using System;
using System.Linq;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Color Presets category: Get Default/User Colors, Get Default Makeup by Category, Set/UnEquip Makeup, Get Default Tattoos. Independent component.
    /// </summary>
    internal class SdkEditorDebugColorPresets : MonoBehaviour
    {
        [Header("Avatar Debug Options")]
        [SerializeField] private ManagedAvatarComponent _avatarToDebug;

        public void SetAvatarToDebug(ManagedAvatarComponent c) { _avatarToDebug = c; }

        private ManagedAvatarComponent AvatarToDebug => _avatarToDebug;

        [Header("Default Builtin Color based on Color Type")]
        [SerializeField] private ColorType _defaultColorType = ColorType.Hair;

        [Header("User Specific (Custom) Color based on Color Type")]
        [SerializeField] private UserColorType _userColorType = UserColorType.Hair;

        [Header("Default Builtin Makeup Assets based on Makeup category")]
        [SerializeField] private AvatarMakeupCategory _makeupCategory = AvatarMakeupCategory.Lipstick;

        [InspectorButton("===== Color Preset Methods =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderColorPresets() { }

        [InspectorButton("\nGet and Set Default Colors based on Color Type\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultColors()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Colors", "Log in first!");
                return;
            }

            try
            {
                var colorPresets = await AvatarSdk.GetDefaultColorsAsync(_defaultColorType);

                if (colorPresets == null || colorPresets.Count == 0)
                {
                    ShowPopUp("Default Color Presets", $"No Default/Builtin {_defaultColorType} colors found");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultColorsListWindow.Show(colorPresets, avatar, _defaultColorType.ToString());
#else
                var message = $"Found {colorPresets.Count} Default/Builtin {_defaultColorType} color presets. Click an item in the list to apply (Editor only).";
                ShowPopUp($"Apply Default/Builtin Color Presets", message);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get color presets: {ex.Message}");
                ShowPopUp("⚠️ Get Default Colors", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet User Colors based on Color Type\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetUserColors()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get User Colors", "Log in first!");
                return;
            }

            try
            {
                var colorPresets = await AvatarSdk.GetUserColorsAsync(_userColorType);

                if (colorPresets == null || colorPresets.Count == 0)
                {
                    ShowPopUp("User Color Presets", $"No user specific {_userColorType} colors found!");
                    return;
                }

                var message = $"Found {colorPresets.Count} user specific {_userColorType} color presets:\n\n";
                for (int i = 0; i < Math.Min(colorPresets.Count, 10); i++) // Show first 10 to avoid overwhelming the popup
                {
                    var preset = colorPresets[i];
                    var colorsStr = preset.Hexes != null && preset.Hexes.Length > 0
                        ? string.Join(", ", preset.Hexes.Select(c => $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})"))
                        : "None";
                    var customLabel = preset.IsCustom ? " (Custom)" : " (Default)";
                    message += $"{i + 1}. {preset.AssetId}\n" +
                              $"   Colors: {colorsStr}\n\n";
                }

                if (colorPresets.Count > 10)
                {
                    message += $"... and {colorPresets.Count - 10} more color presets.\n";
                }

                Debug.Log($"User specific Color Presets ({_userColorType}, Custom):\n{message}");
                ShowPopUp($"User specific color Presets - {_userColorType}", message);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get color presets: {ex.Message}");
                ShowPopUp("⚠️ Get User Colors", $"Error: {ex.Message}");
            }
        }

        [InspectorButton("\nGet Default Makeup By Category with Equip and Unequip\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetDefaultMakeupByCategoryTest()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Get Default Makeup By Category", "Log in first!");
                return;
            }

            try
            {
                var makeupAssets = await AvatarSdk.GetDefaultMakeupByCategoryAsync(_makeupCategory);

                if (makeupAssets == null || makeupAssets.Count == 0)
                {
                    ShowPopUp("Get Default Makeup By Category", $"No default / builtin makeup assets found for [{_makeupCategory}]!");
                    return;
                }

                var avatar = _avatarToDebug != null ? _avatarToDebug.ManagedAvatar : null;
#if UNITY_EDITOR
                DefaultMakeupListWindow.Show(makeupAssets, avatar, _makeupCategory.ToString());
#else
                ShowPopUp("Default Makeup By Category", $"Found {makeupAssets.Count} default makeup assets. Click an item in the list to equip (Editor only).");
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to get default makeup by category: {ex.Message}");
                ShowPopUp("⚠️ Get Default Makeup By Category", $"Error: {ex.Message}");
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
