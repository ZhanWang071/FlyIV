using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace Genies.Sdk.Samples.Common
{
    /// <summary>
    /// Tracks which touches started inside the designated touchzone and should be ignored for camera input.
    /// Touches OUTSIDE the touchzone are allowed for camera rotation.
    /// </summary>
    public class UITouchTracker : MonoBehaviour
    {
        [Header("Touchzone")]
        [Tooltip(
            "The RectTransform that defines the movement control area. Touches starting here will be blocked from camera input.")]
        [SerializeField]
        private RectTransform _touchZone;

        [Tooltip("The canvas containing the touchzone (needed for coordinate conversion)")] [SerializeField]
        private Canvas _canvas;

        private readonly HashSet<int> _blockedTouchIds = new HashSet<int>();

        private static UITouchTracker _instance;
        public static UITouchTracker Instance => _instance;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
        }

        private void OnEnable()
        {
            EnhancedTouchSupport.Enable();
        }

        private void OnDisable()
        {
            EnhancedTouchSupport.Disable();
        }

        private void Update()
        {
            foreach (var touch in Touch.activeTouches)
            {
                int touchId = touch.touchId;

                if (touch.began)
                {
                    if (IsTouchInsideZone(touch.screenPosition))
                    {
                        _blockedTouchIds.Add(touchId);
                    }
                }
                else if (touch.ended || !touch.isInProgress)
                {
                    _blockedTouchIds.Remove(touchId);
                }
            }
        }

        /// <summary>
        /// Returns true if this touch ID should be ignored for camera input.
        /// </summary>
        public bool IsTouchBlocked(int touchId)
        {
            return _blockedTouchIds.Contains(touchId);
        }

        /// <summary>
        /// Attempts to get the first active touch that isn't blocked by the touchzone.
        /// </summary>
        public bool TryGetFirstUnblockedTouch(out Touch touch)
        {
            foreach (var t in Touch.activeTouches)
            {
                if (!_blockedTouchIds.Contains(t.touchId))
                {
                    touch = t;
                    return true;
                }
            }

            touch = default;
            return false;
        }

        private bool IsTouchInsideZone(Vector2 screenPosition)
        {
            if (_touchZone == null)
            {
                return false;
            }

            // Convert screen position to local position in the RectTransform
            Camera cam = null;
            if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                cam = _canvas.worldCamera;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(_touchZone, screenPosition, cam);
        }
    }
}
