using UnityEngine;

namespace Genies.Components.Dynamics
{
    /// <summary>
    /// Common base class for colliders in the Genies Dynamics System.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal abstract class DynamicsCollider : MonoBehaviour
#else
    public abstract class DynamicsCollider : MonoBehaviour
#endif
    {

    }
}
