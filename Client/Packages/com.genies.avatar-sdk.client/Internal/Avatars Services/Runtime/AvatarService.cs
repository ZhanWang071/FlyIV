using System;
using System.Net.Http;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Events;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Utilities;
using UnityEngine;
using Avatar = Genies.Services.Model.Avatar;
using Newtonsoft.Json;
namespace Genies.Avatars.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAvatarService"/> that provides avatar management functionality through remote APIs.
    /// This service handles avatar creation, retrieval, updates, and synchronization with backend systems.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarService : IAvatarService
#else
    public class AvatarService : IAvatarService
#endif
    {
        private readonly IAvatarApi _avatarClient;
        private readonly AvatarServiceApiPathResolver _avatarServiceApiPathResolver = new();
        private UniTaskCompletionSource _apiInitializationSource;
        private readonly JsonSerializerSettings _serializerSettings = new() { Formatting = Formatting.Indented, };
        private UniTaskCompletionSource<(bool, Genies.Naf.AvatarDefinition)> _avatarLoadingTaskCompletionSource;
        private readonly IDefinitionConverter _definitionManager = new NafAvatarDefinitionConverter();
        private Genies.Naf.AvatarDefinition LoadedDefinition { get; set; }
        private Avatar _loadedAvatar;

        /// <summary>
        /// Gets or sets the currently loaded avatar, automatically handling definition deserialization and error recovery.
        /// </summary>
        public Avatar LoadedAvatar
        {
            get => _loadedAvatar;
            private set
            {
                _loadedAvatar = value;
                if (_loadedAvatar == null || string.IsNullOrEmpty(_loadedAvatar.Definition))
                {
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                    return;
                }

                try
                {
                    LoadedDefinition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(_loadedAvatar.Definition);
                }
                catch (JsonSerializationException jse)
                {
                    CrashReporter.Log($"Error deserializing Avatar Definition: {jse.Message}. Generate default.",
                        LogSeverity.Error);
                    CrashReporter.LogHandledException(jse);
                    // probably a legacy definition, load default
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
                catch (AggregateException ae)
                {
                    CrashReporter.LogHandledException(ae);
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
                catch (Exception exception)
                {
                    CrashReporter.LogHandledException(exception);
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
            }
        }

        private EventBus EventBus => EventBusSingleton.EventBus;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class with default configuration.
        /// Creates an avatar API client using the configured target environment.
        /// </summary>
        public AvatarService()
        {
            var avatarConfig = new Configuration()
            {
                BasePath = _avatarServiceApiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment),
            };
            _avatarClient = new AvatarApi(avatarConfig);

            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class with a custom avatar API client.
        /// This constructor is primarily used for testing with mock implementations.
        /// </summary>
        /// <param name="avatarApi">The avatar API client to use for backend communication.</param>
        public AvatarService(IAvatarApi avatarApi)
        {
            _avatarClient = avatarApi;
            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Waits for the avatar service to be fully initialized and ready for API calls.
        /// This includes authentication setup and API client configuration.
        /// </summary>
        /// <returns>A task that completes when the service is initialized.</returns>
        public virtual UniTask WaitUntilInitializedAsync()
        {
            if (_apiInitializationSource == null)
            {
                return UniTask.CompletedTask;
            }

            return _apiInitializationSource.Task;
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task;
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();
            //Await auth access token being set.
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);
            await UniTask.WaitUntil(()=>_avatarClient != null);
            await UniTask.WaitUntil(()=>_avatarClient.Configuration != null);

            _avatarClient.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            // When the token refreshes, use it.
            GeniesLoginSdk.OnTokenRefresh += (token) =>
            {
                _avatarClient.Configuration.AccessToken = token;
            };

            _apiInitializationSource.TrySetResult();
            _apiInitializationSource = null;
        }

        // Note the user has to be signed in order to create an Avatar
        public async UniTask<bool> CreateAvatarAsync(string gender)
        {
            var userId = await GeniesLoginSdk.GetUserIdAsync();
            var status = Avatar.StatusEnum.Visible;
            var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(gender);
            var definition = AvatarExtensions.GetDefaultDefinitionForGender(gender);
            var definitionJson = JsonConvert.SerializeObject(definition, _serializerSettings);
            try
            {
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender, definition: definitionJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Avatar failed: {e.Message}");
            }

            return (LoadedAvatar != null && !string.IsNullOrEmpty(LoadedAvatar.Definition));
        }

        // Note the user has to be signed in in order to create an Avatar
        public async UniTask<bool> CreateAvatarAsync(Naf.AvatarDefinition avatarDefinition)
        {
            var userId = await GeniesLoginSdk.GetUserIdAsync();
            var status = Avatar.StatusEnum.Visible;

            var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(avatarDefinition.BinaryGenderStringFromDna());

            try
            {
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender,
                    definition: JsonConvert.SerializeObject(avatarDefinition, _serializerSettings));
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Avatar failed: {e.Message}");
            }

            return (LoadedAvatar != null && !string.IsNullOrEmpty(LoadedAvatar.Definition));
        }

        private async UniTask<Avatar> CreateAvatar(
            string userId,
            Avatar.StatusEnum? status,
            Avatar.GenderEnum? gender,
            string definition)
        {
            var avatar = new Avatar(userId, status, gender, definition: definition);

            try
            {
                avatar = await _avatarClient.CreateAvatarAsync(avatar);
            }
            catch (ApiException e)
            {
                avatar = null;
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                avatar = null;
                CrashReporter.LogHandledException(e);
            }

            if (avatar != null)
            {
                EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
            }

            return avatar;
        }

        /// <summary>
        /// Retrieves the primary avatar definition for the logged-in user.
        /// This method internally calls <see cref="GetUserAvatarDefinitionOrDefaultAsync"/> to handle fetching or creating a default avatar.
        /// </summary>
        /// <returns>A UniTask that resolves to the user's avatar definition, or a default one if none exists.</returns>
        public async UniTask<Genies.Naf.AvatarDefinition> GetAvatarDefinitionAsync()
        {
            return (await GetUserAvatarDefinitionOrDefaultAsync()).AvatarDefinition;
        }

        /// <summary>
        /// Retrieves the primary avatar definition for the logged-in user.
        /// If no avatar exists, a default avatar definition is returned.
        /// </summary>
        /// <returns>A UniTask<(bool IsSynced, Genies.Naf.AvatarDefinition AvatarDefinition)> indicating
        /// whether the avatar definition was synced from the backend and the avatar definition itself.
        /// </returns>
        public async UniTask<(bool IsSynced, Genies.Naf.AvatarDefinition AvatarDefinition)> GetUserAvatarDefinitionOrDefaultAsync()
        {
            (bool IsSynced, Genies.Naf.AvatarDefinition AvatarDefinition) result = default;
            
            await WaitUntilInitializedAsync();

            if (GeniesLoginSdk.IsUserSignedInAnonymously())
            {
                // Anon users cannot store avatars, therefore, we cannot retrieve them
                LoadedAvatar = null;
                return (true, null);
            }
            
            string userId = await GeniesLoginSdk.GetUserIdAsync();

            LoadedDefinition = null;

            if (_avatarLoadingTaskCompletionSource != null)
            {
                return await _avatarLoadingTaskCompletionSource.Task;
            }

            _avatarLoadingTaskCompletionSource = new UniTaskCompletionSource<(bool, Genies.Naf.AvatarDefinition)>();
            Avatar avatar = null;

            try
            {
                var avatars = await _avatarClient.GetAvatarsAsync(userId);
                if (avatars.Count > 0)
                {
                    result.IsSynced = true;

                    // This will ensure you get the latest avatar
                    avatar = avatars[0];
                    EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
                }
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
                avatar = null;
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
                avatar = null;
            }

            LoadedAvatar = avatar; // LoadedDefinition is set here.
            result.AvatarDefinition = LoadedDefinition;

            _avatarLoadingTaskCompletionSource.TrySetResult(result);
            _avatarLoadingTaskCompletionSource = null;

            return result;
        }

        public async UniTask<Naf.AvatarDefinition> GetUserAvatarDefinitionAsync(string userId)
        {
            await WaitUntilInitializedAsync();

            Avatar avatar = null;
            Naf.AvatarDefinition definition = null;
            try
            {
                var avatars = await _avatarClient.GetAvatarsAsync(userId);
                if (avatars.Count > 0)
                {
                    // This will ensure you get the latest avatar
                    avatar = avatars[0];
                }

                if (avatar == null || string.IsNullOrEmpty(avatar.Definition))
                {
                    return null;
                }

                definition = await NafAvatarExtensions.DeserializeToAvatarDefinitionAsync(avatar.Definition);
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
            return definition;
        }

        public async UniTask<List<Avatar>> GetUserAvatarsAsync(string userId)
        {
            List<Avatar> avatars = new();
            try
            {
                avatars = await _avatarClient.GetAvatarsAsync(userId);
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
            return avatars;
        }

        public async UniTask UpdateAvatarAsync(Genies.Naf.AvatarDefinition avatarDefinition)
        {
            // TODO - Find a better way to fix first time authv2 token issues
            // When a user signs up and logs in for the first time using email OTP
            // the token they get doesnt allow the user to create/update an avatar.
            // These commands will still succeed as far as the client is concerned.
            // Refreshing the token here fixes the issue.
            await GeniesLoginSdk.RefreshTokensAsync();

            if (LoadedAvatar == null)
            {
                var userId = await GeniesLoginSdk.GetUserIdAsync();
                var gender = NafAvatarExtensions.BinaryGenderStringFromDna(avatarDefinition);
                var status = Avatar.StatusEnum.Visible;
                var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(gender);
                var definitionJson = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender, definition: definitionJson);
                LoadedDefinition = avatarDefinition;
                LoadedAvatar.Definition = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
            }
            else
            {
                LoadedDefinition = avatarDefinition;
                LoadedAvatar.Definition = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
            }

            await UpdateAvatarAsync(LoadedAvatar);
        }

        public async UniTask UpdateAvatarAsync(Avatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            string userId = await GeniesLoginSdk.GetUserIdAsync();
            EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
            try
            {
                await _avatarClient.UpdateAvatarAsync(avatar, avatar.AvatarId, userId);
                LoadedAvatar = avatar;
            }
            catch (ApiException e)
            {
                Debug.LogError("UpdateAvatarAsync: " + e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public async UniTask<string> UploadAvatarImageAsync(byte[] imageData, string avatarId)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            }

            if (string.IsNullOrEmpty(avatarId))
            {
                throw new ArgumentException("Avatar ID cannot be null or empty", nameof(avatarId));
            }

            try
            {
                var result = _avatarClient.GetAvatarImageUploadUrl(avatarId, contentType: "image/png");

                if (result == null)
                {
                    // We were unable to retrieve a url... this is normal with an account that does not have write permissions
                    // such as an anonymous account
                    return "";
                }

                using (var httpClient = new HttpClient())
                using (var request = new HttpRequestMessage(HttpMethod.Put, result.PresignedUrl))
                {
                    // Content
                    var content = new ByteArrayContent(imageData);
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                    // Add headers
                    request.Content = content;

                    // Execute
                    HttpResponseMessage response = await httpClient.SendAsync(request);

                    if (response.IsSuccessStatusCode == false)
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        throw new Exception($"Failed to upload image: {error}");
                    }

                    return result.ImageUrl;
                }
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to upload avatar image: {e.Message}");
                throw;
            }
        }

        public async UniTask<Naf.AvatarDefinition> GetOrCreateAvatarAsync(string bodyType)
        {
            if (LoadedAvatar != null && LoadedDefinition != null)
            {
                return LoadedDefinition;
            }

            // the avatar doesn't exists, so we need to create a new avatar
            await CreateAvatarAsync(bodyType);

            if (LoadedAvatar == null)
            {
                Debug.Log("Error creating Avatar");
            }

            if (LoadedAvatar != null)
            {
                EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, LoadedDefinition);
            }

            return LoadedDefinition;
        }


    }
}
