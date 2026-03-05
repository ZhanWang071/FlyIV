using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.AvatarEditor.Core;
using Genies.Avatars.Sdk;
using UnityEngine;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Initializes the Genies Avatar SDK.
        /// Calling is optional as all operations will initialize the SDK if it is not already initialized.
        /// This method is safe to call multiple times - subsequent calls return the cached initialization result.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public static async UniTask<bool> InitializeAsync()
        { 
            return await Instance.InitializeInternalAsync();
        }

        internal static async UniTask<bool> InitializeDemoModeAsync()
        {
            return await Instance.InitializeDemoModeInternalAsync();
        }

        /// <summary>
        /// Loads a default avatar with optional configuration.
        /// </summary>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadDefaultAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();

            if (IsLoggedIn is false)
            {
                throw new NotImplementedException("Spawning a default avatar while not logged in is not yet supported. Log in first.");
            }

            return await Instance.CoreSdk.AvatarApi.LoadDefaultAvatarAsync(avatarName, parent, playerAnimationController);
        }
        
        /// <summary>
        /// Loads the authenticated user's avatar with optional configuration.
        /// This method fetches the latest user avatar definition from the server on each call.
        /// Falls back to default avatar if user is not logged in.
        ///
        /// OPTIMIZATION: For better performance of subsequent loads, consider caching the avatar definition
        /// and loading it with <see cref="LoadAvatarByDefinitionAsync"/>.
        /// </summary>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadUserAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.LoadUserAvatarAsync(avatarName, parent, playerAnimationController);
        }
        
        /// <summary>
        /// Loads an avatar based on a provided UserId.
        /// </summary>
        /// <param name="userId">The avatar definition string.</param>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadUserAvatarByUserIdAsync(
            string userId,
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.LoadUserAvatarByUserIdAsync(userId, avatarName, parent, playerAnimationController);
        }
        

        /// <summary>
        /// Loads an avatar based on a provided JSON definition.
        /// </summary>
        /// <param name="definition">The avatar definition string.</param>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadAvatarByDefinitionAsync(
            string definition,
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.LoadAvatarByDefinitionAsync(
                definition,
                avatarName,
                parent,
                playerAnimationController
            );
        }
        
        /// <summary>
        /// Loads a test avatar
        /// </summary>
        /// <param name="avatarName">Optional name for the avatar GameObject.</param>
        /// <param name="parent">Optional parent transform for the avatar.</param>
        /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
        /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
        public static async UniTask<ManagedAvatar> LoadTestAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.LoadTestAvatarAsync(
                avatarName,
                parent,
                playerAnimationController
            );
        }
        
        /// <summary>
        /// Gets the authenticated user's avatar definition as a JSON string.
        /// This fetches the latest user avatar definition from the server.
        /// The returned definition can be cached and used with <see cref="LoadAvatarByDefinitionAsync"/> for optimized loading.
        /// User must be logged in.
        /// </summary>
        /// <returns>The avatar definition JSON string, or null if retrieval failed.</returns>
        public static async UniTask<string> GetUserAvatarDefinition()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.GetUserAvatarDefinition();
        }

        /// <summary>
        /// Gets a specific user's avatar definition as a JSON string by user ID.
        /// This fetches the latest avatar definition for the specified user from the server.
        /// The returned definition can be cached and used with <see cref="LoadAvatarByDefinitionAsync"/> for optimized loading.
        /// The local client must be authenticated with Genies services (i.e. user is logged in).
        /// </summary>
        /// <param name="userId">The user ID whose avatar definition to retrieve.</param>
        /// <returns>The avatar definition JSON string, or null if retrieval failed.</returns>
        public static async UniTask<string> GetUserAvatarDefinition(string userId)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.GetUserAvatarDefinition(userId);
        }

        /// <summary>
        /// Pre-caches assets required for the authenticated user's avatar without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// User must be logged in.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheUserAvatarAssetsAsync(CancellationToken cancellationToken = default)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.PrecacheUserAvatarAssetsAsync(cancellationToken);
        }

        /// <summary>
        /// Pre-caches assets required for a default avatar without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheDefaultAvatarAssetsAsync(CancellationToken cancellationToken = default)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.PrecacheDefaultAvatarAssetsAsync(cancellationToken);
        }

        /// <summary>
        /// Pre-caches assets required for an avatar based on a JSON definition without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// User must be logged in.
        /// </summary>
        /// <param name="definition">The avatar definition JSON string.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheAvatarAssetsByDefinitionAsync(string definition)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.AvatarApi.PrecacheAvatarAssetsByDefinitionAsync(definition);
        }

        private static readonly Lazy<AvatarSdk> _instance = new Lazy<AvatarSdk>(() => new AvatarSdk());
        private static AvatarSdk Instance => _instance.Value;

        private CoreSdk CoreSdk { get; }
        private AsyncLazy<bool> InitializationTask { get; }
        private bool DemoModeRequested { get; set; }

        private AvatarSdk()
        {
            CoreSdk = new CoreSdk();
            InitializationTask = new AsyncLazy<bool>(PerformInitializationAsync);
        }
        
        private UniTask<bool> InitializeDemoModeInternalAsync()
        {
            DemoModeRequested = true;
            return InitializationTask.Task;
        }

        private UniTask<bool> InitializeInternalAsync()
        {
            DemoModeRequested = false;
            return InitializationTask.Task;
        }

        private async UniTask<bool> PerformInitializationAsync()
        {
            try
            {
                if (DemoModeRequested)
                {
                    return await PerformDemoModeInitializationAsync();
                }

                var avatarEditorResult = await AvatarEditorSDK.InitializeAsync();
                var coreSdkResult = await CoreSdk.InitializeAsync();
                return avatarEditorResult && coreSdkResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize the Avatar SDK: {ex.Message}");
                return false;
            }
        }

        private async UniTask<bool> PerformDemoModeInitializationAsync()
        {
            try
            { 
                var coreSdkResult = await CoreSdk.InitializeDemoModeAsync();
                return coreSdkResult;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize the Avatar SDK: {ex.Message}");
                return false;
            }
        }
    }
}
