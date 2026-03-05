using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Component that acts as a reference between its GameObject and an <see cref="ISpeciesGenieController"/> instance.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal sealed class SpeciesGenieControllerReference : MonoBehaviour
#else
    public sealed class SpeciesGenieControllerReference : MonoBehaviour
#endif
    {
        public ISpeciesGenieController Controller { get; private set; }
        
        private bool _disposeOnDestroy;

        public static SpeciesGenieControllerReference Create(ISpeciesGenieController controller, GameObject target, bool disposeOnDestroy = false)
        {
            if (controller is null || !target)
            {
                return null;
            }

            var reference = target.GetComponent<SpeciesGenieControllerReference>();
            if (!reference)
            {
                reference = target.AddComponent<SpeciesGenieControllerReference>();
            }

            reference.Controller = controller;
            reference._disposeOnDestroy = disposeOnDestroy;
            
            return reference;
        }
        
        private void OnDestroy()
        {
            if (_disposeOnDestroy)
            {
                Controller?.Dispose();
            }

            Controller = null;
        }
    }
}
