#if UNITY_EDITOR
using System.Collections.Generic;
using Genies.AvatarEditor.Core;
using Genies.Avatars.Sdk;
using Genies.Telemetry;
using UnityEditor;

namespace Genies.Sdk.Avatar.Telemetry
{
    [InitializeOnLoad]
    internal static class GeniesSdkTelemetryObserver
    {
        private static bool _subscribed;

        static GeniesSdkTelemetryObserver()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EnsureSubscribed();
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                Unsubscribe();
            }
        }

        private static void EnsureSubscribed()
        {
            if (_subscribed)
                return;

            _subscribed = true;

            AvatarEditorSDK.EditorOpened += OnEditorOpened;
            AvatarEditorSDK.EditorClosed += OnEditorClosed;

            AvatarEditorSDK.EquippedAsset += OnAssetEquipped;
            AvatarEditorSDK.UnequippedAsset += OnAssetUnequipped;

            AvatarEditorSDK.SkinColorSet += OnSkinColorSet;
            AvatarEditorSDK.TattooEquipped += OnTattooEquipped;       // Action<string>
            AvatarEditorSDK.TattooUnequipped += OnTattooUnequipped;   // Action<string>
            AvatarEditorSDK.BodyPresetSet += OnBodyPresetSet;
            AvatarEditorSDK.BodyTypeSet += OnBodyTypeSet;

            AvatarEditorSDK.AvatarDefinitionSaved += OnAvatarSaved;
            AvatarEditorSDK.AvatarDefinitionSavedLocally += OnAvatarSavedLocally;
            AvatarEditorSDK.AvatarDefinitionSavedToCloud += OnAvatarSavedToCloud;
            AvatarEditorSDK.AvatarLoadedForEditing += OnAvatarLoaded;

            AvatarEditorSDK.EditorSaveOptionSet += OnSaveOptionSet;
            AvatarEditorSDK.EditorSaveSettingsSet += OnSaveSettingsSet;
            
            GeniesAvatarsSdk.LoadedAvatar += OnLoadedAvatar;
            
            AvatarEditorInitializer.Instance.AvatarSdkInitialized += OnAvatarEditorSdkInitialized;
            GeniesAvatarsSdk.GeniesAvatarsSdkInitialized += OnGeniesAvatarsSdkInitialized;
        }

        private static void OnLoadedAvatar(bool wasDefault, float loadTime)
        {
            var properties = new Dictionary<string, object>()
            {
                { "IsDefault", wasDefault ? "true" : "false" },
                { "loadTime", loadTime },
            };
            
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_loaded", properties));
        }

        private static void Unsubscribe()
        {
            if (!_subscribed)
                return;

            _subscribed = false;

            AvatarEditorSDK.EditorOpened -= OnEditorOpened;
            AvatarEditorSDK.EditorClosed -= OnEditorClosed;

            AvatarEditorSDK.EquippedAsset -= OnAssetEquipped;
            AvatarEditorSDK.UnequippedAsset -= OnAssetUnequipped;

            AvatarEditorSDK.SkinColorSet -= OnSkinColorSet;
            AvatarEditorSDK.TattooEquipped -= OnTattooEquipped;
            AvatarEditorSDK.TattooUnequipped -= OnTattooUnequipped;
            AvatarEditorSDK.BodyPresetSet -= OnBodyPresetSet;
            AvatarEditorSDK.BodyTypeSet -= OnBodyTypeSet;

            AvatarEditorSDK.AvatarDefinitionSaved -= OnAvatarSaved;
            AvatarEditorSDK.AvatarDefinitionSavedLocally -= OnAvatarSavedLocally;
            AvatarEditorSDK.AvatarDefinitionSavedToCloud -= OnAvatarSavedToCloud;
            AvatarEditorSDK.AvatarLoadedForEditing -= OnAvatarLoaded;

            AvatarEditorSDK.EditorSaveOptionSet -= OnSaveOptionSet;
            AvatarEditorSDK.EditorSaveSettingsSet -= OnSaveSettingsSet;
            GeniesAvatarsSdk.LoadedAvatar -= OnLoadedAvatar;
            
            AvatarEditorInitializer.Instance.AvatarSdkInitialized -= OnAvatarEditorSdkInitialized;
            GeniesAvatarsSdk.GeniesAvatarsSdkInitialized -= OnGeniesAvatarsSdkInitialized;
        }

        private static void OnGeniesAvatarsSdkInitialized(float loadTime)
        {
            var properties = new Dictionary<string, object>()
            {
                { "loadTime", loadTime },
            };
            
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_core_sdk_initialized", properties));
        }

        private static void OnAvatarEditorSdkInitialized(float loadTime)
        {
            var properties = new Dictionary<string, object>()
            {
                { "loadTime", loadTime },
            };
            
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_sdk_initialized", properties));
        }

        // --- handlers ---
        private static void OnEditorOpened() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_opened"));

        private static void OnEditorClosed() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_closed"));

        private static void OnAssetEquipped(string wearableId)
        {
            var properties = new Dictionary<string, object>
            {
                { "wearableid", string.IsNullOrEmpty(wearableId) ? "" : wearableId }
            };
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_asset_equipped", properties));
        }

        private static void OnAssetUnequipped(string wearableId)
        {
            var properties = new Dictionary<string, object>
            {
                { "wearableid", string.IsNullOrEmpty(wearableId) ? "" : wearableId }
            };
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_asset_unequipped", properties));
        }

        private static void OnSkinColorSet() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_skin_color_set"));

        private static void OnTattooEquipped(string tattooId)
        {
            var properties = new Dictionary<string, object>
            {
                { "tattooid", string.IsNullOrEmpty(tattooId) ? "" : tattooId }
            };

            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_tattoo_equipped", properties));
        }

        private static void OnTattooUnequipped(string tattooId)
        {
            var properties = new Dictionary<string, object>
            {
                { "tattooid", string.IsNullOrEmpty(tattooId) ? "" : tattooId }
            };

            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_tattoo_unequipped", properties));
        }

        private static void OnBodyPresetSet() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_body_preset_set"));

        private static void OnBodyTypeSet() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_body_type_set"));

        private static void OnAvatarSaved() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_avatar_definition_saved"));

        private static void OnAvatarSavedLocally() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_avatar_definition_saved_locally"));

        private static void OnAvatarSavedToCloud() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_avatar_definition_saved_to_cloud"));

        private static void OnAvatarLoaded() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_avatar_loaded_for_editing"));

        private static void OnSaveOptionSet() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_save_option_set"));

        private static void OnSaveSettingsSet() =>
            GeniesTelemetry.RecordEvent(TelemetryEvent.Create("avatar_editor_save_settings_set"));
    }
}
#endif
