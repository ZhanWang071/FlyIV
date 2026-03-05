using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
#if UNITY_EDITOR
    /// <summary>
    /// Editor window that shows the full list of default wearables; clicking an item equips it via EquipWearableAsync.
    /// </summary>
    internal class DefaultWearablesListWindow : EditorWindow
    {
        private List<WearableAssetInfo> _assets;
        private ManagedAvatar _avatar;
        private string _categoryTitle;
        private Vector2 _scrollPosition;

        public static void Show(List<WearableAssetInfo> assets, ManagedAvatar avatar, string categoryTitle)
        {
            var window = GetWindow<DefaultWearablesListWindow>(true, "Default Wearables – Click to Equip", true);
            window._assets = assets;
            window._avatar = avatar;
            window._categoryTitle = categoryTitle;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No wearable assets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Category: {_categoryTitle}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_assets.Count}. Click an item to equip on the debug avatar.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                var label = string.IsNullOrEmpty(asset.Name) ? asset.AssetId : $"{asset.Name} ({asset.AssetId})";
                EditorGUILayout.BeginHorizontal();
                bool canEquip = _avatar != null;
                EditorGUI.BeginDisabledGroup(!canEquip);
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22)))
                {
                    EquipWearableAsync(asset);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void EquipWearableAsync(WearableAssetInfo asset)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog("Equip Wearable", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                await AvatarSdk.EquipWearableAsync(_avatar, asset);
                Debug.Log($"Equipped wearable: {asset.Name} ({asset.AssetId})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to equip wearable: {ex.Message}");
                EditorUtility.DisplayDialog("⚠️ Equip Wearable", $"Error: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Editor window that shows the full list of default makeup; clicking an item equips it via EquipMakeupAsync.
    /// </summary>
    internal class DefaultMakeupListWindow : EditorWindow
    {
        private List<AvatarMakeupInfo> _assets;
        private ManagedAvatar _avatar;
        private string _categoryTitle;
        private Vector2 _scrollPosition;

        public static void Show(List<AvatarMakeupInfo> assets, ManagedAvatar avatar, string categoryTitle)
        {
            var window = GetWindow<DefaultMakeupListWindow>(true, "Default Makeup – Click to Equip", true);
            window._assets = assets;
            window._avatar = avatar;
            window._categoryTitle = categoryTitle;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No makeup assets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Category: {_categoryTitle}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_assets.Count}. Click an item to equip on the debug avatar.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                var label =   $"({asset.AssetId})";
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22));
                EditorGUI.BeginDisabledGroup(_avatar == null);
                if (GUILayout.Button("Equip", GUILayout.Width(60), GUILayout.MinHeight(22)))
                {
                    EquipMakeupAsync(asset, equip: true);
                }
                if (GUILayout.Button("Unequip", GUILayout.Width(60), GUILayout.MinHeight(22)))
                {
                    EquipMakeupAsync(asset, equip: false);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void EquipMakeupAsync(AvatarMakeupInfo asset, bool equip)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog(equip ? "Equip Makeup" : "Unequip Makeup", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                if (equip)
                {
                    await AvatarSdk.EquipMakeupAsync(_avatar, asset);
                    Debug.Log($"Equipped makeup: ({asset.AssetId})");
                }
                else
                {
                    if (!_avatar.IsAssetEquipped(asset.AssetId) )
                    {
                        Debug.LogError($"Failed to {(equip ? "equip" : "unequip")} makeup: Error: Asset ID: {asset.AssetId} is not equipped");
                        EditorUtility.DisplayDialog($"⚠️ {(equip ? "Equip" : "Unequip")} Makeup", $"Error: Asset ID: {asset.AssetId} is not equipped", "OK");
                        return;
                    }
                    await AvatarSdk.UnEquipMakeupAsync(_avatar, asset);
                    Debug.Log($"UnEquipped makeup: ({asset.AssetId})");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to {(equip ? "equip" : "unequip")} makeup: {ex.Message}");
                EditorUtility.DisplayDialog($"⚠️ {(equip ? "Equip" : "Unequip")} Makeup", $"Error: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Editor window that shows the full list of default hair assets; clicking an item equips it via EquipHairAsync.
    /// </summary>
    internal class DefaultHairListWindow : EditorWindow
    {
        private List<WearableAssetInfo> _assets;
        private ManagedAvatar _avatar;
        private HairType _hairType;
        private Vector2 _scrollPosition;

        public static void Show(List<WearableAssetInfo> assets, ManagedAvatar avatar, HairType hairType)
        {
            var window = GetWindow<DefaultHairListWindow>(true, "Default Hair – Click to Equip", true);
            window._assets = assets;
            window._avatar = avatar;
            window._hairType = hairType;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No hair assets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Hair type: {_hairType}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_assets.Count}. Click an item to equip on the debug avatar.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected. No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                var label = $"({asset.AssetId})";
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(_avatar == null);
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22)))
                {
                    EquipHairAsync(asset);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void EquipHairAsync(WearableAssetInfo asset)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog("Equip Hair", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                await AvatarSdk.EquipHairAsync(_avatar, asset);
                Debug.Log($"Equipped hair: {asset.Name} ({asset.AssetId})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to equip hair: {ex.Message}");
                EditorUtility.DisplayDialog("⚠️ Equip Hair", $"Error: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Editor window that shows the full list of default tattoos; clicking an item equips it via EquipTattooAsync at the chosen slot.
    /// </summary>
    internal class DefaultTattoosListWindow : EditorWindow
    {
        private List<AvatarTattooInfo> _assets;
        private ManagedAvatar _avatar;
        private MegaSkinTattooSlot _slot;
        private Vector2 _scrollPosition;

        public static void Show(List<AvatarTattooInfo> assets, ManagedAvatar avatar, MegaSkinTattooSlot slot)
        {
            var window = GetWindow<DefaultTattoosListWindow>(true, "Default Tattoos – Click to Equip", true);
            window._assets = assets;
            window._avatar = avatar;
            window._slot = slot;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No tattoo assets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Slot: {_slot}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_assets.Count}. Click an item to equip on the debug avatar at this slot.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                var label = $"({asset.AssetId})";
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(_avatar == null);
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22)))
                {
                    EquipTattooAsync(asset);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void EquipTattooAsync(AvatarTattooInfo asset)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog("Equip Tattoo", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                await AvatarSdk.EquipTattooAsync(_avatar, asset, _slot);
                Debug.Log($"Equipped tattoo: ({asset.AssetId}) at {_slot}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to equip tattoo: {ex.Message}");
                EditorUtility.DisplayDialog("⚠️ Equip Tattoo", $"Error: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Editor window that shows the full list of default color presets; clicking an item applies it via SetColorAsync.
    /// </summary>
    internal class DefaultColorsListWindow : EditorWindow
    {
        private List<IAvatarColor> _colors;
        private ManagedAvatar _avatar;
        private string _categoryTitle;
        private Vector2 _scrollPosition;

        public static void Show(List<IAvatarColor> colors, ManagedAvatar avatar, string categoryTitle)
        {
            var window = GetWindow<DefaultColorsListWindow>(true, "Default Colors – Click to Apply", true);
            window._colors = colors;
            window._avatar = avatar;
            window._categoryTitle = categoryTitle;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_colors == null || _colors.Count == 0)
            {
                EditorGUILayout.HelpBox("No color presets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Color Type: {_categoryTitle}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_colors.Count}. Click an item to apply the color on the debug avatar.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _colors.Count; i++)
            {
                var preset = _colors[i];
                var colorsStr = preset.Hexes != null && preset.Hexes.Length > 0
                    ? string.Join(", ", preset.Hexes.Take(4).Select(c => $"#{ColorUtility.ToHtmlStringRGB(c)}"))
                    : "—";
                var nameOrId =  (preset.AssetId ?? $"Color {i + 1}");
                var customLabel = preset.IsCustom ? " (Custom)" : "";
                var label = $"{nameOrId}{customLabel} [{colorsStr}]";
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(_avatar == null);
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22)))
                {
                    SetColorAsync(preset);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void SetColorAsync(IAvatarColor color)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog("Set Color", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                bool success = await AvatarSdk.SetColorAsync(_avatar, color);
                var nameOrId =  (color.AssetId ?? "Color");
                if (success)
                {
                    Debug.Log($"Applied color: {nameOrId}");
                }
                else
                {
                    EditorUtility.DisplayDialog("⚠️ Set Color", $"Failed to apply: {nameOrId}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set color: {ex.Message}");
                EditorUtility.DisplayDialog("⚠️ Set Color", $"Error: {ex.Message}", "OK");
            }
        }
    }

    /// <summary>
    /// Editor window that shows the full list of default avatar features; clicking an item applies it via SetAvatarFeatureAsync.
    /// </summary>
    internal class DefaultAvatarFeaturesListWindow : EditorWindow
    {
        private List<AvatarFeaturesInfo> _assets;
        private ManagedAvatar _avatar;
        private string _categoryTitle;
        private Vector2 _scrollPosition;

        public static void Show(List<AvatarFeaturesInfo> assets, ManagedAvatar avatar, string categoryTitle)
        {
            var window = GetWindow<DefaultAvatarFeaturesListWindow>(true, "Default Avatar Features – Click to Apply", true);
            window._assets = assets;
            window._avatar = avatar;
            window._categoryTitle = categoryTitle;
            window._scrollPosition = Vector2.zero;
            window.minSize = new Vector2(360, 320);
        }

        private void OnGUI()
        {
            if (_assets == null || _assets.Count == 0)
            {
                EditorGUILayout.HelpBox("No avatar feature assets in list.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"Category: {_categoryTitle}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Count: {_assets.Count}. Click an item to apply the feature on the debug avatar.", EditorStyles.miniLabel);

            if (_avatar == null)
            {
                EditorGUILayout.HelpBox("No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", MessageType.Warning);
            }

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            for (int i = 0; i < _assets.Count; i++)
            {
                var asset = _assets[i];
                var subcategoriesStr = asset.SubCategories != null && asset.SubCategories.Count > 0
                    ? string.Join(", ", asset.SubCategories)
                    : "";
                var label = !string.IsNullOrEmpty(asset.AssetId) ? $"{asset.AssetId}" : $"Feature {i + 1}";
                if (!string.IsNullOrEmpty(subcategoriesStr))
                    label += $" [{subcategoriesStr}]";
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(_avatar == null);
                if (GUILayout.Button(label, GUILayout.ExpandWidth(true), GUILayout.MinHeight(22)))
                {
                    SetAvatarFeatureAsync(asset);
                }
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        private async void SetAvatarFeatureAsync(AvatarFeaturesInfo feature)
        {
            if (_avatar == null)
            {
                EditorUtility.DisplayDialog("Set Avatar Feature", "No avatar selected! Spawn an avatar first and assign the Avatar to Debug in the inspector.", "OK");
                return;
            }

            try
            {
                bool success = await AvatarSdk.SetAvatarFeatureAsync(_avatar, feature);
                var nameOrId = feature.AssetId ?? "Feature";
                if (success)
                {
                    Debug.Log($"Applied avatar feature: {nameOrId}");
                }
                else
                {
                    EditorUtility.DisplayDialog("⚠️ Set Avatar Feature", $"Failed to apply: {nameOrId}", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to set avatar feature: {ex.Message}");
                EditorUtility.DisplayDialog("⚠️ Set Avatar Feature", $"Error: {ex.Message}", "OK");
            }
        }
    }
#endif
}
