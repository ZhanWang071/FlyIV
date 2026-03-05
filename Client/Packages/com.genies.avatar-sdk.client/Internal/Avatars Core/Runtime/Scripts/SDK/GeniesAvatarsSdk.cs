using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars.Behaviors;
using Genies.Avatars.Services;
using Genies.CrashReporting;
using Genies.Login.Native;
using Genies.Naf;
using Genies.Naf.Content;
using Genies.Refs;
using Genies.ServiceManagement;
using Genies.Services.Configs;
using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Avatars.Sdk
{
    /// <summary>
    /// Static convenience facade for creating, loading, and manipulating Genies Avatars and controllers.
    /// - Auto-initializes required services on first use
    /// - Provides symmetrical wrappers for assets, tattoos, colors, body attributes, and definition import/export
    /// - Adds batch/optimized operations that minimize rebuilds
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class GeniesAvatarsSdk
#else
    public static class GeniesAvatarsSdk
#endif
    {
        private static bool IsInitialized =>
            InitializationCompletionSource is not null
            && InitializationCompletionSource.Task.Status == UniTaskStatus.Succeeded;
        private static UniTaskCompletionSource InitializationCompletionSource { get; set; }

        // Default controller prefab path in Resources.
        private const string DefaultControllerResource = "GeniesAvatarRig";

        /// <summary>
        /// Fired when we finish initializing the AvatarsSDK. Returns a float which is how long we took to
        /// initialize in milliseconds
        /// </summary>
        internal static event Action<float> GeniesAvatarsSdkInitialized;

        /// <summary>
        /// Fired when we finish loading an avatar. Returns a bool, signifying if the avatar was a default avatar
        /// and a float that tells us how long it took to load the avatar
        /// </summary>
        internal static event Action<bool, float> LoadedAvatar;

        #region Initialization / Service Access

        /// <summary>
        /// Ensures SDK dependencies are initialized.
        /// This method calls <see cref="ServiceManager.InitializeAppAsync"/> internally with the required installers.
        /// If the consuming application requires specific initialization order or custom installers,
        /// it should call <see cref="ServiceManager.InitializeAppAsync"/> before using any Avatars.Sdk API.
        /// See <see cref="GeniesAvatarSdkInstaller"/> for all required installer dependencies.
        /// </summary>
        public static async UniTask<bool> InitializeAsync()
        {
            if (IsInitialized)
            {
                return true;
            }

#if GENIES_DEV
            return await InitializeAsync(BackendEnvironment.Dev);
#else
            return await InitializeAsync(GeniesApiConfigManager.TargetEnvironment);
#endif
        }

        public static async UniTask<bool> InitializeDemoModeAsync()
        {
#if GENIES_DEV
            return await InitializeDemoModeAsync(BackendEnvironment.Dev);
#else
            return await InitializeDemoModeAsync(GeniesApiConfigManager.TargetEnvironment);
#endif
        }

        /// <summary>
        /// Ensures SDK dependencies are initialized with the specified target environment.
        /// This method calls <see cref="ServiceManager.InitializeAppAsync"/> internally with the required installers.
        /// If the consuming application requires specific initialization order or custom installers,
        /// it should call <see cref="ServiceManager.InitializeAppAsync"/> before using any Avatars.Sdk API.
        /// See <see cref="GeniesAvatarSdkInstaller"/> for all required installer dependencies.
        /// </summary>
        /// <param name="targetEnvironment">The backend environment to target for initialization</param>
        /// <returns>True if initialization was successful, false otherwise</returns>
        public static async UniTask<bool> InitializeAsync(BackendEnvironment targetEnvironment)
        {
            if (InitializationCompletionSource is not null)
            {
                await InitializationCompletionSource.Task;
                return IsInitialized;
            }

            InitializationCompletionSource = new UniTaskCompletionSource();

            if (ServiceManager.IsAppInitialized is false)
            {
                try
                {
                    var apiConfig = new GeniesApiConfig
                    {
                        TargetEnv = targetEnvironment,
                    };

                    var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    await ServiceManager.InitializeAppAsync(
                        customInstallers: new GeniesInstallersSetup(apiConfig)
                            .ConstructInstallersList(),
                        disableAutoResolve: true);

                    GeniesAvatarsSdkInitialized?.Invoke(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timeStamp);
                    InitializationCompletionSource.TrySetResult();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to initialize ServiceManager: {ex.Message}");
                    ServiceManager.Dispose();

                    InitializationCompletionSource = null;
                    return false;
                }
            }

            InitializationCompletionSource.TrySetResult();
            return true;
        }

        public static async UniTask<bool> InitializeDemoModeAsync(BackendEnvironment targetEnvironment)
        {
            if (InitializationCompletionSource is not null)
            {
                await InitializationCompletionSource.Task;
                return IsInitialized;
            }

            InitializationCompletionSource = new UniTaskCompletionSource();

            if (ServiceManager.IsAppInitialized is false)
            {
                try
                {
                    var apiConfig = new GeniesApiConfig
                    {
                        TargetEnv = targetEnvironment,
                    };

                    var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();


                    await ServiceManager.InitializeAppAsync(
                        customInstallers: new GeniesDemoModeInstallersSetup(apiConfig)
                            .ConstructInstallersList(),
                        disableAutoResolve: true);

                    GeniesAvatarsSdkInitialized?.Invoke(DateTimeOffset.UtcNow.ToUnixTimeSeconds() - timeStamp);
                    InitializationCompletionSource.TrySetResult();
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to initialize ServiceManager: {ex.Message}");
                    ServiceManager.Dispose();

                    InitializationCompletionSource = null;
                    return false;
                }
            }

            InitializationCompletionSource.TrySetResult();
            return true;
        }

        /// <summary>
        /// Checks that the GeniesAvatarService exists and automatically initializes one if not present.
        /// </summary>
        internal static async UniTask<IGeniesAvatarSdkService> GetOrCreateAvatarSdkInstance()
        {
            if (await InitializeAsync() is false)
            {
                return default;
            }
            return AvatarSdkServiceProvider.Instance;
        }

        #endregion

        #region Data Loading

        public static async UniTask<List<Genies.Services.Model.Avatar>> LoadAvatarsDataByUserIdAsync(string userId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentNullException(nameof(userId));
                }

                var genieAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                return await genieAvatarSdkService.LoadAvatarsDataByUserIdAsync(userId);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to load avatars data for user {userId}: {ex.Message}");
                return default;
            }
        }

        #endregion

        #region Legacy Runtime Avatar Loaders (GameObjects)

        /// <summary>
        /// Loads and creates a Genie using the currently logged-in user's data.
        /// This method fetches the latest user avatar definition from the server on each call.
        ///
        /// OPTIMIZATION: For better performance of subsequent loads, consider caching the avatar definition
        /// and loading it with <see cref="LoadAvatarControllerByJsonDefinition"/>.
        /// </summary>
        public static async UniTask<GeniesAvatar> LoadUserAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null,
            bool waitUntilUserIsLoggedIn = false)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (waitUntilUserIsLoggedIn)
                {
                    await GeniesLoginSdk.WaitUntilLoggedInAsync();
                }

                var result = await LoadUserAvatarController(parent);

                if (playerAnimationController != null)
                {
                    result.Animator.runtimeAnimatorController = playerAnimationController;
                }

                result.Root.gameObject.name = avatarName;

                return result;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load user avatar: {ex.Message}");
                return default;
            }
        }

        /// <summary>Loads and creates a default instance of an avatar.</summary>
        public static async UniTask<GeniesAvatar> LoadDefaultAvatarAsync(
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var geniesAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                var genie = await geniesAvatarSdkService.LoadDefaultRuntimeAvatarAsync(
                    avatarName,
                    parent,
                    playerAnimationController
                );

                LoadedAvatar?.Invoke(true, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeStamp);

                return new GeniesAvatar(genie);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load default avatar: {ex.Message}");
                return default;
            }
        }

        /// <summary>Loads and creates an avatar using a string JSON definition.</summary>
        public static async UniTask<GeniesAvatar> LoadAvatarFromJsonAsync(
            string json,
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (string.IsNullOrEmpty(json))
                {
                    throw new ArgumentNullException(nameof(json));
                }

                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var geniesAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                var genie = await geniesAvatarSdkService.LoadRuntimeAvatarAsync(
                    json,
                    avatarName,
                    parent,
                    playerAnimationController
                );

                var returnAvatar = new GeniesAvatar(genie);
                LoadedAvatar?.Invoke(IsDefinitionDefault(returnAvatar), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeStamp);

                return returnAvatar;
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to load avatar from JSON: {ex.Message}");
                return default;
            }
        }

        /// <summary>Loads and creates an avatar using an explicit AvatarDefinition.</summary>
        public static async UniTask<GeniesAvatar> LoadAvatarFromDefinitionAsync(
            Naf.AvatarDefinition avatarDefinition,
            string avatarName = null,
            Transform parent = null,
            RuntimeAnimatorController playerAnimationController = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (avatarDefinition == null)
                {
                    throw new ArgumentNullException(nameof(avatarDefinition));
                }

                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var geniesAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                var genie = await geniesAvatarSdkService.LoadRuntimeAvatarAsync(
                    avatarDefinition,
                    avatarName,
                    parent,
                    playerAnimationController
                );

                var returnAvatar = new GeniesAvatar(genie);
                LoadedAvatar?.Invoke(IsDefinitionDefault(returnAvatar), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeStamp);

                return returnAvatar;
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to load avatar from definition: {ex.Message}");
                return default;
            }
        }

        #endregion

        #region Controller Loaders

        /// <summary>
        /// Creates a controller for the current user's avatar.
        /// This method fetches the latest user avatar definition from the server on each call.
        ///
        /// OPTIMIZATION: For better performance of subsequent loads, consider caching the avatar definition
        /// and loading it with <see cref="LoadAvatarControllerByJsonDefinition"/>.
        /// </summary>
        public static async UniTask<GeniesAvatar> LoadUserAvatarController(Transform root = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                using var _ = new ProcessSpan(ProcessIds.LoadUserAvatarController);

                var myDefinition = await GetUserAvatarDefinition();

                return await LoadAvatarControllerWithClassDefinition(
                    JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(myDefinition), root);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load user avatar controller: {ex.Message}");
                return default;
            }
        }

        /// <summary>Creates a controller for a specific user's avatar by userId.</summary>
        public static async UniTask<GeniesAvatar> LoadAvatarControllerById(
            string userId,
            Transform root = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var defString = await GetUserAvatarDefinition(userId);
                return await LoadAvatarControllerWithClassDefinition(
                    JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(defString), root);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar controller by ID {userId}: {ex.Message}");
                return default;
            }
        }

        public static async UniTask<GeniesAvatar> LoadAvatarControllerByJsonDefinition(
            string jsonDef,
            Transform root = null)
        {
            return await LoadAvatarControllerWithClassDefinition(
                JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(jsonDef), root);
        }

        public static async UniTask<GeniesAvatar> LoadTestAvatarAsync(
            Transform root = null)
        {
            var avatarDefinitionAsset = Resources.Load<TextAsset>("Genies/SampleAvatarDefinitions/sample_def");
            if (avatarDefinitionAsset == null || string.IsNullOrWhiteSpace(avatarDefinitionAsset.text))
            {
                Debug.LogWarning("Failed to load test avatar. Loading default instead");
                return await LoadDefaultAvatarAsync();
            }

            return await LoadAvatarControllerWithClassDefinition(
                JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(avatarDefinitionAsset.text), root);
        }


        /// <summary>
        /// Gets the authenticated user's avatar definition as a JSON string.
        /// This fetches the latest user avatar definition from the server.
        /// The returned definition can be cached and used with <see cref="LoadAvatarControllerByJsonDefinition"/> for optimized loading.
        /// User must be logged in.
        /// </summary>
        /// <returns>The avatar definition JSON string, or null if retrieval failed.</returns>
        public static async UniTask<string> GetUserAvatarDefinition()
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var geniesAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                return await geniesAvatarSdkService.GetMyAvatarDefinition();
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to get user avatar definition: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Gets a specific user's avatar definition as a JSON string by user ID.
        /// This fetches the latest avatar definition for the specified user from the server.
        /// The returned definition can be cached and used with <see cref="LoadAvatarControllerByJsonDefinition"/> for optimized loading.
        /// The local client must be authenticated with Genies services (i.e. user is logged in).
        /// </summary>
        /// <param name="userId">The user ID whose avatar definition to retrieve.</param>
        /// <returns>The avatar definition JSON string, or null if retrieval failed.</returns>
        public static async UniTask<string> GetUserAvatarDefinition(string userId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (string.IsNullOrWhiteSpace(userId))
                {
                    throw new ArgumentNullException(nameof(userId));
                }

                var geniesAvatarSdkService = await GetOrCreateAvatarSdkInstance();
                return await geniesAvatarSdkService.LoadAvatarDefStringByUserId(userId);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to get avatar definition for user {userId}: {ex.Message}");
                return default;
            }
        }

        public static async UniTask<GeniesAvatar> LoadAvatarControllerWithClassDefinition(
            Genies.Services.Model.Avatar avatar,
            Transform root = null)
        {
            return await LoadAvatarControllerWithClassDefinition(
                JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(avatar.Definition), root);
        }

        public static async UniTask<GeniesAvatar> LoadAvatarControllerWithClassDefinition(
            Naf.AvatarDefinition avatar,
            Transform root = null)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var timeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (avatar != null)
                {
                    // First resolve pipeline information for all asset ids (including tattoos)
                    IAssetIdConverter converter = ServiceManager.Get<IAssetIdConverter>();
                    var assetsToResolve = new List<string>(avatar.equippedAssetIds);
                    assetsToResolve.AddRange(avatar.equippedTattooIds.Values);
                    await converter.ResolveAssetsAsync(assetsToResolve);
                }

                // Then convert IDs before passing json
                var convertedJson = await ConvertAndRemoveInvalidIds(avatar);

                IAssetParamsService paramsService = ServiceManager.GetService<IAssetParamsService>(null);
                NativeUnifiedGenieController controller = await AvatarControllerFactory.CreateSimpleNafGenie(convertedJson, root, paramsService);

                if (controller != null)
                {
                    LoadedAvatar?.Invoke(IsDefinitionDefault(controller), DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - timeStamp);
                }

                // Process any pending avatar definition updates after all conversions are complete
                if (avatar != null)
                {
                    IAssetIdConverter converter = ServiceManager.Get<IAssetIdConverter>();
                    await converter.ProcessPendingAvatarUpdatesAsync();
                }

                return new GeniesAvatar(controller.Genie, controller);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to load avatar controller with class definition: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// Saves the avatar definition to the cloud for the logged in user.
        /// User must be logged in.
        /// </summary>
        /// <returns>A true if saved successfully.</returns>
        public static async UniTask<bool> SaveUserAvatarWithDefinitionAsync(string definition)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                await AvatarCreatorUtil.CreateOrUpdateAvatar(definition, saveOnCreate: true);
                return true;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to save avatar definition to the user. {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Precache Methods

        /// <summary>
        /// Pre-caches assets required for the current user's avatar without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// </summary>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheUserAvatarAssetsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var myDefinition = await GetUserAvatarDefinition();

                var definition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(myDefinition);

                if (cancellationToken.IsCancellationRequested || definition == null)
                {
                    return false;
                }

                return await PrecacheAvatarAssetsByDefinitionAsync(definition, cancellationToken: cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to precache user avatar assets: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pre-caches assets required for a specific user's avatar by user ID without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// </summary>
        /// <param name="userId">The user ID whose avatar assets to precache.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheAvatarAssetsByIdAsync(string userId)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                var defString = await GetUserAvatarDefinition(userId);
                var definition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(defString);

                return await PrecacheAvatarAssetsByDefinitionAsync(definition);
            }
            catch (Exception ex)
            {
                CrashReporter.LogError($"Failed to precache avatar assets for user {userId}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pre-caches assets required for an avatar based on a JSON definition without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// </summary>
        /// <param name="jsonDefinition">The avatar definition JSON string.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheAvatarAssetsByDefinitionAsync(string jsonDefinition)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (string.IsNullOrWhiteSpace(jsonDefinition))
                {
                    throw new ArgumentNullException(nameof(jsonDefinition));
                }

                var definition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(jsonDefinition);
                return await PrecacheAvatarAssetsByDefinitionAsync(definition);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to precache avatar assets from JSON definition: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Pre-caches assets required for an avatar based on an AvatarDefinition without loading it.
        /// This downloads and caches all assets needed for the avatar, improving subsequent load times.
        /// Uses the same asset loading mechanism as NativeUnifiedGenieController.SetDefinitionAsync.
        /// </summary>
        /// <param name="definition">The avatar definition.</param>
        /// <param name="resolverConfig">Optional asset resolver configuration. If null, uses NafAssetResolverConfig.Default.</param>
        /// <param name="cancellationToken">Cancellation token to cancel the operation if triggered.</param>
        /// <returns>True if pre-caching was successful, false otherwise.</returns>
        public static async UniTask<bool> PrecacheAvatarAssetsByDefinitionAsync(
            Naf.AvatarDefinition definition,
            Naf.NafAssetResolverConfig resolverConfig = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (await InitializeAsync() is false)
                {
                    throw new InvalidOperationException("Failed to initialize GeniesAvatarsSdk");
                }

                if (definition == null)
                {
                    throw new ArgumentNullException(nameof(definition));
                }

                // First resolve pipeline information for all asset ids (including tattoos)
                IAssetIdConverter converter = ServiceManager.Get<IAssetIdConverter>();
                var assetsToResolve = new List<string>(definition.equippedAssetIds ?? new List<string>());
                if (definition.equippedTattooIds != null)
                {
                    assetsToResolve.AddRange(definition.equippedTattooIds.Values);
                }
                await converter.ResolveAssetsAsync(assetsToResolve);

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                // Then convert IDs before processing
                var convertedJson = await ConvertAndRemoveInvalidIds(definition);
                var convertedDefinition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(convertedJson);

                // Use shared asset loader utility
                IAssetParamsService paramsService = ServiceManager.GetService<IAssetParamsService>(null);
                using var loader = new AvatarDefinitionAssetLoader(paramsService, CreateRef.FromDisposable(new ContainerService(resolverConfig)));
                await loader.PreloadAssetsAsync(convertedDefinition, cancellationToken);

                if (cancellationToken.IsCancellationRequested)
                {
                    return false;
                }

                // Process any pending avatar definition updates after all conversions are complete
                await converter.ProcessPendingAvatarUpdatesAsync();

                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex) when (!(ex is ArgumentNullException))
            {
                CrashReporter.LogError($"Failed to precache avatar assets from definition: {ex.Message}");
                return false;
            }
        }

        #endregion

        #region Helpers

        private static bool IsDefinitionDefault(NativeUnifiedGenieController controller)
        {
            controller.GetBodyVariation();
            var defaultDefinitionForGender = NafAvatarExtensions.GetDefaultDefinitionForGender(controller.GetBodyVariation()).SerializeDefinition();
            return controller.GetDefinition() == defaultDefinitionForGender;
        }

        private static bool IsDefinitionDefault(GeniesAvatar avatar)
        {
            var defaultDefinitionForGender = NafAvatarExtensions.GetDefaultDefinitionForGender(avatar.GetBodyVariation()).SerializeDefinition();
            return avatar.GetDefinition() == defaultDefinitionForGender;
        }

        // TEMPORARY METHODS...
        private static async UniTask<string> ConvertAndRemoveInvalidIds(Genies.Naf.AvatarDefinition definition)
        {
            // return JsonConvert.SerializeObject(definition);
            if (definition == null)
            {
                return null;
            }

            IAssetIdConverter converter = ServiceManager.GetService<IAssetIdConverter>(null);

            List<string> assetIds = definition.equippedAssetIds;

            if (assetIds == null)
            {
                return JsonConvert.SerializeObject(definition);
            }

            // Convert assets to Universal IDs, tracking the avatar definition for later update if old IDs are found
            var newIdMappings = await converter.ConvertToUniversalIdsAsync(assetIds, definition);
            if (newIdMappings.Count == assetIds.Count)
            {
                definition.equippedAssetIds = newIdMappings.Values.ToList();
            }
            else
            {
                CrashReporter.LogError("Failed to convert asset ids in avatar definition");
            }

            return JsonConvert.SerializeObject(definition);

        }

        /// <summary>
        /// Instantiates the default controller prefab and optionally parents it under <paramref name="parent"/>.
        /// Returns the instantiated GeniesAvatarController.
        /// </summary>
        public static GeniesAvatarController InstantiateDefaultController(GeniesAvatar avatar)
        {
            var prefab = Resources.Load<GeniesAvatarController>(DefaultControllerResource);
            if (prefab == null)
            {
                Debug.LogError($"Controller prefab not found.");
                return null;
            }

            // If the avatar was parented to something, make sure we keep a reference to it so we can preserve the avatars location relative to its parent
            var currentAvatarParent = avatar.Root.gameObject.transform.parent;

            // Instantiate our loaded prefab with whatever the avatar parent was
            var instance = GameObject.Instantiate(prefab);
            instance.transform.position = currentAvatarParent.position;

            // Now, set the parent of the avatar to be the loaded controller instance
            avatar.Root.gameObject.transform.SetParent(instance.transform);

            // Since we set the new parent to the same local position as the avatar, we can just set this to 0.
            avatar.Root.gameObject.transform.localPosition = Vector3.zero;

            // Setup animation bridge.
            var animatorEventBridge = avatar.Root.gameObject.AddComponent<GeniesAnimatorEventBridge>();
            instance.SetAnimatorEventBridge(animatorEventBridge);

            return instance;
        }

        #endregion
    }
}
