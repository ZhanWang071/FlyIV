using Cysharp.Threading.Tasks;

namespace Genies.Services.DynamicConfigs
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IDynamicConfigService
#else
    public interface IDynamicConfigService
#endif
    {
        /// <summary>
        /// Calling manually to avoid process in the constructor
        /// </summary>
        /// <returns></returns>
        UniTask Initialize();
        /// <summary>
        /// It will return a dynamic config object based on the type
        /// </summary>
        /// <param name="configName"></param>
        /// <param name="jsonKey"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        UniTask<T> GetDynamicConfig<T>(string configName, string jsonKey = default);


        public bool IsInitialized { get; }
    }
}
