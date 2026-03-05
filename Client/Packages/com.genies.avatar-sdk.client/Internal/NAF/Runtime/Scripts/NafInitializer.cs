using UnityEngine;

namespace Genies.Naf
{
    /**
     * Simple component to auto-initialize the NAF plugin in your scene.
     */
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal sealed class NafInitializer : MonoBehaviour
#else
    public sealed class NafInitializer : MonoBehaviour
#endif
    {
        [SerializeField] private NafSettings settings;

        [Tooltip("If true, the NAF plugin will be initialized on Start. If false, it will be initialized on Awake")]
        [SerializeField] private bool initializeOnStart;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject.transform);
            if (!initializeOnStart)
            {
                NafPlugin.Initialize(settings);
            }
        }

        private void Start()
        {
            if (initializeOnStart)
            {
                NafPlugin.Initialize(settings);
            }
        }
    }
}
