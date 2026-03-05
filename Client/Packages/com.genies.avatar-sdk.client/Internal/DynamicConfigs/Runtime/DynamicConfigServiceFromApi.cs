using System;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.ServiceManagement;
using Genies.Services.Configs;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Services.DynamicConfigs
{
    /// <summary>
    /// Obsolete, use <see cref="WebRequestDynamicConfigsToolBehavior"/> instead
    /// </summary>
    // [Obsolete("Obsolete, use WebRequestDynamicConfigsToolBehavior instead")]
    // ^ Commented out since this causes Unity ASV tests to fail.
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicConfigServiceFromApi : IDynamicConfigService
#else
    public class DynamicConfigServiceFromApi : IDynamicConfigService
#endif
    {
        private GeniesAppStateManager _StateManager => this.GetService<GeniesAppStateManager>();

        private const string SelectedBackendEnvironment = "SelectedBackendEnvironment";

        public bool IsInitialized => _isInitialized;
        private bool _isInitialized;

        private UniTaskCompletionSource _apiInitializationSource;
        private IDynamicConfigsToolBehavior _toolBehavior;
        private BackendEnvironment _currentEnvironment = BackendEnvironment.Dev;
        private bool _prodOverride;

        public DynamicConfigServiceFromApi(IDynamicConfigsToolBehavior toolBehavior, bool prodOverride = false)
        {
            _prodOverride = prodOverride;
            if (_prodOverride)
            {
                _currentEnvironment = BackendEnvironment.Prod;
            }

            _toolBehavior = toolBehavior;

#if PRODUCTION_BUILD
            _currentEnvironment = BackendEnvironment.Prod;
#else

            var hasEnvironmentValue = false;
            if (_StateManager != null)
            {
                hasEnvironmentValue = _StateManager.HasState(SelectedBackendEnvironment);
            }

            if (!prodOverride)
            {
                _currentEnvironment = hasEnvironmentValue
                    ? _StateManager.GetState<BackendEnvironment>(SelectedBackendEnvironment)
                    : BackendEnvironment.Dev;
            }
#endif
        }

        public async UniTask Initialize()
        {
            await AwaitApiInitialization();
        }

        private UniTask WaitUntilInitializedAsync()
        {
            if (_apiInitializationSource == null)
            {
                return UniTask.CompletedTask;
            }

            return _apiInitializationSource.Task;
        }

        private async UniTask AwaitApiInitialization()
        {
            try
            {
                // eventually pass trait service before chat service
                if (_apiInitializationSource != null)
                {
                    await _apiInitializationSource.Task;
                    return;
                }

                _apiInitializationSource = new UniTaskCompletionSource();

                var idList = _toolBehavior.DynamicConfigIdList;
#if PRODUCTION_BUILD
                    //load from the BE API for Prod
                  await _toolBehavior.FetchDynamicConfigsFromApi(idList, requestDev: false, requestProd: true);
#else

                if (!_toolBehavior.UseLocalVersion)
                {
                    //load from the BE API for Dev
                    await _toolBehavior.FetchDynamicConfigsFromApi(idList, requestDev: !_prodOverride, requestProd: _prodOverride);
                }

#endif
                _apiInitializationSource.TrySetResult();
                _apiInitializationSource = null;

                _isInitialized = true;
            }
            catch (OperationCanceledException ex)
            {
                Debug.LogWarning($"Operation cancelled by the user: {ex}");
            }
            catch (Exception exception)
            {
                CrashReporter.Log(
                    $"Failed to Initialize API in {nameof(DynamicConfigServiceFromApi)}: {exception}",
                    LogSeverity.Error);
            }
        }

        private string GetApiBaseUrl(BackendEnvironment environment)
        {
            switch (environment)
            {
                case BackendEnvironment.QA:
                    return "https://api.qa.genies.com";
                case BackendEnvironment.Prod:
                    return "https://api.genies.com";
                case BackendEnvironment.Dev:
                    return "https://api.dev.genies.com";
                default:
                    throw new ArgumentOutOfRangeException(nameof(environment), environment, null);
            }
        }

        public async UniTask<T> GetDynamicConfig<T>(string configName, string jsonKey = default)
        {
            await WaitUntilInitializedAsync();

            T response = default(T);

            //try use local version via Editor Window Flag (Non-production only)
#if !PRODUCTION_BUILD
            if (_toolBehavior.UseLocalVersion)
            {
                 response = await _toolBehavior.GetLocalDynamicConfig<T>(_currentEnvironment, configName, jsonKey);
                if (response != null && !response.Equals(default(T)))
                {
                    return response;
                }
            }
#endif

            //try get from API
            response = await _toolBehavior.GetDynamicConfig<T>(_currentEnvironment, configName, jsonKey);
            if (response != null && !response.Equals(default(T)))
            {
                return response;
            }

            //TODO try get from Local
            response = await _toolBehavior.GetLocalDynamicConfig<T>(_currentEnvironment, configName, jsonKey);
            if (response != null && !response.Equals(default(T)))
            {
                return response;
            }

            return default;
        }
    }
}
