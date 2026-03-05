using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Genies.UIFramework
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class InputProvider : MonoBehaviour
#else
    public class InputProvider : MonoBehaviour
#endif
    {
        private InputControls _inputControls;

        private void Awake()
        {
            this.RegisterSelf();

            _inputControls = new InputControls();
        }

        private void OnEnable()
        {
            _inputControls.Enable();
        }

        private void OnDisable()
        {
            _inputControls.Disable();
        }

        public Vector2 ScreenPosition()
        {
            return _inputControls.Touch.ScreenPosition.ReadValue<Vector2>();
        }

        public InputAction TapAndHold()
        {
            return _inputControls.Touch.TapAndHold;
        }

        public bool CheckIfTapIsInRectTransform(RectTransform rectTransform)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, ScreenPosition(), null,
                out Vector2 localPoint);
            return rectTransform.rect.Contains(localPoint);
        }
    }
}