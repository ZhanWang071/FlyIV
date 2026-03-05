using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Genies.Sdk.Samples.Common
{
    public class UIVirtualButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [System.Serializable]
        public class BoolEvent : UnityEvent<bool>
        {
        }

        [System.Serializable]
        public class Event : UnityEvent
        {
        }

        [Header("Output")]
        [SerializeField] private BoolEvent _buttonStateOutputEvent;
        [SerializeField] private Event _buttonClickOutputEvent;

        [field: SerializeField] public bool IsToggleButton { get; private set; } = false;
        [SerializeField] private Button _button;
        [SerializeField] private Color _highlight;
        private Color _currentTexture;
        [SerializeField] private Color _defaultTexture;
        private bool _isToggledOn = false;

        private void OnEnable()
        {
            _currentTexture = _defaultTexture;
            _isToggledOn = false;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OutputButtonStateValue(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            OutputButtonStateValue(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            OutputButtonClickEvent();
        }

        private void OutputButtonStateValue(bool buttonState)
        {
            _buttonStateOutputEvent.Invoke(buttonState);
        }

        private void OutputButtonClickEvent()
        {
            SetToggledOn(!_isToggledOn);
           
            _buttonClickOutputEvent.Invoke();
        }

        public void SetToggledOn(bool isOn)
        {
            if (IsToggleButton)
            {
                _isToggledOn = isOn;
                if (_isToggledOn)
                {
                    _currentTexture = _highlight;
                }
                else
                {
                    _currentTexture = _defaultTexture;
                }
                _button.image.color = _currentTexture;
            }
        }

    }
}
