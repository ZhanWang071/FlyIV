
using System;
using Unity.Properties;
using UnityEngine;

namespace Genies.Sdk.Samples.Common
{
    public class UICanvasControllerInput : MonoBehaviour
    {
        
        [SerializeField]
        private UIVirtualButton _toggleButton = null;
        
        private CanvasGroup _canvasGroup;
       public GeniesInputs GeniesInputs { get; private set; }

       private void Awake()
       {
           _canvasGroup = GetComponent<CanvasGroup>();
           if (_canvasGroup == null)
           {
               _canvasGroup = gameObject.AddComponent<CanvasGroup>();
           }
           SetVisible(false);
       }

       private void Start()
        {
            // Cache FindAnyObjectByType result in Start instead of OnEnable for better performance
            if (GeniesInputs == null)
            {
                GeniesInputs = FindAnyObjectByType<GeniesInputs>();
            }

            if (GeniesInputs == null)
            {
                Debug.LogWarning("GeniesInputs not found! Disabling UICanvasControllerInput.", this);
                gameObject.SetActive(false);
                return;
            }
            if(GeniesInputs.EnableTouchControls)
            {
                SubscribeToNotifiers();
            }
        }

        private void SubscribeToNotifiers()
        {
            AvatarLoadedNotifier.Loaded += OnAvatarLoaded;
            AvatarLoadedNotifier.Destroyed += OnAvatarDestroyed;
            GeniesInputs.SprintToggled += _toggleButton.SetToggledOn;
        }

        private void UnsubscribeFromNotifiers()
        {
            AvatarLoadedNotifier.Loaded -= OnAvatarLoaded;
            AvatarLoadedNotifier.Destroyed -= OnAvatarDestroyed;
            GeniesInputs.SprintToggled -= _toggleButton.SetToggledOn;
        }

        private void OnAvatarDestroyed(ManagedAvatar obj)
        {
            SetVisible(false);
        }

        private void OnEnable()
        {
            // Validate cached reference is still valid
            if (GeniesInputs == null)
            {
                GeniesInputs = FindAnyObjectByType<GeniesInputs>();
            }

            if (GeniesInputs == null)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnAvatarLoaded(ManagedAvatar managedAvatar)
        {
            SetVisible(true);
        }

        private void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.interactable = visible;
                _canvasGroup.blocksRaycasts = visible;
                _canvasGroup.alpha = visible ? 1 : 0;
            }
        }

        private void OnDestroy()
        {
           UnsubscribeFromNotifiers();
        }

        public void VirtualMoveInput(Vector2 virtualMoveDirection)
        {
            if (GeniesInputs != null)
            {
                GeniesInputs.MoveInput(virtualMoveDirection);
            }
        }

        public void VirtualLookInput(Vector2 virtualLookDirection)
        {
            if (GeniesInputs != null)
            {
                GeniesInputs.LookInput(virtualLookDirection);
            }
        }

        public void VirtualJumpInput(bool virtualJumpState)
        {
            if (GeniesInputs != null)
            {
                GeniesInputs.JumpInput(virtualJumpState);
            }
        }

        public void VirtualSprintInput(bool virtualSprintState)
        {
            if (GeniesInputs != null)
            {
                GeniesInputs.SprintInput(virtualSprintState);
            }
        }
    }
}
