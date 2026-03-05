using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;

namespace Genies.Sdk.Bootstrap.Editor
{
    [InitializeOnLoad]
    internal class GeniesSdkBootstrapWizard : EditorWindow
    {
        // Scene bootstrapping
        private const string SampleStarterScenesDisplayName = "Sample Starter Scenes";
        private static readonly string DemoModeSceneWithinSample = Path.Combine("Demo Mode", "Scenes", "DemoMode.unity");
        private static readonly string AvatarStarterSceneWithinSample = Path.Combine("Avatar Starter", "Scenes", "AvatarStarter.unity");

        private const int ProgressUpdateInterval = 16;

        // Preference keys for wizard auto-show behavior
        private const string ShowWizardOnStartupPrefKey = "Genies.Sdk.Bootstrap.Editor.ShowWizardOnStartup";
        private const string CheckPrerequisitesOnLoadPrefKey = "Genies.Sdk.Bootstrap.Editor.CheckPrerequisitesOnLoad";

        // Session state key to track if this is the initial editor launch
        private const string EditorSessionStartedKey = "Genies.Sdk.Bootstrap.Editor.SessionStarted";

        private const string DraftClientIdKey = "Genies.Sdk.Bootstrap.Editor.DraftClientId";
        private const string DraftClientSecretKey = "Genies.Sdk.Bootstrap.Editor.DraftClientSecret";

        // UI Color palette for buttons
        private static readonly Color _primaryActionColor = new Color(0.3f, 0.8f, 0.4f, 1.0f); // Green for important actions
        private static readonly Color _externalLinkColor = new Color(0.4f, 0.7f, 1.0f, 1.0f); // Light blue for docs/external links
        private static readonly Color _secondaryActionColor = new Color(0.7f, 0.7f, 0.7f, 1.0f); // Gray for secondary actions
        private static readonly Color _highlightColor = new Color(0.3f, 0.8f, 1.0f, 1.0f); // Cyan for special highlights

        private static readonly ExternalLinks _externalLinks = new ();

        /// <summary>
        /// Event raised when credentials are set
        /// </summary>
        public static event Action CredentialsSet = delegate { };

        /// <summary>
        /// Event raised when credentials are set
        /// </summary>
        public static event Action SdkConfiguredSuccessfully = delegate { };

        public static event Action SdkConfigurationFailed = delegate { };

        // Static constructor called on domain reload
        static GeniesSdkBootstrapWizard()
        {
            EditorApplication.delayCall += OnEditorLoadSequence;
        }

        private static bool GetUserSettingBool(string key, bool defaultValue)
        {
            var value = EditorUserSettings.GetConfigValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }

            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }

        private static void SetUserSettingBool(string key, bool value)
        {
            EditorUserSettings.SetConfigValue(key, value ? "true" : "false");
        }

        private static void OnEditorLoadSequence()
        {
            // First, initialize SDK version tracking for this session
            if (!GeniesSdkVersionChecker.InitializeSessionSdkVersion())
            {
                // Check if SDK version changed during this session and prompt for restart if needed
                var versionChangeInfo = GeniesSdkVersionChecker.CheckForSdkVersionChange();
                if (versionChangeInfo != null && versionChangeInfo.HasChanged)
                {
                    if (PromptEditorRestart(versionChangeInfo.Title, versionChangeInfo.Message))
                    {
                        return;
                    }
                }
            }

            // Then proceed with wizard checks
            CheckAndShowWizardOnLoad();
        }

        private static bool PromptEditorRestart(string dialogTitle, string dialogMessage)
        {
            if (EditorUtility.DisplayDialog(dialogTitle,
                dialogMessage,
                "Restart Now",
                "Restart Later (Not Recommended)"))
            {
                // User clicked "Restart Now" - restart the editor
                string projectPath = Application.dataPath.Replace("/Assets", "");
                EditorApplication.OpenProject(projectPath);
                return true;
            }

            return false;
        }

        private static void CheckAndShowWizardOnLoad()
        {
            // Determine if this is the first load after Unity Editor started (vs a recompile/domain reload)
            bool isInitialEditorStartup = !SessionState.GetBool(EditorSessionStartedKey, false);
            if (isInitialEditorStartup)
            {
                // Mark that the editor session has started
                SessionState.SetBool(EditorSessionStartedKey, true);
            }

            // Get user preferences for both settings (both true by default)
            bool showWizardOnStartup = GetUserSettingBool(ShowWizardOnStartupPrefKey, true);
            bool checkPrerequisitesOnLoad = GetUserSettingBool(CheckPrerequisitesOnLoadPrefKey, true);

            // Check if SDK is already installed
            bool sdkInstalled = GeniesSdkPrerequisiteChecker.IsSdkInstalled();
            bool allPrerequisitesMet = sdkInstalled && GeniesSdkPrerequisiteChecker.AreAllPrerequisitesMet();

            if (allPrerequisitesMet)
            {
                SdkConfiguredSuccessfully?.Invoke();
            }
            else
            {
                SdkConfigurationFailed?.Invoke();
            }

            // Handle initial editor startup - show wizard for documentation/samples visibility
            if (isInitialEditorStartup && showWizardOnStartup)
            {
                ShowWindow();
                return;
            }

            // Handle prerequisite checks on each recompile/domain reload
            if (checkPrerequisitesOnLoad &&
                (allPrerequisitesMet is false || TryGetValidAuthCredentials(out _) is false))
            {
                if (sdkInstalled)
                {
                    // SDK installed but prerequisites not met
                    ShowWindow();
                }
                else if (!GeniesSdkVersionChecker.IsDotUnityPackageVariant())
                {
                    // SDK not installed (UPM variant) - show wizard
                    ShowWindow();
                }
                return;
            }

            // Log warnings if wizard is disabled but issues exist
            if (!showWizardOnStartup && isInitialEditorStartup)
            {
                if (!sdkInstalled && !GeniesSdkVersionChecker.IsDotUnityPackageVariant())
                {
                    Debug.LogWarning(
                        $"[Genies SDK Bootstrap] The target SDK package ({GeniesSdkPrerequisiteChecker.GetPackageName()}) is NOT installed and is required for the Genies SDK to function. " +
                        $"Open the wizard manually via Tools > Genies > SDK Bootstrap Wizard to install it.");
                }
            }

            if (!checkPrerequisitesOnLoad && !allPrerequisitesMet && sdkInstalled)
            {
                Debug.LogWarning(
                    $"[Genies SDK Bootstrap] Prerequisites are not fully met and are required for the Genies SDK to function. " +
                    $"Open the wizard manually via Tools > Genies > SDK Bootstrap Wizard to configure your project.");
            }
        }

        private Vector2 ScrollPosition { get; set; }
        [field: System.NonSerialized]
        private bool IsInstallingGeniesSdkAvatar { get; set; } = false;

        // Prerequisite check results
        [field: System.NonSerialized]
        private bool IsBuildTargetSupported { get; set; } = true;
        [field: System.NonSerialized]
        private bool IsPlatformSupported { get; set; } = true;
        [field: System.NonSerialized]
        private bool Il2CppBackendConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool Il2CppBackendConfiguredAllPlatforms { get; set; } = false;
        [field: System.NonSerialized]
        private bool NetFrameworkConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool NetFrameworkConfiguredAllPlatforms { get; set; } = false;
        [field: System.NonSerialized]
        private bool ActivePlatformSupportsNetFramework { get; set; } = true;
        [field: System.NonSerialized]
        private string PlatformCompatibilityError { get; set; } = "";
        [field: System.NonSerialized]
        private bool GeniesAvatarSdkInstalled { get; set; } = false;
        [field: System.NonSerialized]
        private bool VulkanConfiguredForWindows { get; set; } = false;
        [field: System.NonSerialized]
        private bool VulkanConfiguredForAndroid { get; set; } = false;
        [field: System.NonSerialized]
        private bool Arm64ConfiguredForAndroid { get; set; } = false;
        [field: System.NonSerialized]
        private bool MinAndroidApiLevelConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus AndroidEntryPointStatus { get; set; } = GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.NotApplicable;
        [field: System.NonSerialized]
        private bool ActiveInputHandlingConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private bool TMPEssentialsImported { get; set; } = false;

        private bool AllPrerequisitesMet => Il2CppBackendConfigured && NetFrameworkConfigured && VulkanConfiguredForWindows && VulkanConfiguredForAndroid && Arm64ConfiguredForAndroid && MinAndroidApiLevelConfigured && IsAndroidEntryPointValid && ActiveInputHandlingConfigured && TMPEssentialsImported;

        private bool IsAndroidEntryPointValid => AndroidEntryPointStatus is GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.Valid ||
                                                 AndroidEntryPointStatus is GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.Warning_BothEnabled ||
                                                  AndroidEntryPointStatus is GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.NotApplicable;

        // Prerequisites that can be auto-fixed with "Fix All" button (excludes Input Handling and TMP Essentials)
        private bool AllAutoFixablePrerequisitesMet
        {
            get
            {
                bool met = Il2CppBackendConfiguredAllPlatforms && NetFrameworkConfiguredAllPlatforms;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                met = met && VulkanConfiguredForWindows;
#endif
#if UNITY_ANDROID
                met = met && VulkanConfiguredForAndroid && Arm64ConfiguredForAndroid && MinAndroidApiLevelConfigured && IsAndroidEntryPointValid;
#endif
                return met;
            }
        }

        // File watching
        [field: System.NonSerialized]
        private FileSystemWatcher ManifestWatcher { get; set; }
        [field: System.NonSerialized]
        private bool NeedsRefresh { get; set; } = false;
        [field: System.NonSerialized]
        private bool RefreshScheduled { get; set; } = false;
        [field: System.NonSerialized]
        private bool IsCompiling { get; set; } = false;

        [field: System.NonSerialized]
        private bool AuthCredentialsConfigured { get; set; } = false;
        [field: System.NonSerialized]
        private string WizardClientId { get; set; } = "";
        [field: System.NonSerialized]
        private string WizardClientSecret { get; set; } = "";
        [field: System.NonSerialized]
        private const string ResourcesDir = "Assets/Genies/Resources";
        private const string JsonBytesFile = "GeniesAuthSettings.bytes";

        [MenuItem("Tools/Genies/SDK Bootstrap Wizard", priority = 0)]
        public static void ShowWindow()
        {
            var window = GetWindow<GeniesSdkBootstrapWizard>("Genies SDK Bootstrap Wizard");
            window.minSize = new Vector2(650, 500);
        }

        private void OnEnable()
        {
            // Restore in-progress draft values (focus + domain reload safe)
            WizardClientId = SessionState.GetString(DraftClientIdKey, WizardClientId ?? "");
            WizardClientSecret = SessionState.GetString(DraftClientSecretKey, WizardClientSecret ?? "");

            IsCompiling = EditorApplication.isCompiling;
            RefreshPrerequisiteStatus();
            SetupFileWatchers();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        private void OnDisable()
        {
            CleanupFileWatchers();
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            Repaint();
        }

        private void OnEditorUpdate()
        {
            if (EditorApplication.isCompiling != IsCompiling)
            {
                IsCompiling = EditorApplication.isCompiling;
                Repaint();
            }
        }

        private void OnFocus()
        {
            // Refresh when window gains focus to detect external changes to project settings
            RefreshPrerequisiteStatus();
        }

        private void Update()
        {
            if (NeedsRefresh)
            {
                NeedsRefresh = false;
                RefreshPrerequisiteStatus();
                Repaint();
            }
        }

        private void OnGUI()
        {
            // === FIXED HEADER SECTION (always visible) ===
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();

            EditorGUILayout.Space(15);

            // Refresh Status button (top right)
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var refreshIcon = EditorGUIUtility.IconContent("Refresh");
            var refreshButton = new GUIContent(refreshIcon.image, "Refresh Status");
            if (GUILayout.Button(refreshButton, GUILayout.Width(24), GUILayout.Height(24)))
            {
                RefreshPrerequisiteStatus();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // Large title
            var titleStyle = new GUIStyle(EditorStyles.boldLabel);
            titleStyle.fontSize = 20;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.wordWrap = true;
            EditorGUILayout.LabelField("Genies SDK Bootstrap Wizard", titleStyle);

            EditorGUILayout.Space(10);

            // Welcome message
            var welcomeStyle = new GUIStyle(EditorStyles.label);
            welcomeStyle.alignment = TextAnchor.MiddleCenter;
            welcomeStyle.wordWrap = true;
            welcomeStyle.fontSize = 12;
            EditorGUILayout.LabelField("Setup made easy\nFor you, the Developer\nLet's build together", welcomeStyle);

            EditorGUILayout.Space(15);

            DrawDivider();

            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();

            // === SCROLLABLE CONTENT ===
            ScrollPosition = EditorGUILayout.BeginScrollView(ScrollPosition);

            // Add horizontal margins
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();

            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                DrawPlayModeInterface();
                return;
            }

            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox(
                    "The Genies SDK Bootstrap Wizard is disabled while the Editor is compiling.\n\n" +
                    "Please wait for compilation to complete.",
                    MessageType.Info);

                DrawDivider();

                EditorGUILayout.EndVertical();
                GUILayout.Space(20);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndScrollView();

                // === FIXED FOOTER SECTION (always visible) ===
                DrawFixedFooter();
                return;
            }

            // Current Build Target section
            EditorGUILayout.LabelField("Current Build Target", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var platformLabel = $"Platform: {activeBuildTarget}";
            EditorGUILayout.LabelField(platformLabel, GUILayout.ExpandWidth(true));

            if (GUILayout.Button("Open Build Settings", GUILayout.Width(150)))
            {
                EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));
            }
            EditorGUILayout.EndHorizontal();

            // Check if build target is unsupported - show error but continue with wizard
            if (IsBuildTargetSupported is false)
            {
                var editorPlatform = Application.platform;
                var supportedBuildTargetsList = GeniesSdkPrerequisiteChecker.GetSupportedBuildTargetsListString();
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    $"Unsupported Build Target: {activeBuildTarget}\n\n" +
                    $"The Genies SDK does not support building for {activeBuildTarget} on {editorPlatform}. Only the following build targets are supported:\n" +
                    supportedBuildTargetsList + "\n" +
                    "Please switch to a supported build target in Build Settings.",
                    MessageType.Error);
            }

            DrawDivider();

            // Calculate section numbering dynamically
            int currentSection = 1;

            // === SECTION 1: Prerequisites ===
            DrawSectionHeader($"{currentSection}. Prerequisites", "Ensure your project meets the requirements");
            EditorGUILayout.Space(5);

            // --- Auto-fixable prerequisites subsection ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();
            var subsectionStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField("Build Settings", subsectionStyle);
            GUILayout.FlexibleSpace();

            // Fix All button
            EditorGUI.BeginDisabledGroup(AllAutoFixablePrerequisitesMet);
            var originalColor = GUI.backgroundColor;
            if (!AllAutoFixablePrerequisitesMet)
            {
                GUI.backgroundColor = _primaryActionColor;
            }
            var fixAllTooltip = AllAutoFixablePrerequisitesMet
                ? "All build settings are already configured"
                : "Apply all build setting configurations for all supported platforms";
            if (GUILayout.Button(new GUIContent("Configure All", fixAllTooltip), GUILayout.Width(100)))
            {
                FixAllAutoFixablePrerequisites();
            }
            GUI.backgroundColor = originalColor;
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            DrawPlatformPrerequisiteCheck("IL2CPP Scripting Backend", Il2CppBackendConfigured, Il2CppBackendConfiguredAllPlatforms, FixIl2CppBackendAllPlatforms,
                "IL2CPP scripting backend is required for the Genies SDK. Configure will configure IL2CPP for all supported build targets.");

            DrawPlatformPrerequisiteCheck(".NET Framework 4.8", NetFrameworkConfigured, NetFrameworkConfiguredAllPlatforms, FixNetFrameworkAllPlatforms,
                ".NET Framework 4.8 is required for the Genies SDK. Configure will configure .NET Framework 4.8 for all compatible platforms.");

            // Show error if active platform doesn't support .NET Framework
            if (!string.IsNullOrEmpty(PlatformCompatibilityError))
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(PlatformCompatibilityError, MessageType.Error);
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            DrawPrerequisiteCheck("Vulkan Graphics API (Windows)", VulkanConfiguredForWindows, FixVulkanForWindows,
                "Vulkan is required as the graphics API for Windows Standalone builds when using the Genies SDK.");
#endif

#if UNITY_ANDROID
            DrawPrerequisiteCheck("Vulkan Graphics API (Android)", VulkanConfiguredForAndroid, FixVulkanForAndroid,
                "Vulkan is required as the graphics API for Android builds when using the Genies SDK.");

            DrawPrerequisiteCheck("ARM64 Architecture (Android)", Arm64ConfiguredForAndroid, FixArm64ForAndroid,
                "ARM64 architecture is required for Android builds when using the Genies SDK.");

            DrawPrerequisiteCheck("Minimum Android 12.0 (API Level 31)", MinAndroidApiLevelConfigured, FixMinAndroidApiLevel,
                "Android 12.0 (API level 31) is required as the minimum API level for Android builds when using the Genies SDK.");

#if UNITY_6000_0_OR_NEWER
            if (EditorUserBuildSettings.activeBuildTarget is BuildTarget.Android)
            {
                DrawAndroidEntryPointCheck();
            }
#endif
#endif

            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(10);

            // --- Manual prerequisites subsection ---
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("Additional Configuration", subsectionStyle);
            EditorGUILayout.Space(5);

            // TextMesh Pro Essentials check for all platforms
            DrawPrerequisiteCheck("TextMesh Pro Essential Resources", TMPEssentialsImported, FixTMPEssentials,
                "TextMesh Pro Essential Resources are required for UI text rendering in the Genies SDK.", "Import");

            // Show Active Input Handling for all platforms to encourage using the new Input System
            DrawActiveInputHandlingCheck();

            EditorGUILayout.Space(3);
            EditorGUILayout.EndVertical();

            DrawDivider();

#if !GENIES_AVATARSDK_CLIENT
            // === SECTION 2: Install SDK (UPM variant only) ===
            currentSection++;
            DrawSectionHeader($"{currentSection}. Install SDK", "Add the Genies Avatar SDK to your project");
            EditorGUILayout.Space(5);
            // Install button
            EditorGUI.BeginDisabledGroup(IsInstallingGeniesSdkAvatar || GeniesAvatarSdkInstalled);

            var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
            var buttonText = GeniesAvatarSdkInstalled
                ? $"Genies Avatar SDK (Already Installed)\n{packageName}"
                : $"Install 'Genies Avatar SDK'\n{packageName}";

            var buttonColor = (!GeniesAvatarSdkInstalled && !IsInstallingGeniesSdkAvatar)
                ? _primaryActionColor
                : GUI.backgroundColor;

            if (DrawCenteredColoredButton(buttonText, buttonColor, 45, 500))
            {
                InstallGeniesSdkAvatar();
            }
            EditorGUI.EndDisabledGroup();

            if (!AllPrerequisitesMet)
            {
                EditorGUILayout.HelpBox("It is recommended to configure all prerequisites before installing 'Genies Avatar SDK'.", MessageType.Warning);
            }
            else if (GeniesAvatarSdkInstalled)
            {
                EditorGUILayout.HelpBox("Genies Avatar SDK is installed. Complete the Quickstart steps below to finish setup.", MessageType.Info);
            }
            else if (!IsInstallingGeniesSdkAvatar)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    $"\nREQUIRED: The target SDK package ({GeniesSdkPrerequisiteChecker.GetPackageName()}) is NOT installed.\n\n" +
                    "The core packages should NEVER be used without the SDK package installed.\n" +
                    "Please click the button above to install it now.\n",
                    MessageType.Error);
                EditorGUILayout.Space(5);
            }

            if (IsInstallingGeniesSdkAvatar)
            {
                EditorGUILayout.HelpBox("Installing 'Genies Avatar SDK' package...", MessageType.Info);
            }

            DrawDivider();
#endif

            // === SECTION 2 or 3: Required Setup (after SDK is installed) ===
            if (GeniesAvatarSdkInstalled)
            {
                int subsection = 1;

                // Auth credentials section
                DrawAuthCredentialsSection(currentSection, subsection++);

                EditorGUILayout.Space(10);

                DrawDivider();
            }

#if GENIES_AVATARSDK_CLIENT
            if (!AllPrerequisitesMet)
            {
                EditorGUILayout.HelpBox("It is recommended to configure all prerequisites before using the Genies Avatar SDK.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("All prerequisites met! Complete the Quickstart steps below to finish setup.", MessageType.Info);
            }

            DrawDivider();
#endif

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();

            // Capture the content rect before ending the scroll view
            var contentRect = GUILayoutUtility.GetLastRect();

            EditorGUILayout.EndScrollView();

            // Capture the actual scroll view rect (valid during Repaint) for correct overlay positioning
            var actualScrollViewRect = GUILayoutUtility.GetLastRect();

            // === FIXED FOOTER SECTION (always visible) ===
            DrawFixedFooter();

            // Draw scroll indicator last so it renders on top of all other elements
            DrawScrollIndicator(contentRect, actualScrollViewRect);
        }

        private void DrawPrerequisiteCheck(string label, bool isConfigured, System.Action fixAction, string tooltip = "", string buttonLabel = "Configure")
        {
            EditorGUILayout.BeginHorizontal();

            // Status icon
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            iconStyle.normal.textColor = isConfigured ? Color.green : Color.red;
            EditorGUILayout.LabelField(isConfigured ? "✓" : "✗", iconStyle, GUILayout.Width(20));

            // Label with tooltip
            var labelContent = new GUIContent(label, tooltip);
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Configure button
            EditorGUI.BeginDisabledGroup(isConfigured);
            var buttonTooltip = isConfigured ? "Already configured" : $"Click to configure this prerequisite";
            var buttonContent = new GUIContent(buttonLabel, buttonTooltip);
            if (GUILayout.Button(buttonContent, GUILayout.Width(70)))
            {
                fixAction?.Invoke();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawPlatformPrerequisiteCheck(string label, bool isActivePlatformConfigured, bool areAllPlatformsConfigured, System.Action fixAllPlatformsAction, string tooltip = "")
        {
            EditorGUILayout.BeginHorizontal();

            // Status icon for active platform
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            iconStyle.normal.textColor = isActivePlatformConfigured ? Color.green : Color.red;
            EditorGUILayout.LabelField(isActivePlatformConfigured ? "✓" : "✗", iconStyle, GUILayout.Width(20));

            // Label with tooltip
            var labelContent = new GUIContent(label, tooltip);
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Configure button (applies to all platforms)
            EditorGUI.BeginDisabledGroup(areAllPlatformsConfigured);
            var buttonTooltip = areAllPlatformsConfigured ? "All platforms already configured" : "Configure setting for all supported build targets";
            var buttonContent = new GUIContent("Configure", buttonTooltip);
            if (GUILayout.Button(buttonContent, GUILayout.Width(70)))
            {
                fixAllPlatformsAction?.Invoke();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
        private void DrawAndroidEntryPointCheck()
        {
            EditorGUILayout.BeginHorizontal();

            // Determine status
            var status = AndroidEntryPointStatus;
            bool isValid = status == GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.Valid;
            bool isWarning = status == GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.Warning_BothEnabled;
            bool isError = status == GeniesSdkPrerequisiteChecker.AndroidEntryPointStatus.Error_GameActivityOnly;

            // Status icon
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            if (isValid)
            {
                iconStyle.normal.textColor = Color.green;
            }
            else if (isWarning)
            {
                iconStyle.normal.textColor = new Color(1.0f, 0.65f, 0.0f); // Orange/yellow for warning
            }
            else
            {
                iconStyle.normal.textColor = Color.red;
            }

            string statusIcon = isValid ? "✓" : (isWarning ? "⚠" : "✗");
            EditorGUILayout.LabelField(statusIcon, iconStyle, GUILayout.Width(20));

            // Label with current status suffix
            string currentSetting = isValid ? "Activity" : (isWarning ? "Both" : "GameActivity");
            var labelContent = new GUIContent(
                $"Application Entry Point (Current: {currentSetting})",
                "For the Genies SDK, 'Activity' must be enabled. 'GameActivity' alone will cause build failures. " +
                "Having both enabled is only valid for development builds.");
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Fix/Open Settings button
            EditorGUI.BeginDisabledGroup(isValid);
            var buttonText = isWarning ? "Settings" : "Configure";
            var buttonTooltip = isValid
                ? "Application Entry Point is correctly configured"
                : isWarning
                    ? "Open Player Settings to review Application Entry Point configuration"
                    : "Click to enable Activity (required for SDK builds)";
            var buttonContent = new GUIContent(buttonText, buttonTooltip);
            if (GUILayout.Button(buttonContent, GUILayout.Width(isWarning ? 65 : 50)))
            {
                if (isWarning)
                {
                    // For warnings (both enabled), open Player Settings
                    // The Application Entry Point setting is under "Other Settings" in the Android tab
                    SettingsService.OpenProjectSettings("Project/Player");
                }
                else
                {
                    // For errors (GameActivity only), apply the fix
                    FixAndroidEntryPoint();
                    RefreshPrerequisiteStatus();
                }
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Warning/Error helpboxes based on current configuration
            string helpBoxMessage = null;
            MessageType helpBoxType = MessageType.None;

            if (isError)
            {
                helpBoxMessage = "Application Entry Point is set to 'GameActivity' only. This will cause build failures with the Genies SDK. " +
                    "Click 'Configure' to enable 'Activity'.";
                helpBoxType = MessageType.Error;
            }
            else if (isWarning)
            {
                helpBoxMessage = "Both 'Activity' and 'GameActivity' are enabled. This works for development builds but may cause issues in production.\n\n" +
                    "For production builds, disable 'GameActivity' and keep only 'Activity' enabled.\n\n" +
                    "Use the 'Settings' button above to open Player Settings, then navigate to:\n" +
                    "Player > Android > Other Settings > Application Entry Point";
                helpBoxType = MessageType.Warning;
            }

            // Display helpbox if there's a message
            if (!string.IsNullOrEmpty(helpBoxMessage))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox(helpBoxMessage, helpBoxType);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }
#endif

        private void DrawActiveInputHandlingCheck()
        {
            EditorGUILayout.BeginHorizontal();

            // Check current input handler value
            int currentInputHandler = GeniesSdkPrerequisiteChecker.GetActiveInputHandlerValue();
            bool isSetToOld = currentInputHandler == 0;
            bool isSetToNew = currentInputHandler == 1;
            bool isSetToBoth = currentInputHandler == 2;

            // Status icon - green if new, yellow if old, red if both
            var iconStyle = new GUIStyle(GUI.skin.label);
            iconStyle.fontSize = 16;
            if (isSetToNew)
            {
                iconStyle.normal.textColor = Color.green;
            }
            else if (isSetToOld)
            {
                iconStyle.normal.textColor = new Color(1.0f, 0.65f, 0.0f); // Orange/yellow for warning
            }
            else
            {
                iconStyle.normal.textColor = Color.red;
            }

            string statusIcon = isSetToNew ? "✓" : (isSetToOld ? "⚠" : "✗");
            EditorGUILayout.LabelField(statusIcon, iconStyle, GUILayout.Width(20));

            // Label with tooltip - include current setting suffix
            string currentSetting = isSetToNew ? "New" : (isSetToOld ? "Old" : (isSetToBoth ? "Both" : "Unknown"));
            var labelContent = new GUIContent(
                $"Active Input Handling (Current: {currentSetting})",
                "The new Input System is STRONGLY RECOMMENDED for all projects. " +
                "On Android, 'Both' is not allowed as it causes build errors.");
            EditorGUILayout.LabelField(labelContent, GUILayout.ExpandWidth(true));

            // Use Old Input System button - disabled if already set to old OR new
            EditorGUI.BeginDisabledGroup(isSetToOld || isSetToNew);
            var useOldTooltip = isSetToOld
                ? "Already set to Input Manager (Old)"
                : isSetToNew
                    ? "Already configured to use the new Input System. Setting to 'Old' only is discouraged. You may do so manually through Project Settings."
                    : "Set to Input Manager (Old) - Legacy input system";
            var useOldContent = new GUIContent("Use Old", useOldTooltip);
            if (GUILayout.Button(useOldContent, GUILayout.Width(70)))
            {
                FixActiveInputHandlingToOld();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();

#if !UNITY_ANDROID
            // Use Both Input Systems button - only available on non-Android platforms, disabled if already set to both
            EditorGUI.BeginDisabledGroup(isSetToBoth);
            var useBothTooltip = isSetToBoth
                ? "Already set to Both"
                : "Enable both Input Manager and Input System (not recommended, Android incompatible)";
            var useBothContent = new GUIContent("Use Both", useBothTooltip);
            if (GUILayout.Button(useBothContent, GUILayout.Width(75)))
            {
                FixActiveInputHandlingToBoth();
                RefreshPrerequisiteStatus();
            }
            EditorGUI.EndDisabledGroup();
#endif

            // Use New Input System button - disabled if already set to new
            EditorGUI.BeginDisabledGroup(isSetToNew);
            var originalColor = GUI.backgroundColor;
            if (!isSetToNew)
            {
                GUI.backgroundColor = _highlightColor;
            }

            var useNewTooltip = isSetToNew
                ? "Already set to Input System Package (New)"
                : "RECOMMENDED: Set to Input System Package (New) - Modern, feature-rich input system";
            var useNewContent = new GUIContent("Use New ★", useNewTooltip);
            if (GUILayout.Button(useNewContent, GUILayout.Width(85)))
            {
                FixActiveInputHandlingToNew();
                RefreshPrerequisiteStatus();
            }

            GUI.backgroundColor = originalColor;
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // Warning/Error helpboxes based on current configuration
            string helpBoxMessage = null;
            MessageType helpBoxType = MessageType.None;

            if (isSetToOld)
            {
                // Old input system - warning on all platforms
                helpBoxMessage = "️You are using the legacy Input Manager (Old). The new Input System is STRONGLY RECOMMENDED for better functionality, performance, and future compatibility. Click 'Use New ★' to upgrade.";
                helpBoxType = MessageType.Warning;
            }
            else if (isSetToBoth)
            {
#if UNITY_ANDROID
                // Both enabled on Android - critical error
                helpBoxMessage = "Active Input Handling is set to 'Both' which will cause Android build errors. You must choose either 'Use New ★' (recommended) or 'Use Old'.";
                helpBoxType = MessageType.Error;
#else
                // Both enabled on non-Android - warning about Android compatibility
                helpBoxMessage = "Active Input Handling is set to 'Both'. While this works on your current platform, it will cause build errors if you target Android. We recommend using only the new Input System. Click 'Use New ★'.";
                helpBoxType = MessageType.Warning;
#endif
            }

            // Display helpbox if there's a message
            if (!string.IsNullOrEmpty(helpBoxMessage))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(20);
                EditorGUILayout.HelpBox(helpBoxMessage, helpBoxType);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space(5);
            }
        }

        private void DrawScrollIndicator(Rect contentRect, Rect scrollViewRect)
        {
            // Fall back to a layout estimate if the captured rect is not yet valid
            if (scrollViewRect.height <= 0)
            {
                var headerHeight = 150f;
                var footerHeight = 80f;
                var margin = 20f;
                scrollViewRect = new Rect(
                    margin,
                    headerHeight,
                    position.width - (margin * 2),
                    position.height - headerHeight - footerHeight - margin
                );
            }

            // Calculate content height from the content rect
            var contentHeight = contentRect.height; // The height of the content area

            // Use the scroll view rect height as the visible height
            var visibleHeight = scrollViewRect.height;

            // Calculate if there's more content below the current visible area
            var currentBottomPosition = ScrollPosition.y + visibleHeight;
            bool hasMoreContent = currentBottomPosition < contentHeight - 10f; // 10f buffer

            if (hasMoreContent)
            {
                // Position the indicator as an overlay well inside the scroll view area
                var indicatorRect = new Rect(
                    scrollViewRect.x + 10,                    // Inside scroll view with margin
                    scrollViewRect.y + scrollViewRect.height - 50, // Near bottom of scroll view
                    scrollViewRect.width - 20,                // Account for margins
                    35                                        // Height
                );

                // Use immediate GUI for overlay drawing
                if (Event.current.type is EventType.Repaint)
                {
                    // Draw a more prominent gradient background
                    var backgroundColor = new Color(0.2f, 0.4f, 0.6f, 0.9f); // More blue tint, higher opacity
                    GUI.DrawTexture(indicatorRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, backgroundColor, 0, 0);

                    // Draw fade effect from top to bottom of the indicator
                    for (int i = 0; i < indicatorRect.height; i++)
                    {
                        var lineRect = new Rect(indicatorRect.x, indicatorRect.y + i, indicatorRect.width, 1);
                        var alpha = (indicatorRect.height - i) / indicatorRect.height * 0.9f;
                        var fadeColor = new Color(0.2f, 0.4f, 0.6f, alpha);
                        GUI.DrawTexture(lineRect, Texture2D.whiteTexture, ScaleMode.StretchToFill, false, 0, fadeColor, 0, 0);
                    }

                    // Draw the scroll indicator text with better contrast
                    var style = new GUIStyle(EditorStyles.miniLabel);
                    style.alignment = TextAnchor.MiddleCenter;
                    style.normal.textColor = new Color(1f, 1f, 1f, 1f); // Pure white for better contrast
                    style.fontSize = 11;
                    style.fontStyle = FontStyle.Bold; // Make it bold instead of italic

                    GUI.Label(indicatorRect, "▼ Scroll for more ▼", style);
                }
            }
        }

        private void DrawPlayModeInterface()
        {
            // Check if there are unmet requirements to show appropriate messaging
            bool hasUnmetPrerequisites = !AllPrerequisitesMet;
            bool hasUnmetCredentials = !AuthCredentialsConfigured;
            bool hasAnyUnmetRequirements = hasUnmetPrerequisites || hasUnmetCredentials;

            if (hasAnyUnmetRequirements)
            {
                var message = "SDK REQUIREMENTS NOT MET\n\n" +
                    "The Genies SDK may not function properly because some requirements are not configured:\n";

                if (hasUnmetPrerequisites)
                {
                    message += "\n• Build Settings: Some project prerequisites are not configured.";
                }

                if (hasUnmetCredentials)
                {
                    message += "\n• API Credentials: Client ID and Client Secret are missing or invalid.";
                }

                message += "\n\nPlease exit Play mode to review and configure the missing requirements.";

                EditorGUILayout.HelpBox(message, MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "The Genies SDK Bootstrap Wizard is disabled while in Play mode.\n\n" +
                    "All requirements are met. Please exit Play mode to modify settings.",
                    MessageType.Info);
            }

            EditorGUILayout.Space(10);

            if (GUILayout.Button("Exit Play Mode"))
            {
                EditorApplication.isPlaying = false;
            }

            DrawDivider();

            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            // === FIXED FOOTER SECTION (always visible) ===
            DrawFixedFooter();
        }

        private void DrawSectionHeader(string title, string subtitle)
        {
            var sectionStyle = new GUIStyle(EditorStyles.boldLabel);
            sectionStyle.fontSize = 14;
            EditorGUILayout.LabelField(title, sectionStyle);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleStyle = new GUIStyle(EditorStyles.label);
                subtitleStyle.fontSize = 11;
                subtitleStyle.normal.textColor = new Color(0.5f, 0.5f, 0.5f, 1f);
                EditorGUILayout.LabelField(subtitle, subtitleStyle);
            }
        }

        private void DrawDivider()
        {
            EditorGUILayout.Space(10);
            var rect = EditorGUILayout.GetControlRect(false, 1);
            rect.height = 1;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            EditorGUILayout.Space(10);
        }

        private bool DrawButtonWithDescription(string buttonText, string descriptionText, Color buttonColor, float buttonHeight = 35)
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space(5);

            bool clicked = DrawCenteredColoredButton(buttonText, buttonColor, buttonHeight);

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            var descStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, alignment = TextAnchor.UpperCenter };
            EditorGUILayout.LabelField(descriptionText, descStyle);
            GUILayout.Space(10);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();

            return clicked;
        }

        private void DrawCreateAccountSection(int parentSection, int subsection)
        {
            EditorGUILayout.LabelField($"{parentSection}.{subsection} Create Account & App", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            EditorGUILayout.LabelField(
                "Create a Genies account and app to link your Unity project and receive API credentials.",
                infoStyle);

            EditorGUILayout.Space(8);

            // Button to open Developer Portal
            if (DrawButtonWithDescription(
                "Open Developer Portal ↗",
                "Sign up or log in to the Developer Portal, then create a new app to obtain your Client ID and Client Secret.",
                _externalLinkColor))
            {
                MenuItems.ExternaLinks.OpenGeniesHub();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawAuthCredentialsSection(int parentSection, int subsection)
        {
            EditorGUILayout.LabelField($"{parentSection}.{subsection} Configure API Credentials", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.HelpBox(
                "Want to try the SDK first? Hit \"Import Sample Scenes\" at the bottom — demo mode works without credentials.",
                MessageType.Warning);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            EditorGUILayout.LabelField(
                "Client ID and Client Secret are required to use some of our APIs.",
                infoStyle);

            EditorGUILayout.Space(8);

            // Status row
            EditorGUILayout.BeginHorizontal();
            var iconStyle = new GUIStyle(GUI.skin.label) { fontSize = 16 };
            iconStyle.normal.textColor = AuthCredentialsConfigured ? Color.green : Color.red;
            EditorGUILayout.LabelField(AuthCredentialsConfigured ? "✓" : "✗", iconStyle, GUILayout.Width(20));

            string statusText = AuthCredentialsConfigured
                ? "Credentials configured and well-formed."
                : "Credentials missing or invalid.";
            EditorGUILayout.LabelField(statusText, GUILayout.ExpandWidth(true));

            var originalColor = GUI.backgroundColor;
            GUI.backgroundColor = _secondaryActionColor;

            if (GUILayout.Button("Open Auth Settings", GUILayout.Width(150)))
            {
                SettingsService.OpenProjectSettings("Project/Genies/Auth Settings");
            }

            GUI.backgroundColor = originalColor;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Editable fields directly in wizard
            EditorGUI.BeginChangeCheck();
            WizardClientId = EditorGUILayout.TextField("Client ID", WizardClientId);

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel("Client Secret");
                WizardClientSecret = EditorGUILayout.PasswordField(WizardClientSecret);
            }

            if (EditorGUI.EndChangeCheck())
            {
                // Persist draft as user types (focus + domain reload safe)
                SessionState.SetString(DraftClientIdKey, WizardClientId ?? "");
                SessionState.SetString(DraftClientSecretKey, WizardClientSecret ?? "");
            }

            EditorGUILayout.Space(5);

            // Actions row: Save + docs link
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var credentialsButtonColor = GUI.backgroundColor;
            GUI.backgroundColor = _primaryActionColor;

            if (GUILayout.Button("Save Credentials", GUILayout.Width(140)))
            {
                if (string.IsNullOrWhiteSpace(WizardClientId) ||
                    string.IsNullOrWhiteSpace(WizardClientSecret))
                {
                    EditorUtility.DisplayDialog(
                        "Missing Credentials",
                        "Both Client ID and Client Secret are required before saving.",
                        "OK");
                }
                else if (!GeniesBootstrapAuthLocalValidator.LooksLikeValidPair(WizardClientId, WizardClientSecret))
                {
                    EditorUtility.DisplayDialog(
                        "Credentials Invalid",
                        "The Client ID or Client Secret does not match the expected format.\n\n" +
                        "Please check for typos before saving.",
                        "OK");
                }
                else
                {
                    SaveJsonToResources(WizardClientId, WizardClientSecret);

                    AuthCredentialsConfigured = true;

                    // Clear drafts on successful save
                    SessionState.EraseString(DraftClientIdKey);
                    SessionState.EraseString(DraftClientSecretKey);

                    CredentialsSet?.Invoke();
                    EditorUtility.DisplayDialog(
                        "Credentials Saved",
                        "Genies API credentials were saved successfully.",
                        "OK");
                }
            }

            GUI.backgroundColor = _externalLinkColor;

            if (GUILayout.Button("Sign Up ↗", GUILayout.Width(140)))
            {
                _externalLinks.OpenGeniesHub();
            }

            GUI.backgroundColor = credentialsButtonColor;
            EditorGUILayout.EndHorizontal();

            if (!AuthCredentialsConfigured)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.HelpBox(
                    "Credentials are not configured or appear invalid. " +
                    "Please provide a valid Client ID and Client Secret.",
                    MessageType.Error);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawImportSamplesSection()
        {
            //EditorGUILayout.LabelField($"{parentSection}.{subsection} Import Samples", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            EditorGUILayout.LabelField(
                "Import sample scenes and scripts to help you get started with the Genies SDK.",
                infoStyle);

            EditorGUILayout.Space(8);

            // Recommendation for getting started
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(5);
            var recommendStyle = new GUIStyle(EditorStyles.label) { wordWrap = true, fontStyle = FontStyle.Italic };
            EditorGUILayout.LabelField(
                "💡 Recommended: Start with the 'AvatarStartScene' sample to learn the basics of the Genies SDK.",
                recommendStyle);
            GUILayout.Space(5);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Button to open Package Manager
            var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
            if (DrawButtonWithDescription(
                "Open Package Manager to Import Samples",
                "This will open the Unity Package Manager window where you can view and import available samples.",
                _externalLinkColor))
            {
                OpenPackageInPackageManager(packageName);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawDocumentationSection(int parentSection, int subsection)
        {
            EditorGUILayout.LabelField($"{parentSection}.{subsection} Tutorials", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            EditorGUILayout.LabelField(
                "Explore tutorials and guides to learn more about the Genies SDK.",
                infoStyle);

            EditorGUILayout.Space(8);

            // Sample scenes documentation link
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(12));
            if (EditorGUILayout.LinkButton("Sample Scenes Documentation"))
            {
                _externalLinks.OpenSampleScenesDocumentation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // First project tutorial link
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(12));
            if (EditorGUILayout.LinkButton("First Project Tutorial"))
            {
                _externalLinks.OpenFirstProjectTutorial();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }

        private void OpenPackageInPackageManager(string packageName)
        {
            ImportAllSamplesHighlightAndOpenScene();
        }

        private void DrawQuickLinks(int sectionNumber = 0)
        {
            if (sectionNumber > 0)
            {
                DrawSectionHeader($"{sectionNumber}. Quick Links", "Access key resources and support");
            }
            else
            {
                EditorGUILayout.LabelField("Quick Links", EditorStyles.boldLabel);
            }
            EditorGUILayout.Space(5);

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var infoStyle = new GUIStyle(EditorStyles.label) { wordWrap = true };
            EditorGUILayout.LabelField(
                "Important links for your development journey with the Genies SDK.",
                infoStyle);

            EditorGUILayout.Space(8);

            // Developer Portal
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(12));
            if (EditorGUILayout.LinkButton("Developer Portal"))
            {
                MenuItems.ExternaLinks.OpenGeniesHub();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Technical Documentation
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(12));
            if (EditorGUILayout.LinkButton("Technical Documentation"))
            {
                MenuItems.ExternaLinks.OpenGeniesTechnicalDocumentation();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Genies Support
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("•", GUILayout.Width(12));
            if (EditorGUILayout.LinkButton("Genies Support"))
            {
                MenuItems.ExternaLinks.OpenGeniesSupport();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.EndVertical();
        }

        private bool DrawCenteredColoredButton(string text, Color buttonColor, float height, float maxWidth = 450)
        {
            var originalColor = GUI.backgroundColor;

            var boldButtonStyle = new GUIStyle(GUI.skin.button);
            boldButtonStyle.fontStyle = FontStyle.Bold;
            boldButtonStyle.fontSize = 12;

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            // Draw border/outline
            var buttonRect = GUILayoutUtility.GetRect(new GUIContent(text), boldButtonStyle,
                GUILayout.Height(height), GUILayout.MaxWidth(maxWidth));

            // Draw a darker border around the button
            var borderColor = new Color(buttonColor.r * 0.6f, buttonColor.g * 0.6f, buttonColor.b * 0.6f, 1f);
            var borderRect = new Rect(buttonRect.x - 2, buttonRect.y - 2, buttonRect.width + 4, buttonRect.height + 4);
            EditorGUI.DrawRect(borderRect, borderColor);

            // Draw the button
            GUI.backgroundColor = buttonColor;
            bool clicked = GUI.Button(buttonRect, text, boldButtonStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUI.backgroundColor = originalColor;
            return clicked;
        }

        private void DrawFixedFooter()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(20);
            EditorGUILayout.BeginVertical();

            DrawDivider();
            EditorGUILayout.BeginHorizontal();
            // Left: checkboxes
            DrawAutoShowWizardCheckbox();

            if (GeniesAvatarSdkInstalled)
            {
                EditorGUILayout.BeginVertical();
                var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
                if (DrawButtonWithDescription(
                        "Import Sample Scenes",
                        "Imports the Avatar SDK sample scenes. We will open the Demo Mode scene for you if you have not embedded your credentials, or the Avatar Starter scene if you have embedded your credentials.",
                        _externalLinkColor))
                {
                    OpenPackageInPackageManager(packageName);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.BeginVertical();
            if (DrawButtonWithDescription(
                    "Technical Documentation ↗",
                    "Open our comprehensive technical documentation for in-depth guides, API references, and best practices to help you make the most of the Genies SDK.",
                    _externalLinkColor))
            {
                MenuItems.ExternaLinks.OpenGeniesTechnicalDocumentation();
            }

            EditorGUILayout.EndVertical();


            // Right: Support button with description
            EditorGUILayout.BeginVertical();
            if (DrawButtonWithDescription(
                    "Genies Support ↗",
                    "Submit tickets, browse FAQs, and get assistance from our technical support team.",
                    _primaryActionColor))
            {
                MenuItems.ExternaLinks.OpenGeniesSupport();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.EndVertical();
            GUILayout.Space(20);
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAutoShowWizardCheckbox()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(5);

            // Section header
            var headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 };
            EditorGUILayout.LabelField("Wizard Display Settings", headerStyle);
            EditorGUILayout.Space(3);

            // Show wizard on startup checkbox (true by default)
            bool showWizardOnStartup = GetUserSettingBool(ShowWizardOnStartupPrefKey, true);
            bool newShowWizardOnStartup = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Show wizard on startup",
                    "When enabled, this wizard will automatically appear when Unity Editor first opens. " +
                    "Only runs once per editor session, not on each recompile."),
                showWizardOnStartup);

            if (newShowWizardOnStartup != showWizardOnStartup)
            {
                SetUserSettingBool(ShowWizardOnStartupPrefKey, newShowWizardOnStartup);
            }

            EditorGUILayout.Space(3);

            // Check prerequisites on load checkbox (true by default)
            bool checkPrerequisitesOnLoad = GetUserSettingBool(CheckPrerequisitesOnLoadPrefKey, true);
            bool newCheckPrerequisitesOnLoad = EditorGUILayout.ToggleLeft(
                new GUIContent(
                    "Check prerequisites on load",
                    "When enabled, prerequisites will be checked on each recompile/domain reload. " +
                    "If prerequisites are not met, the wizard will automatically appear to help you configure them."),
                checkPrerequisitesOnLoad);

            if (newCheckPrerequisitesOnLoad != checkPrerequisitesOnLoad)
            {
                SetUserSettingBool(CheckPrerequisitesOnLoadPrefKey, newCheckPrerequisitesOnLoad);
            }

            // Show note if either setting is disabled
            if (!newShowWizardOnStartup || !newCheckPrerequisitesOnLoad)
            {
                EditorGUILayout.Space(5);
                var noteStyle = new GUIStyle(EditorStyles.miniLabel) { wordWrap = true };

                if (!newShowWizardOnStartup && !newCheckPrerequisitesOnLoad)
                {
                    EditorGUILayout.LabelField(
                        "Note: The wizard will not appear automatically. Warnings will be logged to the console if issues are detected. " +
                        "Open manually via Tools > Genies > SDK Bootstrap Wizard.",
                        noteStyle);
                }
                else if (!newShowWizardOnStartup)
                {
                    EditorGUILayout.LabelField(
                        "Note: The wizard will not appear on editor startup, but will still show if prerequisites are not met during recompile.",
                        noteStyle);
                }
                else if (!newCheckPrerequisitesOnLoad)
                {
                    EditorGUILayout.LabelField(
                        "Note: Prerequisites will not be checked on recompile. Warnings will be logged if issues are detected.",
                        noteStyle);
                }
            }

            EditorGUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }


        private void RefreshPrerequisiteStatus()
        {
            if (RefreshScheduled)
            {
                return;
            }
            RefreshScheduled = true;

            EditorApplication.delayCall += () =>
            {
                RefreshScheduled = false;

                CheckPlatformSupport();

                // Scripting backend
                CheckIL2CPPBackend();
                CheckIL2CPPBackendAllPlatforms();

                // .NET Framework
                CheckNetFramework();
                CheckNetFrameworkAllPlatforms();

                // Vulkan
                CheckVulkanForWindows();
                CheckVulkanForAndroid();

                // Android
                CheckArm64ForAndroid();
                CheckMinAndroidApiLevel();
                CheckAndroidEntryPoint();

                // Input handling
                CheckActiveInputHandling();

                // TextMesh Pro Essentials
                CheckTMPEssentials();

                // SDK installation
                CheckGeniesAvatarSdkInstalled();

                // Credentials
                CheckAuthCredentials();

                Repaint();
            };
        }

        private void CheckPlatformSupport()
        {
            IsBuildTargetSupported = GeniesSdkPrerequisiteChecker.IsActiveBuildTargetSupported();
            IsPlatformSupported = GeniesSdkPrerequisiteChecker.IsActivePlatformSupported();
        }

        private void CheckIL2CPPBackend()
        {
            Il2CppBackendConfigured = GeniesSdkPrerequisiteChecker.IsIL2CPPConfiguredForActivePlatform();
        }

        private void CheckIL2CPPBackendAllPlatforms()
        {
            Il2CppBackendConfiguredAllPlatforms = GeniesSdkPrerequisiteChecker.IsIL2CPPConfiguredForAllPlatforms();
        }

        private void CheckNetFramework()
        {
            var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
            NetFrameworkConfigured = GeniesSdkPrerequisiteChecker.IsNetFrameworkConfiguredForActivePlatform();

            if (!NetFrameworkConfigured)
            {
                ActivePlatformSupportsNetFramework = GeniesSdkPrerequisiteChecker.IsPlatformSupported(activeBuildTargetGroup);
                if (!ActivePlatformSupportsNetFramework)
                {
                    var supportedPlatforms = string.Join(", ", System.Array.ConvertAll(
                        GeniesSdkPrerequisiteChecker.GetSupportedPlatforms(),
                        p => GeniesSdkPrerequisiteChecker.GetPlatformDisplayName(p)));
                    PlatformCompatibilityError = $"The active platform ({activeBuildTargetGroup}) is not supported by the Genies SDK. Please switch to a compatible platform: {supportedPlatforms}.";
                }
                else
                {
                    PlatformCompatibilityError = "";
                }
            }
            else
            {
                ActivePlatformSupportsNetFramework = true;
                PlatformCompatibilityError = "";
            }
        }

        private void CheckNetFrameworkAllPlatforms()
        {
            NetFrameworkConfiguredAllPlatforms = GeniesSdkPrerequisiteChecker.IsNetFrameworkConfiguredForAllPlatforms();
        }

        private void CheckGeniesAvatarSdkInstalled()
        {
            GeniesAvatarSdkInstalled = GeniesSdkPrerequisiteChecker.IsSdkInstalled();
        }

        private void CheckVulkanForWindows()
        {
            VulkanConfiguredForWindows = GeniesSdkPrerequisiteChecker.IsVulkanConfiguredForWindows();
        }

        private void CheckVulkanForAndroid()
        {
            VulkanConfiguredForAndroid = GeniesSdkPrerequisiteChecker.IsVulkanConfiguredForAndroid();
        }

        private void CheckArm64ForAndroid()
        {
            Arm64ConfiguredForAndroid = GeniesSdkPrerequisiteChecker.IsArm64ConfiguredForAndroid();
        }

        private void CheckMinAndroidApiLevel()
        {
            MinAndroidApiLevelConfigured = GeniesSdkPrerequisiteChecker.IsMinAndroidApiLevelConfigured();
        }

        private void CheckAndroidEntryPoint()
        {
            AndroidEntryPointStatus = GeniesSdkPrerequisiteChecker.GetAndroidEntryPointStatus();
        }

        private void CheckActiveInputHandling()
        {
            ActiveInputHandlingConfigured = GeniesSdkPrerequisiteChecker.IsActiveInputHandlingConfigured();
        }

        private void CheckTMPEssentials()
        {
            TMPEssentialsImported = GeniesSdkPrerequisiteChecker.IsTMPEssentialsImported();
        }

        private static bool TryGetValidAuthCredentials(out GeniesBootstrapAuthSettings authSettings)
        {
            authSettings = null;

            try
            {
                authSettings = GeniesBootstrapAuthSettings.LoadFromResources();
                if (authSettings == null)
                {
                    return false;
                }

                return GeniesBootstrapAuthLocalValidator.LooksLikeValidPair(
                    authSettings.ClientId,
                    authSettings.ClientSecret);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }

        private bool CheckAuthCredentials()
        {
            AuthCredentialsConfigured = TryGetValidAuthCredentials(out GeniesBootstrapAuthSettings settings);

            if (settings != null)
            {
                // Only populate fields if user hasn't started typing
                if (string.IsNullOrWhiteSpace(WizardClientId) && string.IsNullOrWhiteSpace(WizardClientSecret))
                {
                    WizardClientId = settings.ClientId ?? string.Empty;
                    WizardClientSecret = settings.ClientSecret ?? string.Empty;

                    // Keep SessionState draft in sync with what's on disk
                    SessionState.SetString(DraftClientIdKey, WizardClientId);
                    SessionState.SetString(DraftClientSecretKey, WizardClientSecret);
                }
            }
            else
            {
                if (string.IsNullOrEmpty(WizardClientId) && string.IsNullOrEmpty(WizardClientSecret))
                {
                    WizardClientId = string.Empty;
                    WizardClientSecret = string.Empty;
                }
            }

            return AuthCredentialsConfigured;
        }

#if !GENIES_AVATARSDK_CLIENT
        private async void SearchAndInstallPackage(string packageName, Action<AddRequest> onPackageInstallComplete)
        {
            var progress = 0f;
            var progressIncrement = ProgressUpdateInterval * 0.001f;

            var search = Client.Search(packageName);

            while (search.IsCompleted is false)
            {
                EditorUtility.DisplayProgressBar($"Fetching {packageName}", "", Math.Min(progress, 0.9f));
                await Task.Delay(ProgressUpdateInterval);
                progress += progressIncrement;
            }
            EditorUtility.ClearProgressBar();

            if (search.Error is not null)
            {
                Debug.LogError($"Failed to fetch {packageName}: {search.Error}");
                EditorUtility.DisplayDialog("Failed to fetch package", $"Failed to fetch {packageName}: {search.Error}", "OK");
                return;
            }

            progress = 0f;
            EditorUtility.DisplayProgressBar($"Installing {packageName}", "", Math.Min(progress, 0.9f));
            var request = Client.Add(packageName);

            EditorApplication.update -= OnComplete;
            EditorApplication.update += OnComplete;

            while (request.IsCompleted is false)
            {
                EditorUtility.DisplayProgressBar($"Installing {packageName}", "", Math.Min(progress, 0.9f));
                await Task.Delay(ProgressUpdateInterval);
                progress += progressIncrement;
            }

            void OnComplete()
            {
                if (request.IsCompleted)
                {
                    EditorApplication.update -= OnComplete;

                    EditorUtility.ClearProgressBar();
                    onPackageInstallComplete?.Invoke(request);
                }
            }
        }
#endif

        private void FixAllAutoFixablePrerequisites()
        {
            // Fix IL2CPP for all platforms
            if (!Il2CppBackendConfiguredAllPlatforms)
            {
                FixIl2CppBackendAllPlatforms();
            }

            // Fix .NET Framework for all platforms
            if (!NetFrameworkConfiguredAllPlatforms)
            {
                FixNetFrameworkAllPlatforms();
            }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            // Fix Vulkan for Windows
            if (!VulkanConfiguredForWindows)
            {
                FixVulkanForWindows();
            }
#endif

#if UNITY_ANDROID
            // Fix Vulkan for Android
            if (!VulkanConfiguredForAndroid)
            {
                FixVulkanForAndroid();
            }

            // Fix ARM64 for Android
            if (!Arm64ConfiguredForAndroid)
            {
                FixArm64ForAndroid();
            }

            // Fix minimum Android API level
            if (!MinAndroidApiLevelConfigured)
            {
                FixMinAndroidApiLevel();
            }

#if UNITY_6000_0_OR_NEWER
            // Fix Android Application Entry Point (Unity 6+)
            if (!IsAndroidEntryPointValid)
            {
                FixAndroidEntryPoint();
            }
#endif
#endif

            RefreshPrerequisiteStatus();
            Debug.Log("[Genies SDK Bootstrap] Applied all auto-configurable prerequisites.");
        }

        private void FixIl2CppBackendAllPlatforms()
        {
            FixPlatformSetting(
                activePlatformOnly: false,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetScriptingBackend(namedBuildTarget, ScriptingImplementation.IL2CPP);
                },
                settingName: "IL2CPP scripting backend"
            );
        }

        private void FixNetFrameworkAllPlatforms()
        {
            FixPlatformSetting(
                activePlatformOnly: false,
                setPlatformSetting: (group) =>
                {
                    var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                    PlayerSettings.SetApiCompatibilityLevel(namedBuildTarget, ApiCompatibilityLevel.NET_Unity_4_8);
                },
                settingName: ".NET Framework 4.8"
            );
        }

        private void FixVulkanForWindows()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var graphicsApis = new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan };
                PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64, graphicsApis);
                EnsureSettingsAreSaved();
                Debug.Log("Vulkan graphics API configured for Windows Standalone builds.");

                // Show warning prompt to restart editor
                PromptEditorRestart(
                    "Graphics API Changed",
                    "Changing this setting requires restarting the Unity Editor for the update to take effect.\n\nNote: For convenience, you can apply other setting changes before restarting. Many settings changes may also require a restart.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Vulkan graphics API for Windows: {e.Message}");
            }
#else
            Debug.LogWarning("Vulkan configuration is only available on Windows platforms.");
#endif
        }

        private void FixVulkanForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var graphicsApis = new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan };
                PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, graphicsApis);
                EnsureSettingsAreSaved();
                Debug.Log("Vulkan graphics API configured for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Vulkan graphics API for Android: {e.Message}");
            }
#else
            Debug.LogWarning("Vulkan configuration is only available when Android support is installed.");
#endif
        }

        private void FixArm64ForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
                EnsureSettingsAreSaved();
                Debug.Log("ARM64 architecture configured for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set ARM64 architecture for Android: {e.Message}");
            }
#else
            Debug.LogWarning("ARM64 configuration is only available when Android support is installed.");
#endif
        }

        private void FixMinAndroidApiLevel()
        {
#if UNITY_ANDROID
            try
            {
                PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel31;
                EnsureSettingsAreSaved();
                Debug.Log("Minimum Android API level set to Android 12.0 (API level 31) for Android builds.");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set minimum Android API level: {e.Message}");
            }
#else
            Debug.LogWarning("Minimum Android API level configuration is only available when Android support is installed.");
#endif
        }

        private void FixAndroidEntryPoint()
        {
#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
            try
            {
                GeniesSdkPrerequisiteChecker.FixAndroidEntryPoint();
                EnsureSettingsAreSaved();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to fix Android Application Entry Point: {e.Message}");
            }
#else
            Debug.LogWarning("Android Application Entry Point configuration is only available for Unity 6+ with Android support installed.");
#endif
        }

        private void FixActiveInputHandlingToNew()
        {
            try
            {
#if HAS_INPUT_SYSTEM // Custom define to check if the Input System package is installed
                // Set active input handler to Input System Package (New)
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(1))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Input System Package (New)' - RECOMMENDED for modern projects. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Input System Package (New)'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.\n\n" +
                        "Note: For convenience, you can apply other setting changes before restarting. Many settings changes may also require a restart.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
#else
                Debug.LogError("Cannot set to Input System Package (New) because the Input System package is not installed. " +
                    "The Input System package will be installed automatically when you install the Genies Avatar SDK. " +
                    "You can also install it manually via Window > Package Manager, or use 'Use Old' instead.");
                EditorUtility.DisplayDialog("Input System Package Not Installed",
                    "The Input System package is not installed in your project.\n\n" +
                    "The Input System package will be installed automatically when you install the Genies Avatar SDK.\n\n" +
                    "Alternatively:\n" +
                    "• Install it manually via Window > Package Manager\n" +
                    "• Click 'Use Old' to use the legacy Input Manager (not recommended)",
                    "OK");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to new input system: {e.Message}");
            }
        }

        private void FixActiveInputHandlingToOld()
        {
            // Show warning and confirmation dialog
            if (!EditorUtility.DisplayDialog("Use Old Input System?",
                "⚠️ WARNING: The new Input System is STRONGLY RECOMMENDED for modern Unity projects.\n\n" +
                "The old Input Manager has limited functionality and is considered legacy.\n\n" +
                "Are you sure you want to use the old Input Manager instead of the new Input System?",
                "Yes, Use Old",
                "Cancel"))
            {
                // User clicked "Cancel" - abort
                return;
            }

            try
            {
                // Set active input handler to Input Manager (Old)
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(0))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Input Manager (Old)' - Consider upgrading to the new Input System for better functionality. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Input Manager (Old)'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.\n\n" +
                        "Note: For convenience, you can apply other setting changes before restarting. Many settings changes may also require a restart.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to old input system: {e.Message}");
            }
        }

        private void FixActiveInputHandlingToBoth()
        {
            // Show warning and confirmation dialog
            if (!EditorUtility.DisplayDialog("Use Both Input Systems?",
                "⚠️ WARNING: The new Input System ONLY is STRONGLY RECOMMENDED for modern Unity projects.\n\n" +
                "Using 'Both' enables both the old Input Manager and new Input System simultaneously, which:\n" +
                "• Increases build size and complexity\n" +
                "• Will cause build errors on Android\n" +
                "• Is not recommended for production\n\n" +
                "Are you sure you want to enable both input systems instead of using only the new Input System?",
                "Yes, Use Both",
                "Cancel"))
            {
                // User clicked "Cancel" - abort
                return;
            }

            try
            {
                // Set active input handler to Both
                // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both
                if (SetActiveInputHandlerInProjectSettings(2))
                {
                    _ = ApplySettingAndPromptRestart(
                        "Active Input Handling set to 'Both' - This is not recommended and will cause Android build errors. Unity Editor will restart for changes to take effect.",
                        "Input Handling Changed",
                        "Active Input Handling has been set to 'Both'.\n\n" +
                        "Unity Editor needs to restart for the changes to take effect.\n\n" +
                        "Note: For convenience, you can apply other setting changes before restarting. Many settings changes may also require a restart.");
                }
                else
                {
                    Debug.LogError("Failed to update Active Input Handling setting in ProjectSettings.asset");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to set Active Input Handling to both input systems: {e.Message}");
            }
        }

        private void FixTMPEssentials()
        {
            // Try to use the menu item to import TMP Essentials
            // Unity's built-in menu: Window/TextMeshPro/Import TMP Essential Resources
            bool success = EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");

            if (success)
            {
                Debug.Log("[Genies SDK Bootstrap] Opened TMP Essential Resources import window. Please complete the import in the dialog that appears.");
            }
            else
            {
                // Fallback: Show dialog with manual instructions
                Debug.LogWarning("[Genies SDK Bootstrap] Could not open TMP import menu. Please import TMP Essential Resources manually.");
                EditorUtility.DisplayDialog("Import TMP Essential Resources",
                    "Please import TMP Essential Resources manually:\n\n" +
                    "1. Go to Window > TextMeshPro > Import TMP Essential Resources\n" +
                    "2. Click 'Import' in the dialog that appears\n\n" +
                    "If the menu item is not available, TextMesh Pro may not be installed. " +
                    "Please install it via Window > Package Manager.",
                    "OK");
            }
        }

        private bool SetActiveInputHandlerInProjectSettings(int value)
        {
            try
            {
                string projectSettingsPath = Path.Combine(Application.dataPath, "../ProjectSettings/ProjectSettings.asset");
                if (!File.Exists(projectSettingsPath))
                {
                    Debug.LogError("ProjectSettings.asset file not found.");
                    return false;
                }

                string[] lines = File.ReadAllLines(projectSettingsPath);
                bool found = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim().StartsWith("activeInputHandler:"))
                    {
                        lines[i] = $"  activeInputHandler: {value}";
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    File.WriteAllLines(projectSettingsPath, lines);
                    AssetDatabase.Refresh();
                    return true;
                }
                else
                {
                    Debug.LogError("activeInputHandler property not found in ProjectSettings.asset");
                    return false;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error updating ProjectSettings.asset: {e.Message}");
                return false;
            }
        }

        private bool ApplySettingAndPromptRestart(string logMessage, string dialogTitle, string dialogMessage)
        {
            EnsureSettingsAreSaved();
            Debug.Log(logMessage);
            return PromptEditorRestart(dialogTitle, dialogMessage);
        }

        private void FixPlatformSetting(bool activePlatformOnly, System.Action<BuildTargetGroup> setPlatformSetting, string settingName)
        {
            if (activePlatformOnly)
            {
                var activeBuildTargetGroup = GeniesSdkPrerequisiteChecker.GetActiveBuildTargetGroup();
                try
                {
                    setPlatformSetting(activeBuildTargetGroup);
                    EnsureSettingsAreSaved();
                    Debug.Log($"{settingName} configured for active platform: {activeBuildTargetGroup}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to set {settingName} for active platform {activeBuildTargetGroup}: {e.Message}");
                }
            }
            else
            {
                foreach (var group in GeniesSdkPrerequisiteChecker.GetSupportedPlatforms())
                {
                    try
                    {
                        setPlatformSetting(group);
                    }
                    catch
                    {
                        continue;
                    }
                }

                EnsureSettingsAreSaved();
                Debug.Log($"{settingName} configured for all supported platforms.");
            }
        }

#if !GENIES_AVATARSDK_CLIENT
        private void InstallGeniesSdkAvatar()
        {
            IsInstallingGeniesSdkAvatar = true;
            SearchAndInstallPackage(GeniesSdkPrerequisiteChecker.GetPackageName(), OnGeniesSdkInstallComplete);
            IsInstallingGeniesSdkAvatar = false;
        }

        private void OnGeniesSdkInstallComplete(AddRequest addRequest)
        {
            Assert.IsTrue(addRequest.IsCompleted);

            var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();
            if (addRequest.Status == StatusCode.Success)
            {
                Debug.Log($"Successfully installed {packageName}");
                EditorUtility.DisplayDialog("Success",
                    $"'Genies Avatar SDK' ({packageName}) has been successfully installed!", "OK");
            }
            else
            {
                Debug.LogError($"Failed to install {packageName}: {addRequest.Error.message}");
                EditorUtility.DisplayDialog("Installation Failed",
                    $"Failed to install 'Genies Avatar SDK':\n{addRequest.Error.message}", "OK");
            }

            RefreshPrerequisiteStatus();
            Repaint();
        }
#endif

#if !GENIES_AVATARSDK_CLIENT
        private string GetManifestPath()
        {
            return Path.Combine(Application.dataPath, "../Packages/manifest.json");
        }
#endif

        private void SetupFileWatchers()
        {
            CleanupFileWatchers();

#if !GENIES_AVATARSDK_CLIENT
            try
            {
                // Watch manifest.json
                var manifestPath = GetManifestPath();
                if (File.Exists(manifestPath))
                {
                    var manifestDirectory = Path.GetDirectoryName(manifestPath);
                    var manifestFileName = Path.GetFileName(manifestPath);

                    ManifestWatcher = new FileSystemWatcher(manifestDirectory, manifestFileName);
                    ManifestWatcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.CreationTime;
                    ManifestWatcher.Changed += OnFileChanged;
                    ManifestWatcher.Created += OnFileChanged;
                    ManifestWatcher.Renamed += OnFileRenamed;
                    ManifestWatcher.EnableRaisingEvents = true;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to setup file watchers: {e.Message}");
            }
#endif
        }

        private void CleanupFileWatchers()
        {
            if (ManifestWatcher != null)
            {
                ManifestWatcher.Changed -= OnFileChanged;
                ManifestWatcher.Created -= OnFileChanged;
                ManifestWatcher.Renamed -= OnFileRenamed;
                ManifestWatcher.Dispose();
                ManifestWatcher = null;
            }
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            NeedsRefresh = true;
        }

        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            NeedsRefresh = true;
        }

        private void EnsureSettingsAreSaved()
        {
            // Save and serialize assets to ensure settings persist
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        private static void SaveJsonToResources(string clientId, string clientSecret)
        {
            if (!Directory.Exists(ResourcesDir))
            {
                Directory.CreateDirectory(ResourcesDir);
            }

            var data = new GeniesBootstrapAuthSettings { ClientId = clientId, ClientSecret = clientSecret };
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

        /// <summary>
        /// Imports ALL samples defined in the package.json for com.genies.sdk.avatar, then
        /// highlights the imported Samples folder, then opens a scene based on auth settings.
        /// </summary>
        private void ImportAllSamplesHighlightAndOpenScene()
        {
            try
            {
                var packageName = GeniesSdkPrerequisiteChecker.GetPackageName();

                if (!GeniesSdkPrerequisiteChecker.IsSdkInstalled())
                {
                    EditorUtility.DisplayDialog(
                        "SDK Not Installed",
                        $"Package '{packageName}' is not installed.",
                        "OK");
                    return;
                }

                var version = GeniesSdkVersionChecker.GetInstalledSdkVersion();
                if (string.IsNullOrWhiteSpace(version))
                {
                    EditorUtility.DisplayDialog(
                        "Version Not Found",
                        $"Could not read installed SDK version from Packages/{packageName}/package.json",
                        "OK");
                    return;
                }

                // 1) Import ALL samples for this package+version
                var samples = Sample.FindByPackage(packageName, version).ToArray();
                if (samples.Length == 0)
                {
                    EditorUtility.DisplayDialog(
                        "No Samples Found",
                        $"No samples were found for {packageName}@{version}.",
                        "OK");
                    return;
                }

                // If any sample is already imported, ask whether to overwrite.
                bool anyAlreadyImported = samples.Any(s => s.isImported);
                var importOptions = Sample.ImportOptions.OverridePreviousImports;

                if (anyAlreadyImported)
                {
                    bool overwrite = EditorUtility.DisplayDialog(
                        "Samples Already Imported",
                        "One or more samples are already imported.\n\n" +
                        "If you continue, Unity will overwrite the existing imported samples.\n\n" +
                        "Do you want to overwrite them?",
                        "Overwrite",
                        "Cancel");

                    if (!overwrite)
                    {
                        return;
                    }
                }

                foreach (var s in samples)
                {
                    if (!s.Import(importOptions))
                    {
                        EditorUtility.DisplayDialog(
                            "Import Failed",
                            $"Unity reported the sample import failed:\n{s.displayName}",
                            "OK");
                        return;
                    }
                }

                AssetDatabase.Refresh();

                // 2) Highlight the imported folder in Project window
                var anyImported = samples.FirstOrDefault(s => s.isImported);
                if (!anyImported.Equals(default(Sample)) && !string.IsNullOrEmpty(anyImported.importPath))
                {
                    var versionFolderFullPath = Directory.GetParent(anyImported.importPath)?.FullName;
                    if (!string.IsNullOrEmpty(versionFolderFullPath))
                    {
                        var versionFolderAssetPath = ToAssetsPath(versionFolderFullPath);
                        if (!string.IsNullOrEmpty(versionFolderAssetPath))
                        {
                            var folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(versionFolderAssetPath);
                            if (folderObj != null)
                            {
                                Selection.activeObject = folderObj;
                                EditorGUIUtility.PingObject(folderObj);
                            }
                        }
                    }
                }

                // 3) Decide which scene to open based on auth settings
                bool hasCreds = HasValidAuthSettings();
                var sceneWithinSample = hasCreds ? AvatarStarterSceneWithinSample : DemoModeSceneWithinSample;

                var starterScenesSample = samples.FirstOrDefault(s =>
                    string.Equals(s.displayName, SampleStarterScenesDisplayName, StringComparison.Ordinal));

                if (starterScenesSample.Equals(default(Sample)) || string.IsNullOrEmpty(starterScenesSample.importPath))
                {
                    EditorUtility.DisplayDialog(
                        "Sample Not Found",
                        $"Could not find imported sample '{SampleStarterScenesDisplayName}' to open a scene from.",
                        "OK");
                    return;
                }

                var fullScenePath = Path.Combine(starterScenesSample.importPath, sceneWithinSample);
                if (!File.Exists(fullScenePath))
                {
                    EditorUtility.DisplayDialog(
                        "Scene Not Found",
                        $"Expected scene was not found:\n{sceneWithinSample}\n\nUnder:\n{starterScenesSample.importPath}",
                        "OK");
                    return;
                }

                var assetScenePath = ToAssetsPath(fullScenePath);
                if (string.IsNullOrEmpty(assetScenePath))
                {
                    EditorUtility.DisplayDialog(
                        "Path Error",
                        "Could not convert full path to an Assets-relative path.\n\n" +
                        $"Full path:\n{fullScenePath}\n\n" +
                        $"Assets root:\n{Application.dataPath}",
                        "OK");
                    return;
                }

                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(assetScenePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorUtility.DisplayDialog("Error", e.Message, "OK");
            }
        }

        private static string ToAssetsPath(string fullPath)
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                return null;
            }

            var path = fullPath.Replace('\\', '/');
            var assetsRoot = Application.dataPath.Replace('\\', '/').TrimEnd('/');

#if UNITY_EDITOR_WIN
            if (!path.StartsWith(assetsRoot + "/", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(path, assetsRoot, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
#else
            if (!path.StartsWith(assetsRoot + "/", StringComparison.Ordinal) &&
                !string.Equals(path, assetsRoot, StringComparison.Ordinal))
            {
                return null;
            }
#endif

            var relativePath = path.Substring(assetsRoot.Length).TrimStart('/');
            return "Assets/" + relativePath;
        }

        private static bool HasValidAuthSettings()
        {
            try
            {
                var settings = GeniesBootstrapAuthSettings.LoadFromResources();
                return settings != null &&
                       !string.IsNullOrWhiteSpace(settings.ClientId) &&
                       !string.IsNullOrWhiteSpace(settings.ClientSecret);
            }
            catch
            {
                return false;
            }
        }
    }
}
