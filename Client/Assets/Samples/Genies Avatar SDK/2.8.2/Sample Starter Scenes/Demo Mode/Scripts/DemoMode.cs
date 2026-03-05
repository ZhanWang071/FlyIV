using System;
using Cysharp.Threading.Tasks;
using Genies.Sdk.Samples.Common;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Genies.Sdk.Samples.DemoMode
{
    public sealed class DemoMode : MonoBehaviour
    {
        [Header("Input Speeds")]
        [SerializeField] private float _freeLookXAxisSpeed = 5f;
        [SerializeField] private float _freeLookYAxisSpeed = 1f;
        [SerializeField] private InputSystemUIInputModule _inputModule;

        [Header("Scene References")]
        [SerializeField] private GeniesAvatarController _avatarController;
        [SerializeField] private RuntimeAnimatorController _optionalController;
        [SerializeField] private CinemachineCamera _freeLookCamera;
        [SerializeField] private CinemachineInputAxisController _inputAxisController;

        private ManagedAvatar _loadedAvatar;

        private void Awake()
        {
            // Avatar controller will eat inputs... don't enable until we're done loading.
            if (_avatarController != null)
            {
                _avatarController.enabled = false;
            }

            ConfigureInputAxisGains();

            if (_inputModule != null)
            {
                _inputModule.enabled = true;
            }
        }

        private void ConfigureInputAxisGains()
        {
            if (_inputAxisController == null || _inputAxisController.Controllers == null)
            {
                return;
            }

            var controllers = _inputAxisController.Controllers;

            if (controllers.Count > 0 && controllers[0] != null)
            {
                controllers[0].Input.Gain = _freeLookXAxisSpeed;
            }

            if (controllers.Count > 1 && controllers[1] != null)
            {
                controllers[1].Input.Gain = _freeLookYAxisSpeed;
            }
        }

        private async void Start()
        {
            await AvatarSdk.InitializeDemoModeAsync();
            await LoadAvatarAsync();
        }

        private async UniTask LoadAvatarAsync()
        {
            try
            {
                if (_avatarController != null)
                {
                    // Parenting the loaded avatar to an inactive GO and then immediately activating it can crash.
                    // Enable the controller first.
                    _avatarController.enabled = true;
                }

                _loadedAvatar = await AvatarSdk.LoadTestAvatarAsync(
                    "name",
                    parent: _avatarController != null ? _avatarController.transform : null,
                    playerAnimationController: _optionalController);

                if (_loadedAvatar == null)
                {
                    Debug.LogError("Failed to load avatar: LoadTestAvatarAsync returned null", this);
                    return;
                }

                var root = _loadedAvatar.Root;
                if (root == null)
                {
                    Debug.LogError("Loaded avatar has null Root component", this);
                    return;
                }

                var animatorEventBridge = root.gameObject.AddComponent<GeniesAnimatorEventBridge>();

                AvatarLoadedNotifier.InvokeLoaded(_loadedAvatar);

                if (_avatarController == null)
                {
                    return;
                }

                _avatarController.SetAnimatorEventBridge(animatorEventBridge);
                _avatarController.GenieSpawned = true;

                ConfigureCameraFollow();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error loading avatar: {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        private void ConfigureCameraFollow()
        {
            if (_freeLookCamera == null || _freeLookCamera.gameObject == null)
            {
                return;
            }

            _freeLookCamera.gameObject.SetActive(true);

            if (_avatarController == null)
            {
                return;
            }

            var cameraTarget = _avatarController.CinemachineCameraTarget;
            if (cameraTarget == null)
            {
                Debug.LogWarning("CinemachineCameraTarget is null on avatarController", this);
                return;
            }

            _freeLookCamera.Follow = cameraTarget.transform;
            _freeLookCamera.LookAt = cameraTarget.transform;
        }

        private void DestroyLoadedAvatar()
        {
            if (_avatarController != null)
            {
                _avatarController.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                _avatarController.transform.localScale = Vector3.one;
            }

            if (_loadedAvatar == null)
            {
                return;
            }

            _loadedAvatar.Dispose();
            AvatarLoadedNotifier.InvokeDestroyed(_loadedAvatar);
            _loadedAvatar = null;
        }

        private void OnDestroy()
        {
            DestroyLoadedAvatar();
        }
    }
}
