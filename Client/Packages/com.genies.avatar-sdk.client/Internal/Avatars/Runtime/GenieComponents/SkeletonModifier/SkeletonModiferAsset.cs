using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "SkeletonModifier", menuName = "Genies/Genie Components/Skeleton Modifier")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class SkeletonModiferAsset : GenieComponentAsset
#else
    public sealed class SkeletonModiferAsset : GenieComponentAsset
#endif
    {
        public List<GenieJointModifier> modifiers = new();

        public override GenieComponent CreateComponent()
        {
            return new SkeletonModifier(modifiers);
        }
    }
}
