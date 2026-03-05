using System.Threading;
using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that follows a target animated camera.
    /// It follows the animation of the camera, configuring to match the physical camera's settings.
    /// </summary>
    [RequireComponent(typeof(AnimatedCameraState), typeof(CinemachineCamera))]
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class AnimatedCameraController : MonoBehaviour, ICameraType
#else
    public class AnimatedCameraController : MonoBehaviour, ICameraType
#endif
    {
        [Tooltip("The target camera to follow")]
        private Camera _animatedCamera;

        //internal
        private AnimatedCameraState _animatedCameraState;
        private CinemachineCamera _vCam;
        private CinemachineHardLockToTarget _vCamTarget;
        private CinemachineRotateWithFollowTarget _vCamRotateWithFollowTarget;

        private bool _followedLastFrame;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Sets the initial configuration to the Virtual Camera component
        /// for proper copying of the proxy Camera
        /// </summary>
        public void ConfigureVirtualCamera()
        {

            if (_vCam == null)
            {
                _vCam = GetComponent<CinemachineCamera>();
            }

            if (_vCam == null)
            {
                _vCam = gameObject.AddComponent<CinemachineCamera>();
            }

            if (_vCamTarget == null)
            {
                _vCamTarget = GetComponent<CinemachineHardLockToTarget>();
            }

            if (_vCamTarget == null)
            {
                _vCamTarget = gameObject.AddComponent<CinemachineHardLockToTarget>();
            }

            if (_vCamRotateWithFollowTarget == null)
            {
                _vCamRotateWithFollowTarget = GetComponent<CinemachineRotateWithFollowTarget>();
            }

            if (_vCamRotateWithFollowTarget == null)
            {
                _vCamRotateWithFollowTarget = gameObject.AddComponent<CinemachineRotateWithFollowTarget>();
            }

            _followedLastFrame = false;

            if (_animatedCameraState == null)
            {
                _animatedCameraState = GetComponent<AnimatedCameraState>();
            }

            if (_animatedCameraState == null)
            {
                _animatedCameraState = gameObject.AddComponent<AnimatedCameraState>();
            }
        }

        /// <summary>
        /// Toggles the behaviour of the camera
        /// </summary>
        /// <param name="value">Toggles the behaviour on/off</param>
        public void ToggleBehaviour(bool value)
        {
            gameObject.SetActive(value);

            if (value)
            {
                BeginFollowing().Forget();
            }
            else
            {
                StopFollowing();
            }
        }

        private async UniTaskVoid BeginFollowing()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            // Return if behaviour hasn't been activated
            await UniTask.WaitUntil(() => _animatedCamera != null, cancellationToken: token);

            if (_vCam == null)
            {
                return;
            }

            if(_animatedCameraState.FollowingProxy)
            {
                // First frame following
                if (!_followedLastFrame)
                {
                    // Updates the virtual camera's settings to match the physical camera's settings when following the proxy
                    // This is necessary to replicate how the camera is configured in the original animation
                    _vCam.Lens.ModeOverride = LensSettings.OverrideModes.Physical;
                    _vCam.Lens.PhysicalProperties.SensorSize = new Vector2(25f, 25f);
                    _vCam.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Overscan;
                }

                // Match the virtual camera's field of view to the proxy camera's field of view
                _vCam.Lens.FieldOfView = UpdateVirtualCameraFOV(_vCam, _animatedCamera);

                // Flag
                _followedLastFrame = true;
            }
            else
            {
                // If is not following anything in the first frame
                if (_followedLastFrame)
                {
                    // Resets them when not following the object
                    _vCam.Lens.ModeOverride = LensSettings.OverrideModes.None;
                }

                // Flag
                _followedLastFrame = false;
            }

            await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
        }

        private void StopFollowing()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        public void SetAnimatedCamera(Camera newAnimatedCamera)
        {
            _animatedCamera = newAnimatedCamera == null ? null : newAnimatedCamera;

            if (_vCam != null)
            {
                _vCam.Follow = newAnimatedCamera == null ? null : newAnimatedCamera.transform;
            }
        }

        /// <summary>
        /// Calculates the FOV value of a camera based on the sensor size and the focal length of the lens.
        /// Using the equation for field of view, this method returns the FOV value as degrees back to the
        /// Virtual Camera.
        /// </summary>
        /// <param name="vCam">The Virtual Camera to configure</param>
        /// <param name="proxyCam">The Proxy Camera to get the focal length from</param>
        /// <returns>The FOV value of the virtual camera</returns>
        private float UpdateVirtualCameraFOV(CinemachineCamera vCam, Camera proxyCam)
        {
            var sensorHeight = vCam.Lens.PhysicalProperties.SensorSize.y;
            var focalLength = proxyCam.focalLength;

            return 2f * Mathf.Atan(sensorHeight / (2 * focalLength)) * Mathf.Rad2Deg;
        }

        private void OnDestroy()
        {
            StopFollowing();
        }
    }
}
