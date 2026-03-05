using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IGeniePrefab
#else
    public interface IGeniePrefab
#endif
    {
        public IGenie Instantiate();
        public IGenie Instantiate(Transform parent);
        public IGenie Instantiate(Transform parent, bool worldPositionStays);
        public IGenie Instantiate(Vector3 position, Quaternion rotation);
        public IGenie Instantiate(Vector3 position, Quaternion rotation, Transform parent);
    }
}