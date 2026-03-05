using System;
using System.Collections.Generic;
using System.IO;
using Genies.Addressables;
using Genies.Avatars.Services;
using Genies.Closet;
using Genies.Inventory.Installers;
using Genies.Login;
using Genies.Login.Native;
using Genies.Naf.Addressables;
using Genies.Naf.Content;
using Genies.ServiceManagement;
using Genies.Services.Configs;
using Genies.Telemetry;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Configuration class for setting up service installers for the Genies Avatar SDK while in demo mode.
    /// Manages default configurations for feature flags and dynamic configs, and provides
    /// override properties for customizing specific installer instances. You can override
    /// specific installers by setting the corresponding property (e.g., AvatarCmsInstallerOverride).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesDemoModeInstallersSetup
#else
    public class GeniesDemoModeInstallersSetup
#endif
    {
        public AddressableServicesInstaller AddressableServicesInstallerOverride { get; set; }
        public AvatarServiceInstaller AvatarServiceInstallerOverride { get; set; }
        public ClosetServiceInstaller ClosetServiceInstallerOverride { get; set; }
        public GeniesAvatarSdkInstaller GeniesAvatarSdkInstallerOverride { get; set; }
        public IGeniesLoginInstaller GeniesLoginInstallerOverride { get; set; }
        public InventoryServiceInstaller InventoryServiceInstallerOverride { get; set; }
        public LocationsFromInventoryInstaller LocationsFromInventoryInstallerOverride { get; set; }
        public NafContentInstaller NafContentInstallerOverride { get; set; }
        public NafResourceProviderInstaller NafResourceProviderInstallerOverride { get; set; }
        public GeniesTelemetryInstaller TelemetryInstallerOverride { get; set; }

        public GeniesDemoModeInstallersSetup(GeniesApiConfig config = null)
        {
            if (config is not null)
            {
                GeniesApiConfigManager.SetApiConfig(config);
            }
        }

        public List<IGeniesInstaller> ConstructInstallersList()
        {
            SeedDemoInventoryDiskCache(seedResourcesPath: "Genies/DemoConfig");

            return new()
            {

                // Deps for GeniesAvatarSdkInstaller ordered by requirements.
                GeniesLoginInstallerOverride ?? new NativeGeniesLoginInstaller()
                {
                    BaseUrl = GeniesApiConfigManager.GetApiPath(),
                },

                AvatarServiceInstallerOverride ?? new AvatarServiceInstaller(),
                ClosetServiceInstallerOverride ?? new ClosetServiceInstaller(),
                AddressableServicesInstallerOverride ?? new AddressableServicesInstaller()
                {
                    InitializeCatalogs = false
                },
                LocationsFromInventoryInstallerOverride ?? new LocationsFromInventoryInstaller(),
                InventoryServiceInstallerOverride ?? new InventoryServiceInstaller()
                {
                   DefaultInventoryAppId = "DEMO_ALL",
                   DefaultInventoryOrgId = "DEMO",
                   cacheExpirationOverride = int.MaxValue,
                   isDemoMode = true
                },
                NafContentInstallerOverride ?? new NafContentInstaller(),
                NafResourceProviderInstallerOverride ?? new NafResourceProviderInstaller(),
                TelemetryInstallerOverride ?? new GeniesTelemetryInstaller()  {
                    BaseUrl = GeniesApiConfigManager.GetApiPath(),
                },

                // Core installer for this library
                GeniesAvatarSdkInstallerOverride ?? new GeniesAvatarSdkInstaller(),
            };
        }

        [Serializable]
        private class DiskCacheEntry
        {
            [JsonProperty] public string s3DistributionUrl;
            [JsonProperty] public string filePath;
            [JsonProperty] public string creationTimestamp;
            [JsonProperty] public string tag;
        }

        [Serializable]
        private class DiskCacheIndex
        {
            [JsonProperty] public Dictionary<string, DiskCacheEntry> _entries = new();
            [JsonProperty] public List<string> _insertionOrder = new();
            [JsonProperty] public Dictionary<string, DiskCacheEntry> Entries = new();
        }
        
        private static bool TryGetCacheKey(string json, out string cacheKey)
        {
            cacheKey = null;

            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                if (dict == null)
                {
                    return false;
                }

                if (!dict.TryGetValue("CacheKey", out var cacheKeyObj) || cacheKeyObj == null)
                {
                    return false;
                }

                cacheKey = cacheKeyObj.ToString();
                return !string.IsNullOrEmpty(cacheKey);
            }
            catch
            {
                return false;
            }
        }

        private static void SeedDemoInventoryDiskCache(
            string seedResourcesPath)
        {
            try
            {
                var cacheDirectory = Path.Combine(
                    Application.persistentDataPath,
                    // Matches DefaultInventoryServiceDiskCache::_cacheFileLocation
                    "DefaultInventoryCache",
                    GeniesApiConfigManager.TargetEnvironment.ToString()
                );

                if (!Directory.Exists(cacheDirectory))
                {
                    Directory.CreateDirectory(cacheDirectory);
                }

                var cacheFileName = "demo_default_inventory_cache.json";

                var cacheFilePath = Path.Combine(cacheDirectory, cacheFileName);

                var textAssets = Resources.LoadAll<TextAsset>(seedResourcesPath);
                if (textAssets == null || textAssets.Length == 0)
                {
                    // That's fine, just continue.
                    return;
                }

                var index = new DiskCacheIndex();
                var nowIso = DateTime.UtcNow.ToString("o");

                foreach (var asset in textAssets)
                {
                    if (asset == null || string.IsNullOrEmpty(asset.text))
                    {
                        continue;
                    }

                    if (!TryGetCacheKey(asset.text, out var cacheKey))
                    {
                        continue;
                    }

                    var safeFileName = $"{cacheKey}.json";
                    var filePath = Path.Combine(cacheDirectory, safeFileName);

                    File.WriteAllBytes(filePath, asset.bytes);

                    var entry = new DiskCacheEntry
                    {
                        s3DistributionUrl = cacheKey,
                        filePath = filePath,
                        creationTimestamp = nowIso,
                        tag = null
                    };

                    index._entries[cacheKey] = entry;
                    index.Entries[cacheKey] = entry;
                    index._insertionOrder.Add(cacheKey);
                }

                var contents = JsonConvert.SerializeObject(index, Formatting.Indented);
                File.WriteAllText(cacheFilePath, contents);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to seed demo inventory disk cache: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
