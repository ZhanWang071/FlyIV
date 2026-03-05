using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using Genies.Avatars.Behaviors;
using Genies.CameraSystem;
using Genies.Customization.MegaEditor;
using Genies.ServiceManagement;
using Genies.UIFramework.Widgets;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace Genies.VirtualCamera
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class VirtualCameraManager : MonoBehaviour
#else
    public class VirtualCameraManager : MonoBehaviour
#endif
    {
        [Header("Virtual Camera Controllers References")]
        public VirtualCameraController<GeniesVirtualCameraCatalog> virtualCameraController;

        public Camera CameraActiveCurrent { get; private set; }
        private CinemachineBlenderSettings CustomBlendCurrentDefault { get; set; }
        private bool CinemachineBrainExistedBefore { get; set; }
        private bool CinemachineBrainWasEnabled { get; set; }

        private void Awake()
        {
            // TODO:
            // Re-work the dependency injection of this VirtualCameraController instance.
            // Since the instance is tied to an attached game object whose lifetime is managed by an external object,
            // handling registering the service as a singleton is confusing and prone to errors.
            ServiceManager.RegisterService(virtualCameraController); // Overrides existing
        }

        private void OnDestroy()
        {
            Deactivate();
        }

        public void Activate(Camera cam)
        {
            if (cam == null) { return; }
            if (virtualCameraController == null) { return; }

            if (ReferenceEquals(cam, CameraActiveCurrent))
            {
                // Already active!
                return;
            }

            Deactivate(); // Deactivate current camera and switch to passed camera

            CameraActiveCurrent = cam;

            // Check if CinemachineBrain already exists and capture its state
            if (cam.TryGetComponent(out CinemachineBrain cinemachineBrain))
            {
                CinemachineBrainExistedBefore = true;
                CustomBlendCurrentDefault = cinemachineBrain.CustomBlends;
                CinemachineBrainWasEnabled = cinemachineBrain.enabled;
            }
            else
            {
                CinemachineBrainExistedBefore = false;
                CustomBlendCurrentDefault = null;
                CinemachineBrainWasEnabled = false;
            }

            //Initialize & Register Virtual Camera Controller
            virtualCameraController.CinemachineCamera = cam;
            virtualCameraController.Initialize().Forget();

            // TODO:
            // Re-work the dependency injection of this VirtualCameraController instance.
            // Since the instance is tied to an attached game object whose lifetime is managed by an external object,
            // handling registering the service as a singleton is confusing and prone to errors.
            ServiceManager.RegisterService(new PictureInPictureCameraProvider(virtualCameraController.CinemachineCamera));
        }

        public void Deactivate()
        {
            if (CameraActiveCurrent == null)
            {
                return;
            }

            if (virtualCameraController != null && virtualCameraController.CinemachineCamera != null)
            {
                if (ReferenceEquals(CameraActiveCurrent, virtualCameraController.CinemachineCamera))
                {
                    virtualCameraController.DeactivateAllVirtualCameras();
                }
            }

            if (CameraActiveCurrent.TryGetComponent(out CinemachineBrain cinemachineBrain))
            {
                if (CinemachineBrainExistedBefore)
                {
                    // Restore the original custom blends (could be null)
                    cinemachineBrain.CustomBlends = CustomBlendCurrentDefault;
                    cinemachineBrain.enabled = CinemachineBrainWasEnabled;
                }
                else
                {
                    // CinemachineBrain was added by the VirtualCameraController.Initialize()
                    Destroy(cinemachineBrain);
                }
            }

            CameraActiveCurrent = null;
            CustomBlendCurrentDefault = null;
            CinemachineBrainExistedBefore = false;
            CinemachineBrainWasEnabled = false;
        }
    }
}
