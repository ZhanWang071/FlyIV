using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using Genies.CameraSystem.Focusable;
using Genies.UI.Animations;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Provides methods to activate or deactivate cameras based on a list of enum (catalogs).
    /// </summary>
    /// <remarks> This class depends on the enum (Camera Catalog) to initialize.
    /// You can create as many controllers as you have catalogs to create different use cases of the camera.</remarks>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class VirtualCameraController<TEnum> where TEnum : struct, Enum
#else
    public class VirtualCameraController<TEnum> where TEnum : struct, Enum
#endif
    {
        [Header("Camera References")]
        [Tooltip("Reference to the camera object to use by this controller")]
        public Camera CinemachineCamera;

        [Header("Virtual Cameras Settings")]
        [Tooltip("Transition settings for this virtual camera controller")]
        [SerializeField] private CinemachineBlenderSettings _blenderSettings;

        [Tooltip("List of cameras to use by this controller. " +
            "Make sure this list represents the catalog of the controller when setting up the controller in the Scene.")]
        [SerializeField] private List<GeniesVirtualCamera> _geniesVirtualCameras;

        private CinemachineBrain _brain;

        private ICameraType _activeCameraController;
        private CinemachineVirtualCameraBase _activeVCam;
        private CinemachineCamera _transitionCamera;

        private const int _mainPriority = int.MaxValue;
        private const int _middlePriority = int.MaxValue - 1;
        private const int _minPriority = 0;
        private const float _firstCameraDelay = 0.3f;

        private float _originalCamFocalLength;
        private Vector2 _originalCamSensorSize;

        private Dictionary<TEnum, int> _enumToIndexMap = new Dictionary<TEnum, int>();

        private SemaphoreSlim _activationLock = new (1,1);

        /// <summary>
        /// Initializes this virtual camera controller and its cameras.
        /// If using multiple Virtual Camera Controllers, make sure to use this method
        /// to override any previous controller.
        /// </summary>
        public async UniTask Initialize()
        {
            // Initialize the dictionary to store enum values to avoid casting
            InitializeEnumToIndexMap();

            // Get the CinemachineBrain from the camera object (assign to main camera or create camera if null)
            if (CinemachineCamera == null)
            {
                CinemachineCamera = Camera.main;
            }

            if (CinemachineCamera == null)
            {
                GameObject camObj = new GameObject("Main Camera");
                CinemachineCamera = camObj.AddComponent<Camera>();
                camObj.AddComponent<AudioListener>();
                CinemachineCamera.tag = "MainCamera";
            }

            await CreateCinemachineBrainIfNeeded();

            // Set the transition
            if (_blenderSettings != null)
            {
                _brain.CustomBlends = _blenderSettings;
            }

            _originalCamFocalLength = CinemachineCamera.focalLength;
            _originalCamSensorSize = CinemachineCamera.sensorSize;

            // Set up virtual cameras
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }
                geniesVirtualCamera.CameraTypeScript.ConfigureVirtualCamera();
            }

            _activeVCam = null;
            _activeCameraController = null;
        }


        /// <summary>
        /// Checks if there is currently a cinemachine brain on the camera, and if not, adds one and enables it. If one is
        /// added at runtime, then a virtual camera is added to prevent cutting from the current camera view
        /// </summary>
        private async UniTask<bool> CreateCinemachineBrainIfNeeded()
        {
            _brain = CinemachineCamera.GetComponent<CinemachineBrain>();

            // If there was no brain before, or if there were no virtual cameras,
            // add a virtual camera onto the current camera to prevent jitter/cutting
            if (_brain == null || _brain.enabled == false || _brain.ActiveVirtualCamera == null)
            {
                if (_brain == null)
                {
                    _brain = CinemachineCamera.gameObject.AddComponent<CinemachineBrain>();
                }

                _brain.enabled = true;

                if (_transitionCamera == null)
                {
                    _transitionCamera = new GameObject("TransitionCamera")
                        .AddComponent<CinemachineCamera>();
                }

                // The transition camera needs to be evaluated for one frame for the brain to be set at its position/rotation
                _transitionCamera.transform.SetPositionAndRotation(CinemachineCamera.transform.position, CinemachineCamera.transform.rotation);
                _transitionCamera.Lens.FieldOfView = CinemachineCamera.fieldOfView;

                _transitionCamera.Priority = _mainPriority;
                await UniTask.Yield();
                _transitionCamera.Priority = _middlePriority;

                return true;
            }

            return false;
        }

        private void InitializeEnumToIndexMap()
        {
            // Store enum values to the dictionary
            TEnum[] values = (TEnum[])Enum.GetValues(typeof(TEnum));
            for (int i = 0; i < values.Length; i++)
            {
                _enumToIndexMap[values[i]] = i;
            }
        }

        /// <summary>
        /// Sets a high priority to a Genies Virtual Camera if it's found inside the
        /// Genies Virtual Camera list. Makes sure the priorities of the other cameras
        /// are set to zero.
        /// </summary>
        /// <remarks>Use the catalog as the parameter directly, it avoids casting between index and camera catalog</remarks>
        /// <param name="virtualCameraIndex">The index of the Genies Virtual Camera to set as active</param>
        /// <param name="overrideBrainMovement">Whether cinemachine brain's movement should be overriden to move towards new virtual camera.
        /// This can be helpful when you want cleaner movement between weird angles or a different duration.</param>
        public async UniTask ActivateVirtualCamera(TEnum virtualCameraIndex, bool overrideBrainMovement = false)
        {
            if (!_enumToIndexMap.TryGetValue(virtualCameraIndex, out var index))
            {
                return;
            }

            var geniesVCam = _geniesVirtualCameras[index];
            var targetVCam = geniesVCam.VirtualCamera;

            if (_activeVCam == targetVCam)
            {
                return;
            }

            await _activationLock.WaitAsync();

            try
            {
                bool firstActivation = _activeVCam == null;

                await CreateCinemachineBrainIfNeeded();

                // If this is the first virtual camera to be added, add a slight delay
                // to allow for the camera system to position the other cameras
                if (firstActivation)
                {
                    await UniTask.WaitForSeconds(_firstCameraDelay);
                }
            }
            finally
            {
                _activationLock.Release();
            }

            foreach (var gvc in _geniesVirtualCameras)
            {
                if (gvc == null)
                {
                    continue;
                }

                gvc.CameraTypeScript.ToggleBehaviour(false);
                gvc.VirtualCamera.Priority = _minPriority;
            }

            _activeVCam = targetVCam;
            _activeCameraController = geniesVCam.CameraTypeScript;

            _activeCameraController.ConfigureVirtualCamera();
            _activeCameraController.ToggleBehaviour(true);

            // Overrides cinemachine brain to move to target virtual camera before resetting it to normal blending
            if (overrideBrainMovement)
            {
                await TransitionToVirtualCameraWithOverride(_activeVCam);
            }
            else
            {
                _activeVCam.Priority = _mainPriority;
            }
        }

        /// <summary>
        /// Overrides the cinemachine brain to move to the virtual camera over the course of the provided duration.
        /// This can make blending between cameras better at weird angles.
        /// </summary>
        /// <param name="targetVCam">The virtual camera to move</param>
        /// <param name="duration">How long the transition should take</param>
        public async UniTask TransitionToVirtualCameraWithOverride(
            CinemachineVirtualCameraBase targetVCam,
            float duration = 1.0f)
        {
            _activeCameraController.ConfigureVirtualCamera();
            _activeCameraController.ToggleBehaviour(true);

            await TransitionWithOverride(
                targetVCam,
                duration
            );
        }

        /// <summary>
        /// Creates a virtual camera and moves the camera view to the provided position/rotation over the course of the provided duration.
        /// The virtual camera is destroyed when finished.
        /// </summary>
        /// <param name="position">The position to move</param>
        /// <param name="rotation">The rotation to move to</param>
        /// <param name="fieldOfView">The field of view to change to</param>
        /// <param name="duration">How long the transition should take</param>
        public async UniTask TransitionToLocationWithOverride(
            Vector3 position,
            Quaternion rotation,
            float fieldOfView,
            float duration = 1.0f)
        {
            if (_transitionCamera == null)
            {
                _transitionCamera = new GameObject("TransitionCamera")
                    .AddComponent<CinemachineCamera>();
            }

            _transitionCamera.transform.SetPositionAndRotation(position, rotation);
            _transitionCamera.Lens.FieldOfView = fieldOfView;

            await TransitionWithOverride(
                _transitionCamera,
                duration,
                onComplete: () =>
                {
                    GameObject.Destroy(_transitionCamera.gameObject);
                    _transitionCamera = null;
                });
        }


        private async UniTask TransitionWithOverride(
            CinemachineVirtualCameraBase targetCam,
            float duration,
            Action onComplete = null)
        {
            await CreateCinemachineBrainIfNeeded();

            // Disable all other vcams
            foreach (var gvc in _geniesVirtualCameras)
            {
                if (gvc == null)
                {
                    continue;
                }

                if (gvc.VirtualCamera != targetCam)
                {
                    gvc.CameraTypeScript.ToggleBehaviour(false);
                    gvc.VirtualCamera.Priority = _minPriority;
                }
            }

            targetCam.Priority = _mainPriority;

            int id = _brain.SetCameraOverride(
                overrideId: -1,
                priority: int.MaxValue,
                camA: null,
                camB: targetCam,
                weightB: 0f,
                deltaTime: -1f
            );

            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float easedT = UIVirtual.EasedValue(0f, 1f, t, Ease.InOutQuad);

                _brain.SetCameraOverride(
                    id,
                    int.MaxValue,
                    null,
                    targetCam,
                    Mathf.Clamp01(easedT / duration),
                    Time.deltaTime
                );

                await UniTask.Yield();
            }

            _brain.ReleaseCameraOverride(id);
            _brain.ActiveBlend = null;

            onComplete?.Invoke();
        }



        public void DeactivateAllVirtualCameras()
        {
            foreach (GeniesVirtualCamera geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }

                geniesVirtualCamera.CameraTypeScript.ToggleBehaviour(false);
                geniesVirtualCamera.VirtualCamera.enabled = false;

            }

            _activeVCam = null;
            _activeCameraController = null;
        }

        /// <summary>
        /// Resets the cinemachine camera to its original focal length and sensor size to avoid broken transitions.
        /// </summary>
        public void ResetCinemachineCamera()
        {
            CinemachineCamera.focalLength = _originalCamFocalLength;
            CinemachineCamera.sensorSize = _originalCamSensorSize;
        }

    #region Focus Cameras Methods

        /// <summary>
        /// Sets the fullscreen mode to all focus cameras.
        /// Makes sure the Focus Camera Controllers focus on the object as if it were in a
        /// fullscreen mode.
        /// </summary>
        /// <param name="isFullScreen">The bool value to set the full screen mode of the Focus Cameras</param>
        public void SetFullScreenModeInFocusCameras(bool isFullScreen)
        {
            foreach (GeniesVirtualCamera geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetFullScreen(isFullScreen);
                }
            }
        }

        /// <summary>
        /// Updates the viewport object of all focus cameras.
        /// This is needed for all focus cameras to behave correctly.
        /// </summary>
        /// <param name="newViewport">The new viewport to set on the cameras</param>
        public void UpdateViewportInFocusCameras(RectTransform newViewport)
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetTargetViewport(newViewport);
                }
            }
        }

        /// <summary>
        /// Sets a focusable object for the focus camera to look at.
        /// </summary>
        /// <param name="focusCameraIndex">catalog (which camera) to use as the focus camera</param>
        /// <param name="focusable">the focusable to have the camera focus on</param>
        public void SetFocusableInFocusCamera(TEnum focusCameraIndex, IFocusable focusable)
        {
            if (_enumToIndexMap.TryGetValue(focusCameraIndex, out var index))
            {
                GeniesVirtualCamera geniesVirtualCamera = _geniesVirtualCameras[index];

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetTargetFocusable(focusable);
                }
            }
        }

        public void SetFocusModeInFocusCamera(TEnum focusCameraIndex, FocusCameraMode targetFocusMode)
        {
            if (_enumToIndexMap.TryGetValue(focusCameraIndex, out var index))
            {
                GeniesVirtualCamera geniesVirtualCamera = _geniesVirtualCameras[index];

                if (geniesVirtualCamera.CameraType == VirtualCameraType.FocusCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as FocusCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.Handler.ChangeFocusMode(targetFocusMode);
                }
            }
        }

    #endregion

    #region Animated Cameras Methods
        /// <summary>
        /// Sets the animated camera to follow onto all animated cameras.
        /// </summary>
        /// <param name="newAnimatedCamera">The target animated camera to follow</param>
        public void SetAnimatedCamera(Camera newAnimatedCamera)
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }

                if (geniesVirtualCamera.CameraType == VirtualCameraType.AnimatedCamera)
                {
                    var cameraScript = geniesVirtualCamera.CameraTypeScript as AnimatedCameraController;

                    if (cameraScript == null)
                    {
                        return;
                    }

                    cameraScript.SetAnimatedCamera(newAnimatedCamera);
                }
            }
        }

    #endregion

    #region Third Person Cameras Methods

        /// <summary>
        /// Sets the Follow & Look At fields on the Orbital Third Person Cameras
        /// </summary>
        public void SetFollowAndLookAtOnOrbitalThirdPersonCameras()
        {
            foreach (var geniesVirtualCamera in _geniesVirtualCameras)
            {
                if (geniesVirtualCamera == null)
                {
                    continue;
                }

                if (geniesVirtualCamera.CameraTypeScript is OrbitalThirdPersonCameraController cameraScript)
                {
                    cameraScript.SetFollow();
                    cameraScript.SetLookAt();
                }
            }
        }

    #endregion
    }
}
