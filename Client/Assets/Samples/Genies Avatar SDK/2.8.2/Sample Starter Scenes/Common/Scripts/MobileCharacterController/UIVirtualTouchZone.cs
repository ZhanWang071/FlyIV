using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace Genies.Sdk.Samples.Common
{
    public class UIVirtualTouchZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [System.Serializable]
        public class Event : UnityEvent<Vector2>
        {
        }

        [Header("Rect References")]
        [SerializeField] private RectTransform _containerRect;
        [SerializeField] private RectTransform _handleRect;
        [SerializeField] private RectTransform _handleBackgroundRect;

        [Header("Settings")]
        [SerializeField] private bool _doAutohide = true;
        [SerializeField] private bool _doContainHandle = false;
        [SerializeField] private bool _clampToMagnitude;
        [SerializeField] private float _magnitudeMultiplier = 1f;
        [SerializeField] private bool _invertXOutputValue;
        [SerializeField] private bool _invertYOutputValue;

        [Header("Output")]
        [SerializeField] private Event _touchZoneOutputEvent;
        [SerializeField] private UnityEvent _onPointerUpEvent;

        private Vector2 _pointerDownPosition;
        private Vector2 _currentPointerPosition;

        private void OnEnable()
        {
            SetupHandle();
        }

        private void SetupHandle()
        {
            if (_handleRect != null && _doAutohide)
            {
                SetObjectActiveState(_handleRect.gameObject, false);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position,
                eventData.pressEventCamera, out _pointerDownPosition);

            if (_handleRect != null)
            {
                if (_doAutohide)
                {
                    SetObjectActiveState(_handleRect.gameObject, true);
                }

                UpdateHandleRectPosition(_pointerDownPosition);
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_containerRect, eventData.position,
                eventData.pressEventCamera, out _currentPointerPosition);

            Vector2 positionDelta = GetDeltaBetweenPositions(_pointerDownPosition, _currentPointerPosition);

            Vector2 clampedPosition = ClampValuesToMagnitude(positionDelta);

            Vector2 outputPosition = ApplyInversionFilter(clampedPosition);

            OutputPointerEventValue(outputPosition * _magnitudeMultiplier);

            if (_handleRect != null)
            {
                UpdateHandleRectPosition(_currentPointerPosition);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _onPointerUpEvent?.Invoke();
            _pointerDownPosition = Vector2.zero;
            _currentPointerPosition = Vector2.zero;

            OutputPointerEventValue(Vector2.zero);

            if (_handleRect != null)
            {
                if (_doAutohide)
                {
                    SetObjectActiveState(_handleRect.gameObject, false);
                }

                UpdateHandleRectPosition(Vector2.zero);
            }
        }

        private void OutputPointerEventValue(Vector2 pointerPosition)
        {
            _touchZoneOutputEvent.Invoke(pointerPosition);
        }

        private void UpdateHandleRectPosition(Vector2 newPosition)
        {
            if (_doContainHandle)
            {
                newPosition = Vector2.ClampMagnitude(newPosition, _handleBackgroundRect.rect.width / 2);
            }

            _handleRect.anchoredPosition = newPosition;
        }

        private void SetObjectActiveState(GameObject targetObject, bool newState)
        {
            targetObject.SetActive(newState);
        }

        private Vector2 GetDeltaBetweenPositions(Vector2 firstPosition, Vector2 secondPosition)
        {
            return secondPosition - firstPosition;
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

    }
}
