using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "AnimationFeatureManager", menuName = "Genies/Genie Components/Animation Feature Manager")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class AnimationFeatureManagerAsset : GenieComponentAsset
#else
    public sealed class AnimationFeatureManagerAsset : GenieComponentAsset
#endif
    {
        [Tooltip("Whether or not the manager should automatically refresh features when the animator parameters have changed")]
        public bool autoRefreshFeatures = true;
        public List<AnimationFeatureAsset> features = new();


        public override GenieComponent CreateComponent()
        {
            return new AnimationFeatureManager(features)
            {
                AutoRefreshFeatures = autoRefreshFeatures
            };
        }
    }
}
