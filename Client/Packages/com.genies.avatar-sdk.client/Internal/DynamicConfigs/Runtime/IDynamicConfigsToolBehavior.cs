using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Services.Configs;
using Genies.Services.Model;
using Newtonsoft.Json;

namespace Genies.Services.DynamicConfigs
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IDynamicConfigsToolBehavior
#else
    public interface IDynamicConfigsToolBehavior
#endif
    {
        bool EnablingUsageToggle { get; set; }
        bool UseLocalVersion { get; set; }
        List<string> DynamicConfigIdList { get; }

        UniTask<DynamicConfigFiles> FetchDynamicConfigsFromApi(
            List<string> dynamicConfigIds,
            bool requestDev,
            bool requestProd);

        UniTask<T> GetLocalDynamicConfig<T>(
            BackendEnvironment environment,
            string configName,
            string jsonKey = default);

        UniTask<T> GetDynamicConfig<T>(
            BackendEnvironment environment,
            string configName,
            string jsonKey = default);

        UniTask<bool> CreateOrUpdateLocalData();
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicConfigsAppState
#else
    public class DynamicConfigsAppState
#endif
    {
        public bool EnablingUsageToggle;
        public bool UseLocalVersion;
        public List<string> DynamicConfigIdList;
    }

    /// <summary>
    /// Represents the collection of all raw dynamic json files per environment
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicConfigFiles
#else
    public class DynamicConfigFiles
#endif
    {
        public Dictionary<BackendEnvironment, Dictionary<string, DynamicConfigFileData>> Files;

        public DynamicConfigFiles()
        {
            Files = new Dictionary<BackendEnvironment, Dictionary<string, DynamicConfigFileData>>();
        }

    }

    /// <summary>
    /// File that contains a json file of a Dynamic Config from Backend API
    /// This file is totally equal to the Statsig dynamic config JSON
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicConfigFileData
#else
    public class DynamicConfigFileData
#endif
    {
        public DynamicConfig ApiJsonObject { get; private set; }
        public string RawJson { get; private set; }

        public DynamicConfigFileData(DynamicConfig apiJsonObject)
        {
            ApiJsonObject = apiJsonObject;
            RawJson = JsonConvert.SerializeObject(ApiJsonObject);
        }

    }

}
