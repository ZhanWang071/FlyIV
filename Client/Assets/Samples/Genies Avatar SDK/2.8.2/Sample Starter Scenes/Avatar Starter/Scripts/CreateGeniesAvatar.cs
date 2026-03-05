using System;
using Cysharp.Threading.Tasks;
using Genies.Sdk.Samples.Common;
using UnityEngine;

namespace Genies.Sdk.Samples.AvatarStarter
{
    /// <summary>
    /// A Unity Monobehavior that can be dropped onto any gameobject to create the User Avatar at Start.
    /// Will load a default avatar if you are not logged in.
    /// </summary>
    public sealed class CreateGeniesAvatar : MonoBehaviour
    {
        // inspector
        [SerializeField]
        private RuntimeAnimatorController animatorController;
        public bool autoSpawn = true;

        public bool DidSpawn { get; private set; }

        private GeniesAvatarController _avatarController;

        private async void Start()
        {
            try
            {
                _avatarController = this.GetComponent<GeniesAvatarController>();
                if (autoSpawn)
                {
                    await SpawnAvatarAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error in CreateGeniesAvatar.Start: {ex.Message}\n{ex.StackTrace}", this);
            }
        }

        public async UniTask<ManagedAvatar> SpawnAvatarAsync()
        {
            try
            {
                var avatar = await AvatarSdk.LoadUserAvatarAsync("User Avatar", this.transform, animatorController);
                
                if (avatar == null)
                {
                    Debug.LogError("Failed to spawn avatar: LoadUserAvatarAsync returned null", this);
                    DidSpawn = false;
                    return null;
                }

                if (avatar.Root == null)
                {
                    Debug.LogError("Spawned avatar has null Root component", this);
                    DidSpawn = false;
                    return null;
                }

                DidSpawn = true;
                var animatorEventBridge = avatar.Root.gameObject.AddComponent<GeniesAnimatorEventBridge>();
                
                if (_avatarController != null)
                {
                    _avatarController.SetAnimatorEventBridge(animatorEventBridge);
                }
                return avatar;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error spawning avatar: {ex.Message}\n{ex.StackTrace}", this);
                DidSpawn = false;
                return null;
            }
        }
    }
}
