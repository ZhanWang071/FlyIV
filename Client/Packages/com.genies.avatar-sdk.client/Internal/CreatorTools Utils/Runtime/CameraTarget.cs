using UnityEngine;

namespace Genies.CreatorTools.utils
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class CameraTarget : MonoBehaviour
#else
    public class CameraTarget : MonoBehaviour
#endif
    {
        public float distance = 10f;
    }
}
