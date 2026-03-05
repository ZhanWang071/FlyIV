using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GeniesCameraEmoteContainer", menuName = "Genies/AnimationLibrary/GeniesCameraEmoteContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesCameraEmoteContainer : AnimationContainer
#else
    public class GeniesCameraEmoteContainer : AnimationContainer
#endif
    {
        // No additional fields needed for now
    }
}
