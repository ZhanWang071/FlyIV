using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Component that acts as a reference between its GameObject and an <see cref="IGenie"/> instance. It also comes with a custom editor
    /// that provides handy testing functionality.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal sealed class GenieReference : MonoBehaviour
#else
    public sealed class GenieReference : MonoBehaviour
#endif
    {
        public IGenie Genie { get; private set; }
        
        private bool _disposeOnDestroy;

        public static GenieReference Create(IGenie genie, GameObject target, bool disposeOnDestroy = false)
        {
            if (genie is null || !target)
            {
                return null;
            }

            var reference = target.GetComponent<GenieReference>();
            if (!reference)
            {
                reference = target.AddComponent<GenieReference>();
            }

            reference.Genie = genie;
            reference._disposeOnDestroy = disposeOnDestroy;
            
            return reference;
        }
        
        private void OnDestroy()
        {
            if (_disposeOnDestroy)
            {
                Genie?.Dispose();
            }

            Genie = null;
        }
    }
}
