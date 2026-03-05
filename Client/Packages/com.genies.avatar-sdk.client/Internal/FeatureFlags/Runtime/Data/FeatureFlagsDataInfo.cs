
using System.Collections.Generic;
using UnityEngine;

namespace Genies.FeatureFlags
{
    /// <summary>
    /// A collection of sensitive data info of the feature flags
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FeatureFlagsDataInfo", menuName = "GeniesParty/Feature Flags/FeatureFlagsDataInfo")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FeatureFlagsDataInfo : ScriptableObject
#else
    public class FeatureFlagsDataInfo : ScriptableObject
#endif
    {
        [SerializeField] private List<string> _data;
        public List<string> Data => _data;

    }

}
