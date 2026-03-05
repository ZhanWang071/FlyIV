using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Sdk.Samples.MultipleAvatars
{
    /// <summary>
    /// Manages UI GameObjects visibility based on Avatar Editor state.
    /// Disables specified GameObjects when Avatar Editor opens and re-enables them when it closes.
    /// </summary>
    public class AvatarEditorUIManager : MonoBehaviour
    {
        [Header("GameObjects to Hide During Avatar Editing")] [field: SerializeField]
        private List<GameObject> _gameObjectsToHide = new();

        private void Awake()
        {
            // Subscribe to Avatar Editor events
            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
        }

        private async void Start()
        {
            try
            {
                await UniTask.WaitUntil(() => AvatarSdk.IsLoggedIn);

                // Check if Avatar Editor is already open on start
                if (AvatarSdk.IsAvatarEditorOpen)
                {
                    OnAvatarEditorOpened();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in AvatarEditorUIManager.Start: {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
        }

        /// <summary>
        /// Called when the Avatar Editor is opened. Disables the specified GameObjects.
        /// </summary>
        private void OnAvatarEditorOpened()
        {
            // Disable the GameObjects
            SetSelectedGameObjectsVisible(false);
        }

        /// <summary>
        /// Called when the Avatar Editor is closed. Re-enables the specified GameObjects.
        /// </summary>
        private void OnAvatarEditorClosed()
        {
            // Restore the GameObjects to their initial states
            SetSelectedGameObjectsVisible(true);
        }

        private void SetSelectedGameObjectsVisible(bool isVisible)
        {
            foreach (var go in _gameObjectsToHide)
            {
                go.SetActive(isVisible);
            }
        }
    }
}
