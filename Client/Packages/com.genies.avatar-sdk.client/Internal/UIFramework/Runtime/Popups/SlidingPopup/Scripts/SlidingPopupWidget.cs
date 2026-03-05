using System;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

namespace Genies.UIFramework
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class SlidingPopupWidget : MonoBehaviour
#else
    public class SlidingPopupWidget : MonoBehaviour
#endif
    {
        public enum AnimationStyle
        {
            Alpha = 0,
            SlideUp = 1,
            SlideDown = 2
        }

        [FormerlySerializedAs("blocker")]
        [Header("References")]
        [SerializeField]
        protected PopupBlocker _blocker;

        [FormerlySerializedAs("popup")] [SerializeField]
        protected GameObject _popup;

        [FormerlySerializedAs("cancelButton")] [SerializeField]
        protected Button _cancelButton;

        //[SerializeField] private CanvasGroup contentGroup; // for alpha animation

        [FormerlySerializedAs("animationStyle")]
        [Header("Animation Settings")]
        [SerializeField]
        protected AnimationStyle _animationStyle = AnimationStyle.SlideUp;

        [FormerlySerializedAs("showAnimTime")] [SerializeField]
        protected float _showAnimTime = 0.2f;

        [FormerlySerializedAs("hideAnimTime")] [SerializeField]
        protected float _hideAnimTime = 0.2f;

        [FormerlySerializedAs("showCurve")] [SerializeField]
        protected AnimationCurve _showCurve;

        [FormerlySerializedAs("hideCurve")] [SerializeField]
        protected AnimationCurve _hideCurve;

        [FormerlySerializedAs("showOnEnable")]
        [Header("Behavior")]
        [SerializeField]
        protected bool _showOnEnable;

        // TODO: make hide position depend on animation style
        [FormerlySerializedAs("hidePosition")] [SerializeField]
        protected Vector2 _hidePosition;

        [FormerlySerializedAs("useShowVector")] [SerializeField]
        protected bool _useShowVector;

        [FormerlySerializedAs("showPosition")] [SerializeField]
        protected Vector2 _showPosition;

        protected Vector2 _displayPosition;
        private RectTransform _popupRectTransform;

        public RectTransform PopupRectTransform
        {
            get
            {
                if (_popupRectTransform == null)
                {
                    _popupRectTransform = _popup.GetComponent<RectTransform>();
                }

                return _popupRectTransform;
            }
        }

        public event Action CloseClicked;

        protected virtual void Awake()
        {
            // Note: set popup displayed position in the editor or check the show vector option
            _displayPosition = _useShowVector ? _showPosition : PopupRectTransform.localPosition;

            if (_showOnEnable)
            {
                Show();
            }
            else
            {
                HideImmediately();
            }
        }

        protected virtual void OnEnable()
        {

            // Hide on cancel
            if (_cancelButton)
            {
                _cancelButton.onClick.RemoveAllListeners();
                _cancelButton.onClick.AddListener(OnCancelButtonClicked);
            }

            if (_blocker != null)
            {
                _blocker.OnBlockerClicked += OnCancelButtonClicked;
            }

            AddListeners();
        }

        protected virtual void OnDisable()
        {
            RemoveListeners();
        }

        protected void SetVisibility(bool visible)
        {
            if (_popup.activeSelf != visible)
            {
                _popup.SetActive(visible);
            }

            if (_blocker != null)
            {
                _blocker.gameObject.SetActive(visible);
            }
        }

        public virtual void Show()
        {
            PopupRectTransform.Terminate();
            if (_blocker != null)
            {
                _blocker.SetBlockerActive(true);
            }

            SetVisibility(true);

            // Reset position
            PopupRectTransform.localPosition = _hidePosition;

            // Smooth animation to display position - OutCubic for smooth deceleration
            var settings = AnimationSettings.Default
                .SetEasing(Ease.OutCubic)
                .SetUnscaledTime(true);

            _popupRectTransform
               .AnimateLocalMove(_displayPosition, _showAnimTime, settings);
        }

        private void OnCancelButtonClicked()
        {
            Hide();
            CloseClicked?.Invoke();
        }

        protected virtual void AddListeners()
        {

        }

        protected virtual void RemoveListeners()
        {
            if (_blocker != null)
            {
                _blocker.OnBlockerClicked -= OnCancelButtonClicked;
            }
        }

        public virtual void Hide()
        {
            PopupRectTransform.Terminate();
            if (_blocker != null)
            {
                _blocker.SetBlockerActive(false);
            }

            // Smooth animation to hide position - InCubic for quick acceleration
            var settings = AnimationSettings.Default
                .SetEasing(Ease.InCubic)
                .SetUnscaledTime(true)
                .SetOnComplete(() => SetVisibility(false));

            PopupRectTransform
               .AnimateLocalMove(_hidePosition, _hideAnimTime, settings);
        }

        protected void CheckIfWeShouldHidePopup(Vector2 direction)
        {
            if (direction.y < 0)
            {
                Hide();
            }
        }

        public virtual async UniTask HideWithDurationAsync(Action onComplete = null)
        {
            PopupRectTransform.Terminate();
            if (_blocker != null)
            {
                _blocker.SetBlockerActive(false);
            }

            // Smooth animation to hide position - InCubic for quick acceleration
            var settings = AnimationSettings.Default
                .SetEasing(Ease.InCubic)
                .SetUnscaledTime(true)
                .SetOnComplete(() =>
                {
                    SetVisibility(false);
                    onComplete?.Invoke();
                });

            await PopupRectTransform.AnimateLocalMove(_hidePosition, _hideAnimTime, settings);
        }


        public virtual void HideImmediately()
        {
            if (_blocker != null)
            {
                _blocker.SetBlockerActive(false);
            }

            PopupRectTransform.localPosition = _hidePosition;
            SetVisibility(false);
        }
    }
}
