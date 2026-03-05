using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Sdk;
using Genies.Naf;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Sdk
{
    internal sealed partial class CoreSdk
    {
        public class Avatar
        {
            private CoreSdk Parent { get; }

            private Avatar() { }

            internal Avatar(CoreSdk parent)
            {
                Parent = parent;
            }

            /// <summary>
            /// Loads a default avatar with optional configuration.
            /// </summary>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadDefaultAvatarAsync(
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdLoadDefaultAvatar);
                var geniesAvatar = await GeniesAvatarsSdk.LoadAvatarControllerWithClassDefinition(new AvatarDefinition(), parent);
                return InstantiateAndConfigure(geniesAvatar, avatarName, playerAnimationController);
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
            public async UniTask<ManagedAvatar> LoadUserAvatarAsync(
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("User is not logged in. Loading default avatar instead.");
                    return await LoadDefaultAvatarAsync(avatarName, parent, playerAnimationController);
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdLoadUserAvatar);
                var geniesAvatar = await GeniesAvatarsSdk.LoadUserAvatarController(parent);
                return InstantiateAndConfigure(geniesAvatar, avatarName, playerAnimationController);
            }
            
            /// <summary>
            /// Saves the avatar definition to the cloud for the logged in user.
            /// User must be logged in.
            /// </summary>
            /// <returns>A true if saved successfully.</returns>
            public async UniTask<bool> SaveUserAvatarWithDefinitionAsync(string avatarDefinition)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return false;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Can only save the avatar definition a logged-in user.");
                    return false;
                }

                return await GeniesAvatarsSdk.SaveUserAvatarWithDefinitionAsync(avatarDefinition);
            }

            /// <summary>
            /// Loads an avatar based on a provided JSON definition.
            /// </summary>
            /// <param name="definition">The avatar definition JSON string</param>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadAvatarByDefinitionAsync(
                string definition,
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdLoadAvatarByDefinition);
                var geniesAvatar = await GeniesAvatarsSdk.LoadAvatarControllerByJsonDefinition(definition, parent);

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Must be logged in to load an avatar by definition.");
                    return null;
                }

                return InstantiateAndConfigure(
                    geniesAvatar,
                    avatarName,
                    playerAnimationController
                );
            }

            /// <summary>
            /// Loads an avatar based on a provided UserId.
            /// </summary>
            /// <param name="userId">The userId of the avatar to spawn</param>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadUserAvatarByUserIdAsync(string userId, string avatarName, Transform parent, RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }
                
                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Must be logged in to load an avatar by userId.");
                    return null;
                }
                
                var geniesAvatar = await GeniesAvatarsSdk.LoadAvatarControllerById(userId, parent);
                return InstantiateAndConfigure(geniesAvatar, avatarName, playerAnimationController);
            }
			
            /// <summary>
            /// Loads a test avatar
            /// </summary>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="parent">Optional parent transform for the avatar.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A ManagedAvatar instance, or null if loading failed.</returns>
            public async UniTask<ManagedAvatar> LoadTestAvatarAsync(
                string avatarName,
                Transform parent,
                RuntimeAnimatorController playerAnimationController)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                var geniesAvatar = await GeniesAvatarsSdk.LoadTestAvatarAsync(parent);

                return InstantiateAndConfigure(
                    geniesAvatar,
                    avatarName,
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
            public async UniTask<string> GetUserAvatarDefinition()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Must be logged in to get user avatar definition.");
                    return null;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdGetUserAvatarDefinition);
                return await GeniesAvatarsSdk.GetUserAvatarDefinition();
            }

            /// <summary>
            /// Gets a specific user's avatar definition as a JSON string by user ID.
            /// This fetches the latest avatar definition for the specified user from the server.
            /// The returned definition can be cached and used with <see cref="LoadAvatarByDefinitionAsync"/> for optimized loading.
            /// The local client must be authenticated with Genies services (i.e. user is logged in).
            /// </summary>
            /// <param name="userId">The user ID whose avatar definition to retrieve.</param>
            /// <returns>The avatar definition JSON string, or null if retrieval failed.</returns>
            public async UniTask<string> GetUserAvatarDefinition(string userId)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Must be logged in to get user avatar definition.");
                    return null;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdGetUserAvatarDefinition);
                return await GeniesAvatarsSdk.GetUserAvatarDefinition(userId);
            }

            /// Instantiates and configures a ManagedAvatar from a GeniesAvatar instance.
            /// </summary>
            /// <param name="geniesAvatar">The GeniesAvatar instance to wrap.</param>
            /// <param name="avatarName">Optional name for the avatar GameObject.</param>
            /// <param name="playerAnimationController">Optional animation controller to apply to the avatar.</param>
            /// <returns>A configured ManagedAvatar instance, or null if the input GeniesAvatar is null.</returns>
            private ManagedAvatar InstantiateAndConfigure(GeniesAvatar geniesAvatar, string avatarName, RuntimeAnimatorController playerAnimationController)
            {
                if (geniesAvatar is null) { return null; }

                var avatarInstance = new ManagedAvatar(geniesAvatar);

                if (string.IsNullOrWhiteSpace(avatarName) is false)
                {
                    avatarInstance.Root.name = avatarName;
                }

                if (playerAnimationController != null)
                {
                    avatarInstance.SetAnimatorController(playerAnimationController);
                }

                return avatarInstance;
            }

            /// <summary>
            /// Pre-caches assets required for the authenticated user's avatar without loading it.
            /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
            /// </summary>
            /// <returns>True if pre-caching was successful, false otherwise.</returns>
            public async UniTask<bool> PrecacheUserAvatarAssetsAsync(CancellationToken cancellationToken = default)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return false;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("User is not logged in. Cannot precache user avatar assets.");
                    return false;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdPrecacheUserAvatarAssets);
                return await GeniesAvatarsSdk.PrecacheUserAvatarAssetsAsync(cancellationToken);
            }

            /// <summary>
            /// Pre-caches assets required for a default avatar without loading it.
            /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
            /// </summary>
            /// <returns>True if pre-caching was successful, false otherwise.</returns>
            public async UniTask<bool> PrecacheDefaultAvatarAssetsAsync(CancellationToken cancellationToken = default)
            {
                if (await Parent.InitializeAsync()is false)
                {
                    return false;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdPrecacheDefaultAvatarAssets);
                return await GeniesAvatarsSdk.PrecacheAvatarAssetsByDefinitionAsync(new AvatarDefinition(), cancellationToken: cancellationToken);
            }

            /// <summary>
            /// Pre-caches assets required for an avatar based on a JSON definition without loading it.
            /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
            /// </summary>
            /// <param name="definition">The avatar definition JSON string.</param>
            /// <returns>True if pre-caching was successful, false otherwise.</returns>
            public async UniTask<bool> PrecacheAvatarAssetsByDefinitionAsync(string definition)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return false;
                }

                if (Parent.LoginApi.IsLoggedIn is false)
                {
                    Debug.LogWarning("Must be logged in to precache an avatar by definition.");
                    return false;
                }

                using var processSpan = new ProcessSpan(ProcessIds.ProcessIdPrecacheAvatarAssetsByDefinition);
                return await GeniesAvatarsSdk.PrecacheAvatarAssetsByDefinitionAsync(definition);
            }
        }
    }
}
