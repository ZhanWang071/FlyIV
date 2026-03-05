using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Unity.Cinemachine;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Genies.Sdk.Samples.Common
{
    /// <summary>
    /// Custom input controller for Cinemachine 3.x that ignores touches which started inside a designated touchzone.
    /// Add this component to your CinemachineCamera instead of the default CinemachineInputAxisController.
    /// </summary>
    [RequireComponent(typeof(CinemachineCamera))]
    public class UIFilteredCinemachineInput : MonoBehaviour
    {
        [Header("Touch Settings (Mobile)")] [Tooltip("Sensitivity multiplier for touch input")] [SerializeField]
        private float _touchSensitivity = 0.1f;

        [Tooltip("Invert X axis")] [SerializeField]
        private bool _invertX = false;

        [Tooltip("Invert Y axis")] [SerializeField]
        private bool _invertY = true;

        [Header("Mouse Settings (Desktop)")]
        [Tooltip("Which mouse button activates camera rotation. -1 for always active.")]
        [SerializeField]
        private int _mouseButton = 0;

        [Tooltip("Mouse sensitivity multiplier")] [SerializeField]
        private float _mouseSensitivity = 0.1f;

        private int? _activeTouchId = null;
        private Vector2 _lastTouchPosition;

        private Vector2 _lastMousePosition;
        private CinemachineOrbitalFollow _orbitalFollow;

        private void Awake()
        {
            _orbitalFollow = GetComponent<CinemachineOrbitalFollow>();
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void LateUpdate()
        {
            if (_orbitalFollow == null)
            {
                return;
            }

            Vector2 input = IsMobilePlatform() ? GetTouchInput() : GetMouseInput();

            _orbitalFollow.HorizontalAxis.Value += -input.x;
            _orbitalFollow.VerticalAxis.Value += -input.y;
        }

        private Vector2 GetMouseInput()
        {
            var mouse = Mouse.current;
            if (mouse == null)
            {
                return Vector2.zero;
            }

            bool mouseActive = _mouseButton < 0;
            if (!mouseActive)
            {
                mouseActive = _mouseButton switch
                {
                    0 => mouse.leftButton.isPressed,
                    1 => mouse.rightButton.isPressed,
                    2 => mouse.middleButton.isPressed,
                    _ => false
                };
            }

            if (!mouseActive)
            {
                return Vector2.zero;
            }

            Vector2 delta = mouse.delta.ReadValue();

            return new Vector2(
                delta.x * _mouseSensitivity * (_invertX ? -1f : 1f),
                delta.y * _mouseSensitivity * (_invertY ? -1f : 1f)
            );
        }

        private Vector2 GetTouchInput()
        {
            var tracker = UITouchTracker.Instance;

            // If no tracker, allow all touches
            if (tracker == null)
            {
                return GetAnyTouchInput();
            }

            // Check if our active touch is still valid
            if (_activeTouchId.HasValue)
            {
                Touch? activeTouch = GetTouchById(_activeTouchId.Value);

                if (!activeTouch.HasValue || !activeTouch.Value.isInProgress)
                {
                    _activeTouchId = null;
                }
                else
                {
                    return CalculateTouchDelta(activeTouch.Value);
                }
            }

            // Look for a new unblocked touch
            if (tracker.TryGetFirstUnblockedTouch(out Touch newTouch))
            {
                if (newTouch.began)
                {
                    _activeTouchId = newTouch.touchId;
                    _lastTouchPosition = newTouch.screenPosition;
                }
                else if (!_activeTouchId.HasValue)
                {
                    _activeTouchId = newTouch.touchId;
                    _lastTouchPosition = newTouch.screenPosition;
                }
            }

            return Vector2.zero;
        }

        private Vector2 GetAnyTouchInput()
        {
            if (Touch.activeTouches.Count == 0)
            {
                return Vector2.zero;
            }

            var touch = Touch.activeTouches[0];

            if (!_activeTouchId.HasValue)
            {
                _activeTouchId = touch.touchId;
                _lastTouchPosition = touch.screenPosition;
                return Vector2.zero;
            }

            if (_activeTouchId.Value == touch.touchId)
            {
                return CalculateTouchDelta(touch);
            }

            return Vector2.zero;
        }

        private Vector2 CalculateTouchDelta(Touch touch)
        {
            Vector2 currentPos = touch.screenPosition;
            Vector2 delta = currentPos - _lastTouchPosition;
            _lastTouchPosition = currentPos;

            return new Vector2(
                delta.x * _touchSensitivity * (_invertX ? -1f : 1f),
                delta.y * _touchSensitivity * (_invertY ? -1f : 1f)
            );
        }

        private Touch? GetTouchById(int touchId)
        {
            foreach (var touch in Touch.activeTouches)
            {
                if (touch.touchId == touchId)
                {
                    return touch;
                }
            }

            return null;
        }

        private bool IsMobilePlatform()
        {
#if UNITY_EDITOR
            return UnityEngine.Device.SystemInfo.deviceType == DeviceType.Handheld;
#else
        return Application.isMobilePlatform;
#endif
        }

    }
}
