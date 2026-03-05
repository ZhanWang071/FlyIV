using System;
using System.Collections;
using System.Threading;
using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using Genies.CameraSystem.Focusable;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that rotates around a target Focusable object
    /// Allows for auto-rotation when no input is detected
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera), typeof(CinemachineInputAxisController))]
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class OrbitalThirdPersonCameraController : MonoBehaviour, ICameraType
#else
    public class OrbitalThirdPersonCameraController : MonoBehaviour, ICameraType
#endif
    {
        [Header("Anchors Container")]
        [SerializeField] private Transform anchorsContainer;

        [Header("Target Bounds")]
        [SerializeField] private CameraFocusPoint cameraFocusPoint;

        [Header("Initial Settings")]
        [SerializeField] private Vector3 initialPosition;
        [SerializeField] private Vector3 initialRotation;

        [Header("Field of View")]
        [SerializeField] private float fieldOfView;

        [Header("Auto Rotation Settings")]
        [SerializeField] private float autoRotationWaitTime;
        [SerializeField] private float autoRotationDuration;

        [Header("Orbital Rigs Settings")]
        [SerializeField] private float topRigHeight;
        [SerializeField] private float topRigRadius;
        [SerializeField] private float mediumRigHeight;
        [SerializeField] private float mediumRigRadius;
        [SerializeField] private float bottomRigHeight;
        [SerializeField] private float bottomRigRadius;

        private CinemachineCamera _cinemachineCamera;
        private CinemachineInputAxisController _inputProvider;
        private CinemachineRotationComposer _rotationComposer;
        private CinemachineFreeLookModifier _freeLookModifier;
        private CinemachineOrbitalFollow _orbitalFollow;

        private float _currentValue;
        private float _timer;
        private UIAnimator _rotationAnimation;
        private Coroutine _rotationLoopCoroutine;
        [SerializeField] private bool isAutoRotationEnabled = true;
        private bool _isRotationLoopActive;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Sets the initial configurations of the Free Look Virtual Camera
        /// and the behaviour of the autorotation feature
        /// </summary>
        public void ConfigureVirtualCamera()
        {
            transform.position = initialPosition;
            transform.rotation = Quaternion.Euler(initialRotation);

            if (_inputProvider == null)
            {
                _inputProvider = GetComponent<CinemachineInputAxisController>();
            }

            if (_inputProvider == null)
            {
                _inputProvider = gameObject.AddComponent<CinemachineInputAxisController>();
            }

            _inputProvider.enabled = false;

            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = GetComponent<CinemachineCamera>();
            }

            if (_cinemachineCamera == null)
            {
                _cinemachineCamera = gameObject.AddComponent<CinemachineCamera>();
            }

            if (_rotationComposer == null)
            {
                _rotationComposer = GetComponent<CinemachineRotationComposer>();
            }

            if (_rotationComposer == null)
            {
                _rotationComposer = gameObject.AddComponent<CinemachineRotationComposer>();
            }

            if (_freeLookModifier == null)
            {
                _freeLookModifier = GetComponent<CinemachineFreeLookModifier>();
            }

            if (_freeLookModifier == null)
            {
                _freeLookModifier = gameObject.AddComponent<CinemachineFreeLookModifier>();
            }

            if (_orbitalFollow == null)
            {
                _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
            }

            if (_orbitalFollow == null)
            {
                _orbitalFollow = gameObject.AddComponent<CinemachineOrbitalFollow>();
            }

            _cinemachineCamera.Lens.FieldOfView = fieldOfView;

            _orbitalFollow.Orbits.Top.Height = topRigHeight;
            _orbitalFollow.Orbits.Top.Radius = topRigRadius;

            _orbitalFollow.Orbits.Center.Height = mediumRigHeight;
            _orbitalFollow.Orbits.Center.Radius = mediumRigRadius;

            _orbitalFollow.Orbits.Bottom.Height = bottomRigHeight;
            _orbitalFollow.Orbits.Bottom.Radius = bottomRigRadius;
        }

        /// <summary>
        /// Toggles the behaviour of the camera
        /// </summary>
        /// <param name="value">Toggles the behaviour on/off</param>
        public void ToggleBehaviour(bool value)
        {
            gameObject.SetActive(value);
            _inputProvider.enabled = value;

            if (value && isAutoRotationEnabled)
            {
                // X-axis input
                _inputProvider.Controllers[0].Input.InputAction.action.performed += StopAutoRotationAnimation;
                _currentValue = _inputProvider.Controllers[0].InputValue;
                AutoRotationCheck().Forget();
            }
            else
            {
                _inputProvider.Controllers[0].Input.InputAction.action.performed -= StopAutoRotationAnimation;
                StopAutoRotationCheck();
            }
        }

        /// <summary>
        /// Enable the auto-rotation behaviour and check when
        /// no input is being detected
        /// </summary>
        private async UniTaskVoid AutoRotationCheck()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            await UniTask.WaitUntil(() => _cinemachineCamera.Follow != null && _cinemachineCamera.LookAt != null, cancellationToken: token);

            while (!token.IsCancellationRequested)
            {
                if (Math.Abs(_currentValue - _inputProvider.Controllers[0].InputValue) > 0.01f)
                {
                    _currentValue = _inputProvider.Controllers[0].InputValue;
                    _timer = 0f;
                }
                else
                {
                    _timer += Time.deltaTime;

                    if (_timer >= autoRotationWaitTime)
                    {
                        StartAutoRotation();
                    }
                }
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
            }
        }

        /// <summary>
        /// Disables the auto-rotation behaviour
        /// </summary>
        private void StopAutoRotationCheck()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }

            // Set flag to stop rotation loop
            _isRotationLoopActive = false;
            StopAutoRotationAnimation(default);
        }

        /// <summary>
        /// Sets the Follow field on the Virtual Camera
        /// </summary>
        public void SetFollow()
        {
            var follow = new GameObject("Follow Anchor " + name);
            follow.transform.parent = anchorsContainer;
            follow.transform.position = cameraFocusPoint.GetBounds().center;
            _cinemachineCamera.Follow = follow.transform;
        }

        /// <summary>
        /// Sets the Look At field on the Virtual Camera
        /// </summary>
        public void SetLookAt()
        {
            var lookAt = new GameObject("Look At Anchor " + name);
            lookAt.transform.parent = anchorsContainer;
            Bounds bounds = cameraFocusPoint.GetBounds();
            lookAt.transform.position = bounds.center;
            lookAt.transform.localScale = new Vector3(bounds.size.x, bounds.size.y, bounds.size.z);
            _cinemachineCamera.LookAt = lookAt.transform;
        }

        /// <summary>
        /// Begins auto-rotation feature when no input is detected after a waiting time.
        /// </summary>
        private void StartAutoRotation()
        {
            if (_rotationLoopCoroutine != null)
            {
                return;
            }

            _isRotationLoopActive = true;
            _rotationLoopCoroutine = StartCoroutine(RotationLoop());
        }

        private IEnumerator RotationLoop()
        {
            while (_isRotationLoopActive)
            {
                float startValue = _inputProvider.Controllers[0].InputValue;
                float endValue = startValue + 360f;

                _rotationAnimation = AnimateVirtual.Float(startValue, endValue, autoRotationDuration,
                    x => _inputProvider.Controllers[0].InputValue = x);

                yield return _rotationAnimation.WaitForCompletion();

                if (!_isRotationLoopActive)
                {
                    break;
                }
            }

            // Clean up when loop exits
            _rotationLoopCoroutine = null;
        }

        /// <summary>
        /// Stops auto-rotation when input is detected.
        /// </summary>
        /// <param name="context"></param>
        private void StopAutoRotationAnimation(InputAction.CallbackContext context)
        {
            _isRotationLoopActive = false;

            // Stop the coroutine if it's still running
            if (_rotationLoopCoroutine != null)
            {
                StopCoroutine(_rotationLoopCoroutine);
                _rotationLoopCoroutine = null;
            }

            // Terminate any active rotation animation
            if (_rotationAnimation != null)
            {
                _rotationAnimation.Terminate();
                _rotationAnimation = null;
            }
        }

        private void OnDestroy()
        {
            if (isAutoRotationEnabled)
            {
                _inputProvider.Controllers[0].Input.InputAction.action.performed -= StopAutoRotationAnimation;
                StopAutoRotationCheck();
            }
        }
    }
}
