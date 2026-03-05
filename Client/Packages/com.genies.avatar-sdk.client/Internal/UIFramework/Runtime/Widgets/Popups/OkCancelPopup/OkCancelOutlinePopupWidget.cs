using Genies.UIFramework;
using UnityEngine;
using UnityEngine.Events;

namespace Genies.UI.Widgets {
#if GENIES_SDK && !GENIES_INTERNAL
    internal class OkCancelOutlinePopupWidget : PopupWidget {
#else
    public class OkCancelOutlinePopupWidget : PopupWidget {
#endif
        public UnityEvent OnOkButtonClicked = new UnityEvent();
        public UnityEvent OnCancelButtonClicked = new UnityEvent();
        [SerializeField] private OutlineButton OkButton;
        [SerializeField] private OutlineButton CancelButton;

        public override void OnEnable() {
            base.OnEnable();
            OkButton.OnClick.AddListener(Ok);
            CancelButton?.OnClick.AddListener(Cancel);
        }

        public void OnDisable() {
            OkButton.OnClick.RemoveListener(Ok);
            CancelButton?.OnClick.RemoveListener(Cancel);
        }

        private async void Ok() {
            await HideAsync();
            OnOkButtonClicked.Invoke();
        }

        private async void Cancel() {
            await HideAsync();
            OnCancelButtonClicked.Invoke();
        }
    }
}
