using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Genies.Sdk.Samples.Common
{
    public class UIVirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [System.Serializable]
        public class Event : UnityEvent<Vector2>
        {
        }

        [Header("Rect References")]
        [SerializeField] private RectTransform _containerRect;
        [SerializeField] private RectTransform _handleRect;

        [Header("Settings")]
        [SerializeField] private float _joystickRange = 50f;
        [SerializeField] private float _magnitudeMultiplier = 1f;
        [SerializeField] private bool _invertXOutputValue;
        [SerializeField] private bool _invertYOutputValue;
        
        // Constant for position normalization calculation
        private const float PositionNormalizationFactor = 2.5f;

        [Header("Output")]
        [SerializeField] private Event _joystickOutputEvent;

        private Vector2 _inputVector = Vector2.zero;

        private void Start()
        {
            SetupHandle();
        }

        private void SetupHandle()
        {
            if (_handleRect)
            {
                UpdateHandleRectPosition(Vector2.zero);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position,
                eventData.pressEventCamera, out Vector2 position);

            position = ApplySizeDelta(position);

            Vector2 clampedPosition = ClampValuesToMagnitude(position);

            Vector2 outputPosition = ApplyInversionFilter(position);

            _inputVector = outputPosition * _magnitudeMultiplier;
            OutputPointerEventValue(outputPosition * _magnitudeMultiplier);

            if (_handleRect)
            {
                UpdateHandleRectPosition(clampedPosition * _joystickRange);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OutputPointerEventValue(Vector2.zero);
            _inputVector = Vector2.zero;
            if (_handleRect)
            {
                UpdateHandleRectPosition(Vector2.zero);
            }
        }

        private void OutputPointerEventValue(Vector2 pointerPosition)
        {
            _joystickOutputEvent.Invoke(pointerPosition);
        }

        private void UpdateHandleRectPosition(Vector2 newPosition)
        {
            _handleRect.anchoredPosition = newPosition;
        }

        private Vector2 ApplySizeDelta(Vector2 position)
        {
            // Normalize position relative to container size with scaling factor
            float x = (position.x / _containerRect.sizeDelta.x) * PositionNormalizationFactor;
            float y = (position.y / _containerRect.sizeDelta.y) * PositionNormalizationFactor;
            return new Vector2(x, y);
        }

        private Vector2 ClampValuesToMagnitude(Vector2 position)
        {
            return Vector2.ClampMagnitude(position, 1);
        }

        private Vector2 ApplyInversionFilter(Vector2 position)
        {
            if (_invertXOutputValue)
            {
                position.x = InvertValue(position.x);
            }

            if (_invertYOutputValue)
            {
                position.y = InvertValue(position.y);
            }

            return position;
        }

        private float InvertValue(float value)
        {
            return -value;
        }

        public void ResetJoystick()
        {
            _inputVector = Vector2.zero;
            OutputPointerEventValue(Vector2.zero);
            _handleRect.anchoredPosition = Vector2.zero;
        }

        private void OnEnable()
        {
            ResetJoystick();
        }

    }
}