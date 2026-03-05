using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

#if UNITY_IOS
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
#endif

namespace Genies.NafPlugin.Editor
{
    /// <summary>
    /// Handles platform-specific post-process build steps for iOS and macOS.
    /// For macOS standalone builds, enables "Load on Startup" for native plugins
    /// that should only be preloaded in standalone builds, not in the Editor!!!
    /// </summary>
    internal class XcodePostProcess : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        // GUIDs of macOS native plugins that should be preloaded in standalone builds
        private static readonly string[] MacOSPluginGuids = new[]
        {
            "aabe75528800f4458848514f415417ce", // libgnAnimationWrapper.dylib
            "54bfecec9e9e0439b879b452aa8c5e06", // libgnBehaviourWrapper.dylib
            "bc1fc2064a47940f2885d039d03a23e7", // libgnContainerWrapper.dylib
            "e6bc1c185ebb242d589bb887f64d1dce", // libgnCoreWrapper.dylib
            "46245de6ff8534b288bd2c13489db563", // libgnGraphicsWrapper.dylib
            "50bd0046da81d48cbb14781431195d25", // libgnUnityPlugin.dylib
            "53d4c4fad8421455b924697933b1d1c6", // libgnUtilsWrapper.dylib
        };

        // Store original preload states by GUID for portability across environments
        private static Dictionary<string, bool> _originalPreloadStatesByGuid = new Dictionary<string, bool>();

        public int callbackOrder => 0;

        /// <summary>
        /// Pre-build: Enable "Load on Startup" for macOS plugins when building standalone.
        /// </summary>
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
            {
                return;
            }

            Debug.Log("macOS Standalone build detected. Enabling 'Load on Startup' for native plugins...");

            _originalPreloadStatesByGuid.Clear();

            foreach (string guid in MacOSPluginGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Could not resolve asset path for GUID: {guid}");
                    continue;
                }

                PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer != null)
                {
                    // Store original state by GUID
                    _originalPreloadStatesByGuid[guid] = importer.isPreloaded;

                    // Enable preload for standalone build
                    if (!importer.isPreloaded)
                    {
                        importer.isPreloaded = true;
                        importer.SaveAndReimport();
                        Debug.Log($"Enabled 'Load on Startup' for: {Path.GetFileName(assetPath)}");
                    }
                }
                else
                {
                    Debug.LogWarning($"Could not find PluginImporter for: {assetPath} (GUID: {guid})");
                }
            }
        }

        /// <summary>
        /// Post-build: Restore original "Load on Startup" settings for macOS plugins.
        /// </summary>
        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneOSX)
            {
                return;
            }

            Debug.Log("Restoring original 'Load on Startup' settings for native plugins...");

            foreach (var kvp in _originalPreloadStatesByGuid)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(kvp.Key);
                if (string.IsNullOrEmpty(assetPath))
                {
                    Debug.LogWarning($"Could not resolve asset path for GUID: {kvp.Key}");
                    continue;
                }

                PluginImporter importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                if (importer != null && importer.isPreloaded != kvp.Value)
                {
                    importer.isPreloaded = kvp.Value;
                    importer.SaveAndReimport();
                    Debug.Log($"Restored 'Load on Startup' to {kvp.Value} for: {Path.GetFileName(assetPath)}");
                }
            }

            _originalPreloadStatesByGuid.Clear();
        }

#if UNITY_IOS
        [PostProcessBuild]
        public static void OnPostprocessBuildIOS(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS)
            {
                Debug.Log("Skipping: not an iOS build.");
                return;
            }

            // Load the Xcode project
            var projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
            Debug.Log("Loading Xcode project at: " + projPath);
            var proj = new PBXProject();
            proj.ReadFromFile(projPath);

            // Get Unity's main target GUID
#if UNITY_2019_3_OR_NEWER
            var frameworkTargetGuid = proj.GetUnityFrameworkTargetGuid();
            Debug.Log("UnityFramework target GUID = " + frameworkTargetGuid);
#else
            // For older Unity versions there's only one target, so fall back:
            var frameworkTargetGuid = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
            Debug.Log("(Legacy) Unity target GUID = " + frameworkTargetGuid);
#endif
            Debug.Log("Unity iOS target GUID = " + frameworkTargetGuid);

            // Ask Xcode to link against ImageIO.framework (located in the iOS SDK)
            proj.AddFrameworkToProject(frameworkTargetGuid, "ImageIO.framework", false);

            // Save the modified project file
            proj.WriteToFile(projPath);
            Debug.Log("Wrote modifications to Xcode project. OnPostprocessBuild complete.");
        }
#endif
    }
}
