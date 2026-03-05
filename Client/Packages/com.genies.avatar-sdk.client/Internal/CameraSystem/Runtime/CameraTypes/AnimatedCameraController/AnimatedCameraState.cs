using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Holds the current state of the main camera then follows a proxy camera.
    /// Updates itself through events.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class AnimatedCameraState : MonoBehaviour
#else
    public class AnimatedCameraState : MonoBehaviour
#endif
    {
        public bool FollowingProxy {get; private set;}
        private const string DefaultSceneId = "Default";

        public void OnSceneSwitched(string sceneID)
        {
            FollowingProxy = sceneID != DefaultSceneId;
        }
    }
}
