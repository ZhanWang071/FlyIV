#if UNITY_EDITOR && !GENIES_EXPERIENCE_SDK
using System.IO;
using System.Text;
using Genies.Login.Native.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Genies.Login.Native.Editor
{
    /// <summary>
    /// Editor-only persistent state using ScriptableSingleton.
    /// This stores the in-editor values and survives domain reloads / editor restarts.
    /// </summary>
    [FilePath("UserSettings/GeniesSettingsEditorState.asset", FilePathAttribute.Location.ProjectFolder)]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesSettingsEditorState : ScriptableSingleton<GeniesSettingsEditorState>
#else
    public class GeniesSettingsEditorState : ScriptableSingleton<GeniesSettingsEditorState>
#endif
    {
        public string ClientId = "";
        public string ClientSecret = "";

        private void OnEnable()
        {
            if (ClientId == null) ClientId = "";
            if (ClientSecret == null) ClientSecret = "";
        }

        public void SaveState() => Save(true);
    }

    /// <summary>
    /// Project Settings window for editing and saving Genies API credentials.
    /// Writes JSON bytes to Assets/Resources/GeniesAuthSettings.bytes (runtime-loadable).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthSettingsWindow : SettingsProvider
#else
    public class GeniesAuthSettingsWindow : SettingsProvider
#endif
    {
        private const string ResourcesDir = "Assets/Genies/Resources";
        private const string JsonBytesFile = "GeniesAuthSettings.bytes";

        private static readonly GUIContent ClientIdLabel = new GUIContent("Client ID");
        private static readonly GUIContent ClientSecretLabel = new GUIContent("Client Secret");

        private SerializedObject _stateSO;
        private SerializedProperty _clientIdProp;
        private SerializedProperty _clientSecretProp;
        
        public GeniesAuthSettingsWindow(string path, SettingsScope scope) : base(path, scope) {}

        public static SettingsProvider CreateProvider()
        {
            var provider = new GeniesAuthSettingsWindow("Project/Genies/Auth Settings", SettingsScope.Project);
            provider.keywords = new System.Collections.Generic.HashSet<string>(new[] { "Genies", "Client", "Secret", "ID", "OAuth" });
            return provider;
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            // Ensure singleton exists
            var state = GeniesSettingsEditorState.instance;

            // Load from Resources into the ScriptableSingleton once
            LoadAuthSettingsFromResourcesIntoState(state);

            _stateSO = new SerializedObject(state);
            _clientIdProp = _stateSO.FindProperty("ClientId");
            _clientSecretProp = _stateSO.FindProperty("ClientSecret");
        }

        public override void OnGUI(string searchContext)
        {
            // Re-evaluate current persisted status from Resources every draw
            bool hasFile;
            bool hasValidCredentials;
            EvaluateAuthStatusFromResources(out hasFile, out hasValidCredentials);

            // Title and description
            EditorGUILayout.LabelField("Genies – API Credentials", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Enter your Client ID and Client Secret, then click `Save`.",
                MessageType.Info
            );

            // Status line
            DrawStatusRow(hasFile, hasValidCredentials);

            EditorGUILayout.Space(8);

            _stateSO.Update();

            // Fields bound to ScriptableSingleton (kept in sync with the .bytes file)
            EditorGUILayout.PropertyField(_clientIdProp, ClientIdLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(ClientSecretLabel);
                _clientSecretProp.stringValue = EditorGUILayout.PasswordField(_clientSecretProp.stringValue);
            }

            _stateSO.ApplyModifiedProperties();

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save", GUILayout.Height(24)))
                {
                    var clientId = _clientIdProp.stringValue;
                    var clientSecret = _clientSecretProp.stringValue;

                    if (string.IsNullOrWhiteSpace(clientId) ||
                        string.IsNullOrWhiteSpace(clientSecret))
                    {
                        EditorUtility.DisplayDialog(
                            "Missing Credentials",
                            "Both Client ID and Client Secret are required before saving.",
                            "OK");
                    }
                    else if (!GeniesAuthLocalValidator.LooksLikeValidPair(clientId, clientSecret))
                    {
                        EditorUtility.DisplayDialog(
                            "Credentials Invalid",
                            "The Client ID or Client Secret does not match the expected format.\n\n" +
                            "Please check for typos before saving.",
                            "OK");
                    }
                    else
                    {
                        SaveJsonToResources(clientId, clientSecret);

                        // Keep ScriptableSingleton as a mirror of the source-of-truth file
                        var state = GeniesSettingsEditorState.instance;
                        state.ClientId = clientId;
                        state.ClientSecret = clientSecret;
                        state.SaveState();

                        EditorUtility.DisplayDialog(
                            "Credentials Saved",
                            "Successfully saved credentials.",
                            "OK");
                    }
                }

                if (GUILayout.Button("Ping JSON", GUILayout.Height(24)))
                {
                    var path = Path.Combine(ResourcesDir, JsonBytesFile).Replace("\\", "/");
                    var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                        Selection.activeObject = obj;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Genies Settings",
                            "No JSON file found yet. Click 'Save' first.",
                            "OK");
                    }
                }

                if (GUILayout.Button("Reload From Resources", GUILayout.Height(24)))
                {
                    // Pull current file contents back into the ScriptableSingleton + UI
                    LoadAuthSettingsFromResourcesIntoState(GeniesSettingsEditorState.instance);
                    // Rebind serialized object so fields reflect new state
                    _stateSO = new SerializedObject(GeniesSettingsEditorState.instance);
                    _clientIdProp = _stateSO.FindProperty("ClientId");
                    _clientSecretProp = _stateSO.FindProperty("ClientSecret");
                }

                if (GUILayout.Button("View Setup Docs", GUILayout.Height(24)))
                {
                    Application.OpenURL("https://docs.genies.com/docs/sdk-avatar/getting-started#register-your-project");
                }
                
                if (GUILayout.Button("Go To Hub", GUILayout.Height(24)))
                {
                    Application.OpenURL("https://hub.genies.com/auth");
                }
            }

            // If invalid, show a big warning
            if (!hasFile || !hasValidCredentials)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(
                    "Credentials are not fully configured. The Genies SDK will not be able to authenticate " +
                    "until a valid Client ID and Client Secret are written to Resources/GeniesAuthSettings.bytes.",
                    MessageType.Error);
            }
        }

        private void DrawStatusRow(bool hasFile, bool hasValidCredentials)
        {
            EditorGUILayout.BeginHorizontal();

            var glyphStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };

            if (!hasFile)
            {
                glyphStyle.normal.textColor = Color.red;
                EditorGUILayout.LabelField("✗", glyphStyle, GUILayout.Width(20));
                EditorGUILayout.LabelField("Could not find stored credentials.");
            }
            else if (!hasValidCredentials)
            {
                glyphStyle.normal.textColor = new Color(1.0f, 0.65f, 0.0f); // orange
                EditorGUILayout.LabelField("!", glyphStyle, GUILayout.Width(20));
                EditorGUILayout.LabelField("Credentials found, but Client ID and/or Client Secret do not match the expected format.");
            }
            else
            {
                glyphStyle.normal.textColor = Color.green;
                EditorGUILayout.LabelField("✓", glyphStyle, GUILayout.Width(20));
                EditorGUILayout.LabelField("Valid credentials detected (Client ID + Secret present and well-formed).");
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Loads current auth settings from Resources/GeniesAuthSettings.bytes and pushes them into the ScriptableSingleton.
        /// This treats the Resources file as the source of truth.
        /// </summary>
        private void LoadAuthSettingsFromResourcesIntoState(GeniesSettingsEditorState state)
        {
            var settings = GeniesAuthSettings.LoadFromResources();
            if (settings != null)
            {
                state.ClientId = settings.ClientId ?? "";
                state.ClientSecret = settings.ClientSecret ?? "";
                state.SaveState();
            }
        }

        /// <summary>
        /// Checks if the Resources-based settings file exists and whether it contains a well-formed ClientId and ClientSecret.
        /// This is exactly the "has the user attempted to set credentials, and are they valid?" check.
        /// </summary>
        private void EvaluateAuthStatusFromResources(out bool hasFile, out bool hasValidCredentials)
        {
            hasFile = false;
            hasValidCredentials = false;

            // Use the same loader used at runtime
            var settings = GeniesAuthSettings.LoadFromResources();
            if (settings == null)
            {
                return;
            }

            hasFile = true;
            hasValidCredentials = GeniesAuthLocalValidator.LooksLikeValidPair(
                settings.ClientId,
                settings.ClientSecret);
        }

        /// <summary>
        /// Writes JSON bytes to Assets/Resources/GeniesAuthSettings.bytes using the same XOR scheme
        /// expected by GeniesAuthSettings.LoadFromResources().
        /// </summary>
        private static void SaveJsonToResources(string clientId, string clientSecret)
        {
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
            }

            var data = new GeniesAuthSettings { ClientId = clientId, ClientSecret = clientSecret };
            string json = JsonUtility.ToJson(data, prettyPrint: false);

            // Convert to bytes
            byte[] rawBytes = Encoding.UTF8.GetBytes(json);

            // XOR obfuscation (must match LoadFromResources)
            byte key = 0x5A;
            for (int i = 0; i < rawBytes.Length; i++)
            {
                rawBytes[i] ^= key;
            }

            var path = Path.Combine(ResourcesDir, JsonBytesFile).Replace("\\", "/");
            File.WriteAllBytes(path, rawBytes);

            AssetDatabase.ImportAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [SettingsProvider]
        public static SettingsProvider Register() => CreateProvider();

        [MenuItem("Tools/Genies/Settings/Auth Settings")]
        private static void OpenWindow()
        {
            SettingsService.OpenProjectSettings("Project/Genies/Auth Settings");
        }
    }
}
#endif
