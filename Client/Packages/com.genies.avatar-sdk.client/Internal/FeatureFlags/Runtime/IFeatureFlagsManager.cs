using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.FeatureFlags
{

#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IFeatureFlagsManager
#else
    public interface IFeatureFlagsManager
#endif
    {
        UniTask Initialize();
        UniTask<Dictionary<string,bool>> GetAllFeatureFlagsStatus();

        void SetFeatureFlagOverride(string featureFlag, Func<bool> isEnabledGetter);
        void RemoveFeatureFlagOverride(string featureFlag);
        bool IsFeatureEnabled(string featureFlag);
    }
}
