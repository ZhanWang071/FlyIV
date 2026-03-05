using System;
using Cysharp.Threading.Tasks;
using Genies.Sdk.Samples.Common;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Genies.Sdk.Samples.AvatarStarter
{
    public class LoadMyAvatar : MonoBehaviour
    {
        
        public bool IsAvatarLoaded { get; private set; }
        
        [Header("Input Speeds")]
        public float FreeLookXAxisSpeed = 5f;
        public float FreeLookYAxisSpeed = 1f;
        public InputSystemUIInputModule InputSystemUIInputModule;

        public ManagedAvatar LoadedAvatar;

        [Header("Scene References")]
        public GeniesAvatarController loadedController;
        public RuntimeAnimatorController OptionalController;
        public CinemachineCamera CinemachineFreeLookSettings;
        public CinemachineInputAxisController InputAxisController;

        [SerializeField] private GeniesLoginUI geniesLoginUI;
        [SerializeField] private GeniesInputs geniesInputs;

        private void Awake()
        {
            // Avatar controller will eat inputs... dont enable until we're done logging in.
            if (loadedController != null)
            {
                loadedController.enabled = false;
            }

            if (InputAxisController != null && InputAxisController.Controllers != null)
            {
                if (InputAxisController.Controllers.Count > 0 && InputAxisController.Controllers[0] != null)
                {
                    InputAxisController.Controllers[0].Input.Gain = FreeLookXAxisSpeed;
                }
                if (InputAxisController.Controllers.Count > 1 && InputAxisController.Controllers[1] != null)
                {
                    InputAxisController.Controllers[1].Input.Gain = FreeLookYAxisSpeed;
                }
            }

            if (InputSystemUIInputModule != null)
            {
                InputSystemUIInputModule.enabled = true;
            }

            if (geniesLoginUI == null)
            {
                geniesLoginUI = FindAnyObjectByType<GeniesLoginUI>();
            }
            if (geniesLoginUI != null)
            {
                geniesLoginUI.AvatarEditorButtonPressed += OpenAvatarEditor;
            }

            if (geniesInputs == null)
            {
                geniesInputs = FindAnyObjectByType<GeniesInputs>();
            }
        }

        private void OpenAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                Debug.LogWarning("The Avatar Editor is already open.");
                return;
            }

            if (LoadedAvatar != null)
            {
                AvatarSdk.OpenAvatarEditorAsync(LoadedAvatar).Forget();
            }
            else
            {
                Debug.LogWarning("The Avatar Editor is already open.");
                
            }
        }

        private void Start()
        {
            if (!AvatarSdk.IsLoggedIn)
            {
                AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
                AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;
                AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
                AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;

                return;
            }
        }

        private void OnUserLoggedOut()
        {
            DestroyLoadedAvatar();
            geniesLoginUI.ResetUI();
        }

        private void OnUserLoggedIn()
        {
            LoadAvatarAsync().Forget();
        }
        private void OnAvatarEditorClosed()
        {
            if (geniesLoginUI != null)
            {
                geniesLoginUI.ResetUI();
            }
            if (geniesInputs == null)
            {
                geniesInputs = FindAnyObjectByType<GeniesInputs>();
            }
            if (geniesInputs != null)
            {
                geniesInputs.enabled = true;
            }
        }

        private void OnAvatarEditorOpened()
        {
            if (geniesLoginUI != null)
            {
                geniesLoginUI.ShowTitleBar(false);
            }
            if (geniesInputs == null)
            {
                geniesInputs = FindAnyObjectByType<GeniesInputs>();
            }

            if (geniesInputs != null)
            {
                geniesInputs.enabled = false;
            }
        }
        

        private async UniTask LoadAvatarAsync()
        {
            try
            {
                if (loadedController != null)
                {
                    // Parenting the loaded avatar to an inactive GO and then immediately activating it will crash the application.
                    // Activate the parent object first.
                    loadedController.enabled = true;
                }

                LoadedAvatar = await AvatarSdk.LoadUserAvatarAsync(
                    parent: loadedController.transform,
                    playerAnimationController: OptionalController != null ? OptionalController : null);

                if (LoadedAvatar == null)
                {
                    Debug.LogError("Failed to load avatar: LoadUserAvatarAsync returned null", this);
                    ShowTitleBar(false);

                    return;
                }

                if (LoadedAvatar.Root == null)
                {
                    Debug.LogError("Loaded avatar has null Root component", this);
                    ShowTitleBar(false);

                    return;
                }
                
                var animatorEventBridge = LoadedAvatar.Root.gameObject.AddComponent<GeniesAnimatorEventBridge>();
                ShowTitleBar(true);
                
                IsAvatarLoaded = true;
                AvatarLoadedNotifier.InvokeLoaded(LoadedAvatar);

                if (loadedController != null)
                {
                    loadedController.SetAnimatorEventBridge(animatorEventBridge);
                    loadedController.GenieSpawned = true;

                    if (CinemachineFreeLookSettings != null && CinemachineFreeLookSettings.gameObject != null)
                    {
                        CinemachineFreeLookSettings.gameObject.SetActive(true);
                        if (loadedController.CinemachineCameraTarget != null)
                        {
                            CinemachineFreeLookSettings.Follow = loadedController.CinemachineCameraTarget.transform;
                            CinemachineFreeLookSettings.LookAt = loadedController.CinemachineCameraTarget.transform;
                        }
                        else
                        {
                            Debug.LogWarning("CinemachineCameraTarget is null on loadedController", this);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading avatar: {ex.Message}\n{ex.StackTrace}", this);
                ShowTitleBar(false);
            }
        }

        private void DestroyLoadedAvatar()
        {
            if (loadedController != null)
            {
                loadedController.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                loadedController.transform.localScale = Vector3.one;
            }
            if (LoadedAvatar != null)
            {
                
                LoadedAvatar.Dispose();
                AvatarLoadedNotifier.InvokeDestroyed(LoadedAvatar);
            }
        }

        private void ShowTitleBar(bool isVisible)
        {
            if (geniesLoginUI != null)
            {
                geniesLoginUI.ShowAvatarEditorButton(isVisible);
            }
        }


        private void OnDestroy()
        {
            // Unsubscribe from events to prevent memory leaks
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
            DestroyLoadedAvatar();
        }
    }
}
