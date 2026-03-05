using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "IKRetargetingConfig", menuName = "Genies/Genie Components/Configs/IK Retargeting Config")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class IkrConfig : ScriptableObject
#else
    public sealed class IkrConfig : ScriptableObject
#endif
    {
        public List<Goal> goals = new();

        [Serializable]
        public struct Goal
        {
            public AvatarIKGoal goal;
            public List<TransformIkrTarget.Config> transformTargets;
        }
    }
}
