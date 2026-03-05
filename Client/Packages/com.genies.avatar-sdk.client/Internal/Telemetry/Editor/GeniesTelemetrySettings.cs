#if UNITY_EDITOR && !GENIES_EXPERIENCE_SDK
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Genies.Telemetry.Editor
{
    /// <summary>
    /// Editor-only persistent state for telemetry settings.
    /// Survives domain reloads / editor restarts.
    /// </summary>
    [FilePath("UserSettings/GeniesTelemetrySettingsEditorState.asset", FilePathAttribute.Location.ProjectFolder)]
    internal class GeniesTelemetrySettingsEditorState : ScriptableSingleton<GeniesTelemetrySettingsEditorState>
    {
        /// <summary>
        /// Whether telemetry is enabled from the editor's point of view.
        /// Default is true (on).
        /// </summary>
        public bool EnableTelemetry = true;
        private void OnEnable()
        {
            // If we ever want to sync from PlayerPrefs into the editor toggle:
            var val = PlayerPrefs.GetInt(GeniesTelemetry.TelemetryEnabledKey, 0);
            EnableTelemetry = (val != 0);
        }

        public void SaveState()
        {
            Save(false);
        }

        /// <summary>
        /// Writes the current EnableTelemetry value into PlayerPrefs
        /// so runtime code can read it.
        /// </summary>
        public void SyncToPlayerPrefs()
        {
            PlayerPrefs.SetInt(GeniesTelemetry.TelemetryEnabledKey, EnableTelemetry ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Project Settings window for toggling telemetry on/off.
    /// Writes a PlayerPrefs flag "Genies.Telemetry.Enabled" (0/1).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesTelemetrySettingsWindow : SettingsProvider
#else
    public class GeniesTelemetrySettingsWindow : SettingsProvider
#endif
    {
        private static readonly GUIContent TelemetryToggleLabel =
            new GUIContent("Enable Telemetry",
                "If disabled, the Genies SDK should skip sending telemetry events for this user.");

        private SerializedObject _stateSO;
        private SerializedProperty _enableTelemetryProp;
        public static event Action<bool> OnTelemetrySet = delegate { };

        public GeniesTelemetrySettingsWindow(string path, SettingsScope scope)
            : base(path, scope)
        {
        }

        public static SettingsProvider CreateProvider()
        {
            var provider = new GeniesTelemetrySettingsWindow(
                "Project/Genies/Telemetry Settings",
                SettingsScope.Project);

            provider.keywords = new HashSet<string>(new[]
            {
                "Genies",
                "Telemetry",
                "Analytics",
                "Tracking"
            });

            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            var state = GeniesTelemetrySettingsEditorState.instance; // ensure load/create
            _stateSO = new SerializedObject(state);
            _enableTelemetryProp = _stateSO.FindProperty("EnableTelemetry");
        }

        public override void OnGUI(string searchContext)
        {
            EditorGUILayout.LabelField("Genies – Telemetry Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Use this toggle to enable or disable Genies telemetry collection.",
                MessageType.Info);

            _stateSO.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_enableTelemetryProp, TelemetryToggleLabel);
            if (EditorGUI.EndChangeCheck())
            {
                _stateSO.ApplyModifiedProperties();

                // Persist ScriptableSingleton and PlayerPrefs
                var state = (GeniesTelemetrySettingsEditorState)_stateSO.targetObject;
                state.SaveState();
                state.SyncToPlayerPrefs();
                
                if (OnTelemetrySet != null)
                {
                    OnTelemetrySet.Invoke(state.EnableTelemetry);
                }
            }
        }

        [SettingsProvider]
        public static SettingsProvider Register() => CreateProvider();

        [MenuItem("Tools/Genies/Settings/Telemetry Settings")]
        private static void OpenWindow()
        {
            SettingsService.OpenProjectSettings("Project/Genies/Telemetry Settings");
        }
    }
}
#endif
