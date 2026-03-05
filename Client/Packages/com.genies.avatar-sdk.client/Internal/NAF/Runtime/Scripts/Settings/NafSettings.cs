using System;
using System.IO;
using GnWrappers;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Genies.Naf
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "NAF-Settings", menuName = "Genies/NAF/NAF Settings", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NafSettings : ScriptableObject
#else
    public sealed class NafSettings : ScriptableObject
#endif
    {
        private const string DefaultSettingsFilename = "NafSettings_Default";
        private const string ProjectSettingsFilename = "NafSettings_Project";

        private static string ProjectSettingsResourcesPath { get; } = Path.Combine("Assets", "Genies", "NAF", "Resources");

        public enum Logging
        {
            Disabled              = 0,
            EditorOnly            = 1,
            EditorOrDevBuildOnly  = 2,
            Always                = 3,
        }

        public Logging                logging                    = Logging.EditorOnly;
        public NafAssetResolverConfig defaultAssetResolverConfig;
        public NafTextureSettings     globalTextureSettings;
        public NafMaterialSettings    defaultMaterialSettings;

        /**
         * Applies the settings to the NAF plugin. This ignores any settings that can only be applied during initialization, like logging.
         */
        public void Apply()
        {
            // apply global texture settings
            using TextureSettings textureSettings = TextureSettings.Global();
            globalTextureSettings.Write(textureSettings);

            // set default asset resolver config
            NafAssetResolverConfig.Default = defaultAssetResolverConfig;

            // set default material settings
            NafMaterialSettings.Default = defaultMaterialSettings;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Creates the project specific NafSettings if one does not already exist.
        /// If an existing asset is found at the expected path but is not a NafSettings object, it will be deleted and a new one created.
        /// If a default NafSettings asset exists, its contents will be copied to the new project settings asset.
        /// </summary>
        public static void CreateProjectSettings()
        {
            var fullAssetPath = Path.Combine(ProjectSettingsResourcesPath, $"{ProjectSettingsFilename}.asset");

            try
            {
                // Optimal check
                if (AssetDatabase.GetMainAssetTypeAtPath(fullAssetPath) == typeof(NafSettings))
                {
                    // Already exists.
                    return;
                }

                if (File.Exists(fullAssetPath))
                {
                    // Force import of asset
                    AssetDatabase.ImportAsset(fullAssetPath);

                    // Check again after import
                    if (AssetDatabase.GetMainAssetTypeAtPath(fullAssetPath) == typeof(NafSettings))
                    {
                        return;
                    }

                    // Asset exists at the target file path, but did not validate as the correct type.
                    // Delete the invalid asset.
                    if (AssetDatabase.DeleteAsset(fullAssetPath) is false)
                    {
                        File.Delete(fullAssetPath);
                    }
                }

                var directory = Path.GetDirectoryName(fullAssetPath);
                if (Directory.Exists(directory) is false)
                {
                    Directory.CreateDirectory(directory);
                    AssetDatabase.ImportAsset(directory);
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            try
            {
                var created = false;

                if (TryLoadDefault(out NafSettings defaultSettings))
                {
                    var defaultAssetPath = AssetDatabase.GetAssetPath(defaultSettings);
                    if (AssetDatabase.CopyAsset(defaultAssetPath, fullAssetPath))
                    {
                        created = true;
                    }
                }

                if (created is false)
                {
                    AssetDatabase.CreateAsset(CreateInstance<NafSettings>(), fullAssetPath);
                }

                AssetDatabase.SaveAssets();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
#endif

        /// <summary>
        /// Tries to load the project specific NafSettings.
        /// </summary>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static bool TryLoadProject(out NafSettings settings)
        {
            settings = Resources.Load<NafSettings>(ProjectSettingsFilename);
            return settings != null;
        }

        public static bool TryLoadDefault(out NafSettings settings)
        {
            settings = Resources.Load<NafSettings>(DefaultSettingsFilename);
            return settings != null;
        }
    }
}
