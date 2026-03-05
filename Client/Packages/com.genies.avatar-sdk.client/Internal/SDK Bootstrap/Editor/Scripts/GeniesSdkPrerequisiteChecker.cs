using UnityEditor;

namespace Genies.Sdk.Bootstrap.Editor
{
    /// <summary>
    /// Helper class to check prerequisites for the Genies SDK.
    /// Used by both the wizard UI and the domain reload check.
    /// </summary>
    internal static class GeniesSdkPrerequisiteChecker
    {
        private static BuildTargetGroup[] SupportedPlatforms { get; } = new[]
        {
            BuildTargetGroup.Standalone, // Standalone builds are only supported on Windows
            BuildTargetGroup.Android,
            BuildTargetGroup.iOS,
        };

        private static BuildTarget[] SupportedBuildTargets { get; } = new[]
        {
            BuildTarget.StandaloneWindows64,
            BuildTarget.Android,
            BuildTarget.iOS,
        };

        public static string GetPackageName()
        {
            const string externalPackageName = "com.genies.avatar-sdk.client";
            const string internalPackageName = "com.genies.sdk.avatar";

#if GENIES_AVATARSDK_COREUTILS
            return externalPackageName;
#else
            if (GeniesSdkVersionChecker.IsDotUnityPackageVariant())
            {
                return externalPackageName;
            }

            return internalPackageName;
#endif
        }

        public static bool IsSdkInstalled()
        {
            if (GeniesSdkVersionChecker.IsDotUnityPackageVariant())
            {
                return true;
            }

            var packagePath = $"Packages/{GetPackageName()}";
            return AssetDatabase.IsValidFolder(packagePath);
        }

        public static bool IsPlatformSupported(BuildTargetGroup group)
        {
            foreach (var supportedPlatform in SupportedPlatforms)
            {
                if (group == supportedPlatform)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsActivePlatformSupported()
        {
            return IsPlatformSupported(GetActiveBuildTargetGroup());
        }

        public static bool IsActiveBuildTargetSupported()
        {
            return IsBuildTargetSupported(EditorUserBuildSettings.activeBuildTarget);
        }

        public static bool IsIL2CPPConfigured(BuildTargetGroup group)
        {
            try
            {
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                var backend = PlayerSettings.GetScriptingBackend(namedBuildTarget);
                return backend == ScriptingImplementation.IL2CPP;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsIL2CPPConfiguredForActivePlatform()
        {
            return IsIL2CPPConfigured(GetActiveBuildTargetGroup());
        }

        public static bool IsIL2CPPConfiguredForAllPlatforms()
        {
            foreach (var group in SupportedPlatforms)
            {
                if (!IsIL2CPPConfigured(group))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsNetFrameworkConfigured(BuildTargetGroup group)
        {
            try
            {
                var namedBuildTarget = UnityEditor.Build.NamedBuildTarget.FromBuildTargetGroup(group);
                var apiLevel = PlayerSettings.GetApiCompatibilityLevel(namedBuildTarget);
                return apiLevel == ApiCompatibilityLevel.NET_Unity_4_8;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsNetFrameworkConfiguredForActivePlatform()
        {
            return IsNetFrameworkConfigured(GetActiveBuildTargetGroup());
        }

        public static bool IsNetFrameworkConfiguredForAllPlatforms()
        {
            foreach (var group in SupportedPlatforms)
            {
                if (!IsNetFrameworkConfigured(group))
                {
                    return false;
                }
            }
            return true;
        }

        public static bool IsVulkanConfiguredForWindows()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64);
                return graphicsApis != null && graphicsApis.Length > 0 && graphicsApis[0] == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsVulkanConfiguredForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.Android);
                return graphicsApis != null && graphicsApis.Length > 0 && graphicsApis[0] == UnityEngine.Rendering.GraphicsDeviceType.Vulkan;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsArm64ConfiguredForAndroid()
        {
#if UNITY_ANDROID
            try
            {
                var targetArchitectures = PlayerSettings.Android.targetArchitectures;
                return targetArchitectures == AndroidArchitecture.ARM64;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        public static bool IsMinAndroidApiLevelConfigured()
        {
#if UNITY_ANDROID
            try
            {
                return PlayerSettings.Android.minSdkVersion >= AndroidSdkVersions.AndroidApiLevel31;
            }
            catch
            {
                return false;
            }
#else
            return true;
#endif
        }

        /// <summary>
        /// Android Application Entry Point status for Unity 6+.
        /// </summary>
        public enum AndroidEntryPointStatus
        {
            /// <summary>Activity is enabled (valid configuration)</summary>
            Valid,
            /// <summary>Only GameActivity is enabled (error - builds will not work)</summary>
            Error_GameActivityOnly,
            /// <summary>Both Activity and GameActivity are enabled (warning - development builds only)</summary>
            Warning_BothEnabled,
            /// <summary>Not applicable (not on Android platform or not Unity 6+)</summary>
            NotApplicable
        }

        /// <summary>
        /// Checks the Android Application Entry Point configuration for Unity 6+.
        /// For the SDK, Activity must be enabled. GameActivity alone will cause build failures.
        /// </summary>
        public static AndroidEntryPointStatus GetAndroidEntryPointStatus()
        {
#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
            try
            {
                var entry = PlayerSettings.Android.applicationEntry;

                bool activityEnabled = (entry & AndroidApplicationEntry.Activity) != 0;
                bool gameActivityEnabled = (entry & AndroidApplicationEntry.GameActivity) != 0;

                if (activityEnabled && gameActivityEnabled)
                {
                    // Both enabled - warning (development builds only)
                    return AndroidEntryPointStatus.Warning_BothEnabled;
                }
                else if (activityEnabled)
                {
                    // Only Activity enabled - valid
                    return AndroidEntryPointStatus.Valid;
                }
                else if (gameActivityEnabled)
                {
                    // Only GameActivity enabled - error
                    return AndroidEntryPointStatus.Error_GameActivityOnly;
                }
                else
                {
                    // Neither enabled - treat as error (shouldn't happen normally)
                    return AndroidEntryPointStatus.Error_GameActivityOnly;
                }
            }
            catch
            {
                return AndroidEntryPointStatus.NotApplicable;
            }
#else
            return AndroidEntryPointStatus.NotApplicable;
#endif
        }

        /// <summary>
        /// Returns true if Android Application Entry Point is configured correctly (Activity is enabled).
        /// Returns true for non-Android platforms or pre-Unity 6.
        /// </summary>
        public static bool IsAndroidEntryPointConfigured()
        {
            var status = GetAndroidEntryPointStatus();
            return status is AndroidEntryPointStatus.Valid || status is AndroidEntryPointStatus.Warning_BothEnabled || status is AndroidEntryPointStatus.NotApplicable;
        }

        /// <summary>
        /// Fixes the Android Application Entry Point to use Activity (and disable GameActivity if it was the only one enabled).
        /// If both are enabled, this will ensure Activity remains enabled.
        /// </summary>
        public static void FixAndroidEntryPoint()
        {
#if UNITY_ANDROID && UNITY_6000_0_OR_NEWER
            try
            {
                var entry = PlayerSettings.Android.applicationEntry;

                bool activityEnabled = (entry & AndroidApplicationEntry.Activity) != 0;
                bool gameActivityEnabled = (entry & AndroidApplicationEntry.GameActivity) != 0;

                if (!activityEnabled)
                {
                    // Activity not enabled - enable it
                    // If GameActivity was the only one, we'll now have just Activity (or both if user wants to keep GameActivity)
                    // For the SDK, we set Activity only to ensure builds work
                    PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.Activity;
                    UnityEngine.Debug.Log("[Genies SDK Bootstrap] Android Application Entry Point set to 'Activity'.");
                }
                else if (gameActivityEnabled)
                {
                    // Both are enabled - Activity is already there, so it's valid (warning state)
                    // We don't automatically remove GameActivity since user may want it for development
                    UnityEngine.Debug.Log("[Genies SDK Bootstrap] Android Application Entry Point already has 'Activity' enabled. " +
                        "Note: Both 'Activity' and 'GameActivity' are enabled. This configuration is only valid for development builds.");
                }
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"[Genies SDK Bootstrap] Failed to fix Android Application Entry Point: {e.Message}");
            }
#endif
        }

        public static bool IsActiveInputHandlingConfigured()
        {
            // Check using scripting defines
            // ENABLE_INPUT_SYSTEM = New Input System
            // ENABLE_LEGACY_INPUT_MANAGER = Old Input Manager
            // Both defined = "Both" which causes build errors on Android

#if UNITY_ANDROID
#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            // Both enabled - not allowed on Android
            return false;
#else
            // Only one enabled - valid configuration for Android
            return true;
#endif
#else
            // For non-Android platforms, we encourage using the new input system
            // but "Both" doesn't cause build errors, so we consider it configured
            // (The UI will still show a warning to encourage using new)
            return true;
#endif
        }

        public static int GetActiveInputHandlerValue()
        {
            // Use scripting defines to determine the active input handler
            // 0 = Input Manager (Old), 1 = Input System Package (New), 2 = Both

#if ENABLE_INPUT_SYSTEM && ENABLE_LEGACY_INPUT_MANAGER
            return 2; // Both
#elif ENABLE_INPUT_SYSTEM
            return 1; // New Input System
#elif ENABLE_LEGACY_INPUT_MANAGER
            return 0; // Old Input Manager
#else
            return -1; // Neither (shouldn't happen in normal Unity projects)
#endif
        }

        public static bool AreAllPrerequisitesMet()
        {
            if (!IsActivePlatformSupported())
            {
                return false;
            }

            if (!IsIL2CPPConfiguredForActivePlatform())
            {
                return false;
            }

            if (!IsNetFrameworkConfiguredForActivePlatform())
            {
                return false;
            }

            if (!IsVulkanConfiguredForWindows())
            {
                return false;
            }

            if (!IsVulkanConfiguredForAndroid())
            {
                return false;
            }

            if (!IsArm64ConfiguredForAndroid())
            {
                return false;
            }

            if (!IsMinAndroidApiLevelConfigured())
            {
                return false;
            }

            if (!IsAndroidEntryPointConfigured())
            {
                return false;
            }

            if (!IsActiveInputHandlingConfigured())
            {
                return false;
            }

            if (!IsTMPEssentialsImported())
            {
                return false;
            }

            return true;
        }

        public static bool IsTMPEssentialsImported()
        {
            var folders = AssetDatabase.FindAssets("t:folder", new[] { "Assets" });
            foreach (var guid in folders)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Replace("\\", "/").Contains("TextMesh Pro/Resources"))
                {
                    return true;
                }
            }
            return false;
        }

        public static BuildTargetGroup GetActiveBuildTargetGroup()
        {
            return BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
        }

        public static BuildTargetGroup[] GetSupportedPlatforms()
        {
            return SupportedPlatforms;
        }

        public static string GetPlatformDisplayName(BuildTargetGroup group)
        {
            switch (group)
            {
                case BuildTargetGroup.Standalone:
                    return "Standalone (limited support for Windows)";
                case BuildTargetGroup.Android:
                    return "Android";
                case BuildTargetGroup.iOS:
                    return "iOS";
                default:
                    return group.ToString();
            }
        }

        public static string GetBuildTargetDisplayName(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.StandaloneWindows64:
                    return "Windows Standalone (64-bit)";
                case BuildTarget.Android:
                    return "Android";
                case BuildTarget.iOS:
                    return "iOS";
                default:
                    return target.ToString();
            }
        }

        public static string GetSupportedBuildTargetsListString()
        {
            var targetsList = new System.Text.StringBuilder();
            foreach (var target in SupportedBuildTargets)
            {
                targetsList.Append("• ");
                targetsList.Append(GetBuildTargetDisplayName(target));
                targetsList.Append("\n");
            }
            return targetsList.ToString().TrimEnd('\n');
        }

        public static bool IsBuildTargetSupported(BuildTarget target)
        {
            foreach (var supportedTarget in SupportedBuildTargets)
            {
                if (target == supportedTarget)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

