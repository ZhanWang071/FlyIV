using Genies.Avatars;
using Genies.Components.Dynamics;
using UnityEngine;

namespace Genies.Dynamics
{
    /// <summary>
    /// Wrapper around <see cref="DynamicsRecipe"/> that allows for creation of a <see cref="DynamicsAnimator"/>
    /// for running Dynamics on Genies Avatars.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DynamicsAnimatorAsset", menuName = "Genies/Dynamics/Dynamics Animation Asset")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DynamicsAnimatorAsset : GenieComponentAsset
#else
    public class DynamicsAnimatorAsset : GenieComponentAsset
#endif
    {
        public DynamicsRecipe recipe;

        public override GenieComponent CreateComponent()
        {
            return new DynamicsAnimator(recipe);
        }
    }
}
