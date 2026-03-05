using System;
using Toolbox.Core;
using UnityEngine;

namespace Genies.FeatureFlags
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FeatureData
#else
    public class FeatureData
#endif
    {
        public SerializableFeatureFlag featureFlag;
        public bool enabled;

#if UNITY_EDITOR
        public bool shouldOverrideRemote;
#endif
    }

#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FeatureConfig", menuName = "Genies/FeatureConfig")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FeatureConfig : ScriptableObject
#else
    public class FeatureConfig : ScriptableObject
#endif
    {
        [LabelByChild("featureFlag.featureFlagKey"), ReorderableList(HasLabels = true, Foldable = false)]
        public FeatureData[] features;
    }
}
