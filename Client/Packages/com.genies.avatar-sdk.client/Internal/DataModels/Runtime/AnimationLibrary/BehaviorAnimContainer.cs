using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BehaviorAnimContainer", menuName = "Genies/AnimationLibrary/BehaviorAnimContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BehaviorAnimContainer : AnimationContainer
#else
    public class BehaviorAnimContainer : AnimationContainer
#endif
    {
        // No additional fields needed for now
    }
}
