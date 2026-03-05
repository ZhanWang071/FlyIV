using System;
using System.Threading;
using Unity.Cinemachine;
using Cysharp.Threading.Tasks;
using Genies.CameraSystem.Focusable;
using UnityEngine;
using UnityEngine.Assertions;

namespace Genies.CameraSystem
{
    /// <summary>
    /// Cinemachine Camera Type that focuses on a target Focusable object through a Rect Transform viewport
    /// </summary>
    [RequireComponent(typeof(CinemachineMixingCamera), typeof(FocusCameraModeHandler))]
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class FocusCameraController : MonoBehaviour, ICameraType
#else
    public class FocusCameraController : MonoBehaviour, ICameraType
#endif
    {
        // Events
        public static Action<Vector3> OnFocusSetTo;

        [Header("Anchors Container")]
        public Transform anchorsContainer;

        [Header("Target Viewport")]
        public RectTransform targetViewport;

        [Header("Target Camera Focus Point")]
        public CameraFocusPoint cameraFocusPoint;

        [Header("Mixing Camera Settings")]
        public float fieldOfView = 40f;
        public Vector2 sensorSize = new Vector2(25f, 25f);
        public Vector3 zoomInInitialOffset = new Vector3(0f, 0f, 0.1f);
        public Vector3 zoomOutInitialOffset = new Vector3(0f, 0f, 0.5f);

        // private fields
        private CinemachineMixingCamera _mixingCamera;
        private CinemachineCamera _zoomInVirtualCamera;
        private CinemachineCamera _zoomOutVirtualCamera;
        private CinemachinePositionComposer _zoomInFramingTransposer;
        private CinemachinePositionComposer _zoomOutFramingTransposer;

        private Bounds _targetBounds;
        private Vector3 _boundsCenter = Vector3.zero;
        private Vector3 _boundsSize = Vector3.zero;

        private Rect _screenSpaceViewportRect;

        private GameObject _followAnchor;
        private GameObject _lookAtAnchor;

        private float _cameraDistance;
        private Vector3 _movementDirection;
        private Vector3 _objectOffset;

        private CameraState _zoomInCurrentCameraState;
        private CameraState _zoomOutCurrentCameraState;

        private IFocusable _currentFocusableObject;

        private bool _hasBeenConfigured;
        public bool _isFullScreen;

        private Camera _MainCamera => Camera.main;
        private float _HalfCameraFieldOfView => fieldOfView * 0.5f;

        private FocusCameraModeHandler handler;
        public FocusCameraModeHandler Handler => handler;

        private CancellationTokenSource _cancellationTokenSource;

        /// <summary>
        /// Sets the initial configuration of the virtual cameras.
        /// Avoids the need for modifying the Cinemachine components.
        /// </summary>
        public void ConfigureVirtualCamera()
        {
            // Get the Mixing Camera component.
            if (_mixingCamera == null)
            {
                _mixingCamera = GetComponent<CinemachineMixingCamera>();
            }

            if (_mixingCamera == null)
            {
                _mixingCamera = gameObject.AddComponent<CinemachineMixingCamera>();
            }

            // get the Handler component.
            if (handler == null)
            {
                handler = GetComponent<FocusCameraModeHandler>();
            }

            if (handler == null)
            {
                handler = gameObject.AddComponent<FocusCameraModeHandler>();
            }

            // Get the virtual cameras from the Mixing Camera.
            // Mixing Camera SHOULD have 2 children: one for zooming in
            // and another one for zooming out.
            Assert.IsTrue(_mixingCamera.ChildCameras.Count == 2);

            _zoomInVirtualCamera = _mixingCamera.ChildCameras[0] as CinemachineCamera;
            _zoomOutVirtualCamera = _mixingCamera.ChildCameras[1] as CinemachineCamera;

            // Check if children are not null.
            if (_zoomInVirtualCamera == null || _zoomOutVirtualCamera == null)
            {
                Debug.Log("Virtual Cameras are missing on Mixing Camera");
                return;
            }

            // Set cameras to Physical. This avoids bugs.
            _zoomInVirtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Physical;
            _zoomOutVirtualCamera.Lens.ModeOverride = LensSettings.OverrideModes.Physical;

            _zoomInVirtualCamera.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Vertical;
            _zoomOutVirtualCamera.Lens.PhysicalProperties.GateFit = Camera.GateFitMode.Vertical;

            // Set the FOV for the virtual cameras.
            _zoomInVirtualCamera.Lens.FieldOfView = fieldOfView;
            _zoomOutVirtualCamera.Lens.FieldOfView = fieldOfView;

            _zoomInVirtualCamera.Lens.PhysicalProperties.SensorSize = sensorSize;
            _zoomOutVirtualCamera.Lens.PhysicalProperties.SensorSize = sensorSize;

            // Get the Framing Transposers from the virtual cameras.
            if (_zoomInFramingTransposer == null)
            {
                _zoomInFramingTransposer = _zoomInVirtualCamera.GetComponent<CinemachinePositionComposer>();
            }

            if (_zoomInFramingTransposer == null)
            {
                _zoomInFramingTransposer = _zoomInVirtualCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            }

            if (_zoomOutFramingTransposer == null)
            {
                _zoomOutFramingTransposer = _zoomOutVirtualCamera.GetComponent<CinemachinePositionComposer>();
            }

            if (_zoomOutFramingTransposer == null)
            {
                _zoomOutFramingTransposer = _zoomOutVirtualCamera.gameObject.AddComponent<CinemachinePositionComposer>();
            }

            if (!_hasBeenConfigured)
            {
                // Set the initial configurations to the framing transposers.
                // This is mandatory to avoid miscalculations when first starting the camera.
                _zoomInFramingTransposer.TargetOffset = zoomInInitialOffset;
                _zoomOutFramingTransposer.TargetOffset = zoomOutInitialOffset;
            }

            // Create and assign the Follow anchor to the virtual cameras.
            if (_followAnchor == null)
            {
                _followAnchor = new GameObject("Follow Anchor " + name) { transform = { parent = anchorsContainer } };
            }

            _zoomInVirtualCamera.Follow = _followAnchor.transform;
            _zoomOutVirtualCamera.Follow = _followAnchor.transform;

            // Create and assign the Look At anchor to the virtual cameras.
            if (_lookAtAnchor == null)
            {
                _lookAtAnchor = new GameObject("Look At Anchor " + name) { transform = { parent = anchorsContainer } };
            }

            _zoomInVirtualCamera.LookAt = _lookAtAnchor.transform;
            _zoomOutVirtualCamera.LookAt = _lookAtAnchor.transform;

           _zoomInVirtualCamera.Target.TrackingTarget = _followAnchor.transform;
           _zoomOutVirtualCamera.Target.TrackingTarget = _followAnchor.transform;

           _zoomInFramingTransposer.CameraDistance = 0.01f;
           _zoomOutFramingTransposer.CameraDistance = 0.01f;

            SetTargetFocusable(cameraFocusPoint);
            SetTargetViewport(targetViewport);

            _hasBeenConfigured = true;
        }

        /// <summary>
        /// Activates fullscreen mode on focus camera
        /// </summary>
        /// <param name="isFullScreen">boolean to handle fullscreen mode</param>
        public void SetFullScreen(bool isFullScreen)
        {
            _isFullScreen = isFullScreen;
            FrameFocusable();
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
                BeginFocus().Forget();
            }
            else
            {
                StopFocus();
            }
        }

        private async UniTaskVoid BeginFocus()
        {
            if (_zoomInVirtualCamera != null)
            {
                _zoomInVirtualCamera.enabled = true;
            }

            if (_zoomOutVirtualCamera != null)
            {
                _zoomOutVirtualCamera.enabled = true;
            }

            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            await UniTask.WaitUntil(() => targetViewport != null && targetViewport.hasChanged, cancellationToken: token);

            while (!token.IsCancellationRequested)
            {
                FrameFocusable();
                await UniTask.Yield(PlayerLoopTiming.PostLateUpdate, token);
            }
        }

        private void StopFocus()
        {
            if (_zoomInVirtualCamera != null)
            {
                _zoomInVirtualCamera.enabled = false;
            }

            if (_zoomOutVirtualCamera != null)
            {
                _zoomOutVirtualCamera.enabled = false;
            }

            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        /// <summary>
        /// Get target focusable within the frame, using a target Rect Transform
        /// </summary>
        private void FrameFocusable()
        {
            if (targetViewport == null || _currentFocusableObject == null)
            {
                return;
            }

            targetViewport.hasChanged = false;

            // Focus the virtual camera on the target viewport.
            FocusOnTargetViewport(targetViewport);

            // Check if the virtual cameras have reached their final destination,
            // set on the previous FocusOnTarget() method.
            if (_isFullScreen ||
                _zoomInVirtualCamera.transform.position != _zoomInCurrentCameraState.GetFinalPosition() ||
                _zoomOutVirtualCamera.transform.position != _zoomOutCurrentCameraState.GetFinalPosition())
            {
                return;
            }

            //Update height position to frame focusable on viewport
            UpdateVirtualCamerasHeight();
        }

    #if UNITY_EDITOR
        private void OnValidate()
        {
            if (targetViewport != null)
            {
                targetViewport.hasChanged = true;
            }
        }
    #endif

        /// <summary>
        /// Focuses the camera on a target object, using a RectTransform as a
        /// viewport and positioning the camera closer or further from the object.
        /// </summary>
        /// <param name="viewport">The viewport area for focusing the object</param>
        private void FocusOnTargetViewport(RectTransform viewport)
        {
            // Get the center and size of the target bounds.
            _boundsCenter = _targetBounds.center;
            _boundsSize = _targetBounds.size;

            // Make sure that the size of the bounds is not negative.
            _boundsSize.x = Mathf.Abs(_boundsSize.x);
            _boundsSize.y = Mathf.Abs(_boundsSize.y);
            _boundsSize.z = Mathf.Abs(_boundsSize.z);

            // Apply the proportion to the height of the target and calculate its half-height.
            float halfObjectHeight;

            if (_isFullScreen)
            {
                halfObjectHeight = _boundsSize.y * 0.5f;
            }
            else
            {
                // Get Rect in Screen Space of Target Viewport.
                _screenSpaceViewportRect = GetRectScreenSpaceFromRectTransform(viewport);

                // Get the proportion of the full height of the screen and the viewport rect height.
                var inverseViewportScreenProportion = Screen.height / _screenSpaceViewportRect.height;

                halfObjectHeight = (_boundsSize.y * inverseViewportScreenProportion) * 0.5f;
            }

            // Calculate the camera distance.
            _cameraDistance = halfObjectHeight / Mathf.Tan(Mathf.Deg2Rad * _HalfCameraFieldOfView);

            // Set the Look At anchor position and scale.
            _lookAtAnchor.transform.localPosition = _boundsCenter;
            _lookAtAnchor.transform.localScale = new Vector3(_boundsSize.x, _boundsSize.y, _boundsSize.z);

            // Set the Follow anchor position.
            _followAnchor.transform.localPosition = _boundsCenter;
            OnFocusSetTo?.Invoke(_boundsCenter);

            // Set the offset on the Zoom In & Zoom Out Framing Transposers.
            _objectOffset = _cameraDistance * _movementDirection.normalized;
            _zoomInFramingTransposer.TargetOffset = zoomInInitialOffset + _objectOffset;
            _zoomOutFramingTransposer.TargetOffset = zoomOutInitialOffset + _objectOffset;

            // Update the current camera state.
            _zoomInCurrentCameraState = _zoomInVirtualCamera.State;
            _zoomOutCurrentCameraState = _zoomOutVirtualCamera.State;
        }

        /// <summary>
        /// Updates the camera height to the target object, using the currentViewport's center as the point
        /// that the camera should align itself to.
        /// </summary>
        private void UpdateVirtualCamerasHeight()
        {
            // Convert Rect to World Point data & retrieve target direction to calculate the offset on the Y axis.
            var screenPoint = new Vector2(_screenSpaceViewportRect.x + _screenSpaceViewportRect.width * 0.5f,
                                          _screenSpaceViewportRect.y + _screenSpaceViewportRect.height * 0.5f);
            // The world position of the center of the viewport
            var worldPoint = _MainCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, _cameraDistance));
            //At this point, the avatar is located at the center of the screen.
            var centerWorldPoint = _MainCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, _cameraDistance));
            var screenDirection = centerWorldPoint - worldPoint;

            // Check if y offset is different than zero. Return if true.
            if (screenDirection.y == 0f)
            {
                return;
            }

            // Screen direction should only have a Y Axis value.
            screenDirection.x = 0f;
            screenDirection.z = 0f;

            // Apply Y axis offset to virtual cameras & Look At anchor.
            var verticalOffset = screenDirection.y * Vector3.up;

            // The Look At value should move along the cameras so that the camera doesn't lose the initial angle of focus
            _lookAtAnchor.transform.localPosition = _boundsCenter + verticalOffset;
            _zoomInFramingTransposer.TargetOffset = zoomInInitialOffset + _objectOffset + verticalOffset;
            _zoomOutFramingTransposer.TargetOffset = zoomOutInitialOffset + _objectOffset + verticalOffset;
        }

        /// <summary>
        /// Calculates a Rect object out of a given RectTransform parameter.
        /// </summary>
        /// <param name="rect">The RectTransform to get the Rect from</param>
        /// <returns>The Rect object of the RectTransform</returns>
        private Rect GetRectScreenSpaceFromRectTransform(RectTransform rect)
        {
            var lossyScale = rect.lossyScale;
            var size = Vector2.Scale(rect.rect.size,
                new Vector3(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y), Mathf.Abs(lossyScale.z)));
            return new Rect((Vector2)rect.position - (size * rect.pivot), size);
        }

        /// <summary>
        /// Set the IFocusable object to use.
        /// </summary>
        /// <param name="focusable">The IFocusable object to use</param>
        public void SetTargetFocusable(IFocusable focusable)
        {
            if (focusable == null)
            {
                return;
            }

            // Set the IFocusable object.
            _currentFocusableObject = focusable;

            // Save the bounds of the IFocusable object.
            _targetBounds = _currentFocusableObject.GetBounds();

            // Get the target view direction from the IFocusable object.
            _movementDirection = _currentFocusableObject.TargetViewDirection;
        }

        /// <summary>
        /// Sets a viewport to be used as target.
        /// </summary>
        /// <param name="viewport"></param>
        public void SetTargetViewport(RectTransform viewport)
        {
            if (viewport == null)
            {
                return;
            }

            // Set the target viewport to use
            targetViewport = viewport;

            targetViewport.hasChanged = true;
        }

        private void OnDestroy()
        {
            StopFocus();

            if (_followAnchor != null)
            {
                Destroy(_followAnchor);
            }

            if (_lookAtAnchor != null)
            {
                Destroy(_lookAtAnchor);
            }
        }

        //For local testing of the viewport
    #if UNITY_EDITOR
        void OnDrawGizmos()
        {
            //Draws red rectangle out of the current viewport rect object.
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            DrawRect(_screenSpaceViewportRect);
        }

        void DrawRect(Rect rect)
        {
            Gizmos.DrawWireCube(new Vector3(rect.center.x, rect.center.y, 0.01f), new Vector3(rect.size.x, rect.size.y, 0.01f));
        }
    #endif
    }
}
