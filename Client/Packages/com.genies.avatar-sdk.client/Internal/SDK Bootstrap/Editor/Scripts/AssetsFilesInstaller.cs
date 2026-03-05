using UnityEditor;
using System.IO;
using System.Collections.Generic;

namespace Genies.Sdk.Bootstrap.Editor
{
    [InitializeOnLoad]
    internal static class AssetsFilesInstaller
    {
        private static readonly Dictionary<string, string> TargetFiles = new Dictionary<string, string>
        {
            { "fddeff77f01254fd7ab16dc3537dd4bd", "GETTING_STARTED.md" }, // SDK Bootstrap/GETTING_STARTED.md
            { "f79b83491abbf474b8d4ee75068c6af8", "IMPORTANT.md" } // SDK Bootstrap/IMPORTANT.md
        };

        private const string TargetDir = "Assets/Genies";
        private const string SessionStateKey = "Genies.Sdk.Editor.AssetsFilesInstalled";

        static AssetsFilesInstaller()
        {
            // Use delayCall to ensure the AssetDatabase is initialized before we query it.
            EditorApplication.delayCall += InstallAssetsFiles;
        }

        private static void InstallAssetsFiles()
        {
            try
            {
                bool isFirstRunThisSession = SessionState.GetBool(SessionStateKey, false) is false;
                if (isFirstRunThisSession)
                {
                    SessionState.SetBool(SessionStateKey, true);
                }

                if (TargetFiles.Count > 0 && Directory.Exists(TargetDir) is false)
                {
                    Directory.CreateDirectory(TargetDir);
                    AssetDatabase.ImportAsset(TargetDir);
                }

                foreach (var entry in TargetFiles)
                {
                    string guid = entry.Key;
                    string fileName = entry.Value;

                    string sourcePath = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrWhiteSpace(sourcePath))
                    {
                        // Source file not found via GUID. This can happen if the .meta file is deleted or changed.
                        continue;
                    }

                    string destPath = Path.Combine(TargetDir, fileName);

                    bool destinationFileExists = File.Exists(destPath);
                    bool destinationMetaFileExists = File.Exists(destPath + ".meta");

                    if (isFirstRunThisSession)
                    {
                        if (destinationFileExists && destinationMetaFileExists)
                        {
                            // Case 1: First run, destination file and meta exist.
                            // Overwrite content to preserve GUID.
                            File.WriteAllBytes(destPath, File.ReadAllBytes(sourcePath));
                            AssetDatabase.ImportAsset(destPath); // Notify Unity of content change
                            AssetDatabase.SaveAssets();
                        }
                        else
                        {
                            // Case 2: First run, destination file or meta does NOT exist.
                            // Perform a fresh copy. This will create a new GUID if one doesn't exist.
                            // If destinationFileExists is true but destinationMetaFileExists is false, DeleteAsset will clean up.
                            if (destinationFileExists)
                            {
                                if (AssetDatabase.DeleteAsset(destPath) is false)
                                {
                                    // If we can't delete the old file, we can't copy over it. Silently fail.
                                    continue;
                                }
                            }

                            if (AssetDatabase.CopyAsset(sourcePath, destPath))
                            {
                                AssetDatabase.SaveAssets();
                            }
                        }
                    }
                    else // Not first run this session
                    {
                        if (destinationFileExists is false)
                        {
                            // Case 3: Not first run, destination file does NOT exist.
                            // Copy the asset. This will create a new GUID.
                            if (AssetDatabase.CopyAsset(sourcePath, destPath))
                            {
                                AssetDatabase.SaveAssets();
                            }
                        }
                        // Case 4: Not first run, destination file EXISTS. Do nothing to preserve GUID and user modifications.
                    }
                    // If CopyAsset fails, we do nothing. This is a non-critical feature,
                    // so we silently fail to avoid interrupting the user with warnings.
                }
            }
            catch
            {
                // An unexpected error occurred during a file operation.
                // This could be due to file permissions, invalid paths, or other I/O issues.
                // We are catching this to prevent it from halting other editor processes,
                // but we are intentionally not logging it as this is a non-essential.
            }
        }
    }
}
