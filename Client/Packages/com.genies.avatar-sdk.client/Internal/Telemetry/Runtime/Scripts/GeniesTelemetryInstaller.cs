using System;
using Cysharp.Threading.Tasks;
using Genies.Login.Native.Data;
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.Telemetry
{
    [AutoResolve]
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesTelemetryInstaller : IGeniesInstaller, IGeniesInitializer
#else
    public class GeniesTelemetryInstaller : IGeniesInstaller, IGeniesInitializer
#endif
    {
        [Header("Genies Telemetry Config")]
        [SerializeField] private string _baseUrl         = "https://api.genies.com";
        [SerializeField] private string _appName         = "GeniesSdk";   // must match auth installer
        [SerializeField] private string _clientId        = "";
        [SerializeField] private string _sdkVersion      = "1.0.0";
        [SerializeField] private int    _maxBatchSize    = 20;
        [SerializeField] private int    _flushIntervalMs = 2000;

        internal const string CacheName = "GeniesSdkVersionCache";
        /// <summary>
        /// Telemetry backend base URL (should match your telemetry API environment).
        /// </summary>
        public string BaseUrl
        {
            get => _baseUrl;
            set => _baseUrl = value;
        }

        /// <summary>
        /// Can be anything. Used as key for keychain / token scope (must line up with auth).
        /// </summary>
        public string AppName
        {
            get => _appName;
            set => _appName = value;
        }

        /// <summary>
        /// Partner / app client ID. If left empty, we try to pull from GeniesAuthSettings.
        /// </summary>
        public string ClientId
        {
            get => _clientId;
            set => _clientId = value;
        }

        /// <summary>
        /// SDK version string reported to telemetry.
        /// Defaults to this serialized value; you can override at runtime.
        /// </summary>
        public string SdkVersion
        {
            get => _sdkVersion;
            set => _sdkVersion = value;
        }

        public int MaxBatchSize
        {
            get => _maxBatchSize;
            set => _maxBatchSize = value;
        }

        public int FlushIntervalMs
        {
            get => _flushIntervalMs;
            set => _flushIntervalMs = value;
        }

        public void Install(IContainerBuilder builder)
        {
            // Telemetry is exposed via static GeniesTelemetry API,
            // so there's nothing we strictly need to register here.
            // Keeping this method for symmetry / future expansion.
        }

        public async UniTask Initialize()
        {
#if !UNITY_EDITOR
            {
                return;
            }
#endif
            // Resolve clientId from GeniesAuthSettings if not provided explicitly.
            if (string.IsNullOrWhiteSpace(_clientId))
            {
                var settings = GeniesAuthSettings.LoadFromResources();
                if (settings != null && !string.IsNullOrWhiteSpace(settings.ClientId))
                {
                    _clientId = settings.ClientId;
                }
            }

            // Attempt to load SDK version from Resources version cache (if present)
            if (string.IsNullOrWhiteSpace(_sdkVersion) || _sdkVersion == "1.0.0")
            {
                var cache = Resources.Load<GeniesSdkVersionCache>(
                    CacheName);

                if (cache != null &&
                    !string.IsNullOrWhiteSpace(cache.Version) &&
                    cache.Version != "unknown")
                {
                    _sdkVersion = cache.Version;
                }
            }
            
            // Decide effective SDK + platform metadata
            var unityVersion = Application.unityVersion;
            var platform     = Application.platform.ToString();
            var sdkVersion   = string.IsNullOrWhiteSpace(_sdkVersion) ? unityVersion : _sdkVersion;

            try
            {
                GeniesTelemetry.Initialize(
                    _baseUrl,
                    _appName,
                    _clientId ?? string.Empty,
                    sdkVersion,
                    platform,
                    unityVersion,
                    _maxBatchSize,
                    _flushIntervalMs
                );
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[GeniesTelemetryInstaller] Failed to initialize telemetry: {ex.Message}");
            }

            await UniTask.CompletedTask;
        }
    }
}
