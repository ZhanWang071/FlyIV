using System;
using System.Collections.Generic;
using Toolbox.Core;
using UnityEngine;

namespace Genies.FeatureFlags
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SerializableFeatureFlag
#else
    public class SerializableFeatureFlag
#endif
    {

        private static IReadOnlyList<string> _AllFeatureFlags => FeatureFlagsHelper.AllFlagValues;
        private static IReadOnlyList<string> _AllFeatureFlagKeys => FeatureFlagsHelper.AllFlagKeys;

        [Preset(nameof(_AllFeatureFlags), nameof(_AllFeatureFlagKeys))]
        public string featureFlag;

        [SerializeField]
        [Hide]
        private string featureFlagKey;

        public static implicit operator string(SerializableFeatureFlag sff)
        {
            return sff.featureFlag;
        }

        public override string ToString()
        {
            return featureFlag;
        }
    }
}
