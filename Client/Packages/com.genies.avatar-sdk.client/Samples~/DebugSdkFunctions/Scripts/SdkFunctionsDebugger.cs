using System;
using System.Collections.Generic;
using Genies.Utilities;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Sdk.Samples.DebugSdkFunctions
{
    /// <summary>
    /// Add this component to a game object in the scene to test runtime functionality of <see cref="AvatarSdk"/>.
    /// Interfaces through the attached game object's Inspector.
    /// Intended for Editor-use only.
    /// </summary>
    internal class SdkFunctionsDebugger : MonoBehaviour
    {
        public enum LoginType
        {
            EmailOtp,
            Password
        }

        [Header("Login Options")]
        [Tooltip("Automatically login with cached credentials on start?")]
        [SerializeField] private bool _instantLoginOnStart = true;
        [Tooltip("The login type to use for authentication.")]
        [SerializeField] private LoginType _loginType = LoginType.EmailOtp;
        [Space]
        [Tooltip("The email address to prefill with the login flow.")]
        [SerializeField] private string _emailPrefill;

        [Header("Spawn Avatar Options")]
        [Tooltip("Optional: leave NULL if avatar should not be parented on spawn.")]
        [SerializeField] private Transform _avatarParent;
        [Tooltip("Optional: leave NULL if no custom animation controller should be applied.")]
        [SerializeField] private RuntimeAnimatorController _customAnimatorController;
        [Tooltip("Name for the default avatar.")]
        [SerializeField] private string _defaultAvatarName = "Default Genies Avatar";
        [Tooltip("Name for the user avatar.")]
        [SerializeField] private string _userAvatarName = "User Genies Avatar";
        [Tooltip("Avatar definition to load using 'Spawn Avatar Definition'")]
        [TextArea]
        [SerializeField] private string _avatarDefintiion = "";

        [Header("Avatar Debug Options")]
        [FormerlySerializedAs("_avatarToEdit")]
        [Tooltip("The subject avatar for the debug methods below. Auto-assigned to the avatar spawned by this debugger.")]
        [SerializeField] private ManagedAvatarComponent _avatarToDebug;

        [Space(20)]
        [Tooltip("The camera to use for the Avatar Editor. Auto-assigned to Camera.main if null.")]
        [SerializeField] private Camera _avatarEditorCamera;

        private LoginType CurrentLoginType
        {
            get => _loginType;
            set => _loginType = value;
        }

        private bool InstantLoginOnStart => _instantLoginOnStart;
        private string EmailPrefill => _emailPrefill;
        private Transform AvatarParent => _avatarParent;
        private RuntimeAnimatorController CustomAnimatorController => _customAnimatorController;
        private string DefaultAvatarName => _defaultAvatarName;
        private string UserAvatarName => _userAvatarName;
        private string AvatarDefinition => _avatarDefintiion;
        private Camera AvatarEditorCamera => _avatarEditorCamera;

        private ManagedAvatarComponent AvatarToDebug
        {
            get => _avatarToDebug;
            set => _avatarToDebug = value;
        }

        private LoginEmailOtp LoginEmailOtpInstance { get; set; }
        private LoginPassword LoginPasswordInstance { get; set; }
        private SpawnAvatar SpawnAvatarStateDisplay { get; set; }
        private AvatarEditor AvatarEditorStateDisplay { get; set; }
        private List<ManagedAvatar> SpawnedAvatars { get; set; } = new List<ManagedAvatar>();

        private void OnEnable()
        {
            if (_avatarEditorCamera == null)
            {
                _avatarEditorCamera = Camera.main;
            }
        }

        private void Awake()
        {
#if UNITY_EDITOR
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;

            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
            AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;

            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorOpened += OnAvatarEditorOpened;

            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
            AvatarSdk.Events.AvatarEditorClosed += OnAvatarEditorClosed;
#endif
        }

        private async void Start()
        {
#if UNITY_EDITOR
            await AvatarSdk.InitializeAsync();

            SpawnAvatarStateDisplay = new SpawnAvatar(gameObject);
            AvatarEditorStateDisplay = new AvatarEditor(gameObject);

            if (InstantLoginOnStart)
            {
                var instantLoginResult = await AvatarSdk.TryInstantLoginAsync();
                if (instantLoginResult.isLoggedIn)
                {
                    Debug.Log($"Automatically logged in as '{instantLoginResult.username}'");
                    // Let the AvatarSdk.Events.UserLoggedIn event response handle logged in state.
                    return;
                }
            }

            StartLoginFlow();
#endif
        }

        private void OnUserLoggedIn()
        {
            if (LoginEmailOtpInstance == null && LoginPasswordInstance == null)
            {
                StartLoginFlow();
            }
        }

        private void OnUserLoggedOut()
        {
            StartLoginFlow();
        }

        private void OnAvatarEditorOpened()
        {
            Debug.Log("Avatar Editor opened");
        }

        private void OnAvatarEditorClosed()
        {
            Debug.Log("Avatar Editor closed");
        }

        private void StartLoginFlow()
        {
            switch (CurrentLoginType)
            {
                case LoginType.EmailOtp:
                    RestartEmailLogin();
                    break;
                case LoginType.Password:
                    RestartPasswordLogin();
                    break;
            }
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;
            AvatarSdk.Events.AvatarEditorOpened -= OnAvatarEditorOpened;
            AvatarSdk.Events.AvatarEditorClosed -= OnAvatarEditorClosed;
#endif

            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            SpawnAvatarStateDisplay?.Dispose();
            SpawnAvatarStateDisplay = null;

            AvatarEditorStateDisplay?.Dispose();
            AvatarEditorStateDisplay = null;

            DestroyAllSpawnedAvatars();
        }

        [InspectorButton("===== Account Management =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAccountManagement() { }

        [InspectorButton("\n(Re)Start Email OTP Login\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void RestartEmailLogin()
        {
            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            CurrentLoginType = LoginType.EmailOtp;
            LoginEmailOtpInstance = new LoginEmailOtp(gameObject, EmailPrefill);
        }

        [InspectorButton("(Re)Start Password Login", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void RestartPasswordLogin()
        {
            LoginEmailOtpInstance?.Dispose();
            LoginEmailOtpInstance = null;

            LoginPasswordInstance?.Dispose();
            LoginPasswordInstance = null;

            CurrentLoginType = LoginType.Password;
            LoginPasswordInstance = new LoginPassword(gameObject, EmailPrefill);
        }

        [InspectorButton("===== User Info =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderLoginState() { }

        [InspectorButton("\nCheck User Info\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void CheckUserInfo()
        {
            var isLoggedIn = AvatarSdk.IsLoggedIn;
            var userId = await AvatarSdk.GetUserIdAsync();
            var username = await AvatarSdk.GetUserNameAsync();

            var message = $"Is Logged In: {(isLoggedIn ? "TRUE" : "FALSE")}\n" +
                          $"User ID: {userId}\n" +
                          $"Username: {username}";

            ShowPopUp("Login Status", message);
        }

        [InspectorButton("\nGet User Avatar Definition\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void GetUserAvatarDefinition()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp(title: "⚠️ Get User Avatar Definition", message: "Log in first!");
                return;
            }

            var avatarDefinition = await AvatarSdk.GetUserAvatarDefinition();

            if (string.IsNullOrWhiteSpace(avatarDefinition))
            {
                ShowPopUp(title: "⚠️ Get User Avatar Definition", message: "Failed to retrieve avatar definition");
                return;
            }

            ShowAvatarDefinitionPopup(avatarDefinition);
        }

        [InspectorButton("===== Avatar Spawning =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarSpawning() { }

        [InspectorButton("Spawn Default Avatar", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SpawnDefaultAvatar()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Spawn Default Avatar", "Log in first!");
                return;
            }

            var managedAvatar = await AvatarSdk.LoadDefaultAvatarAsync(
                DefaultAvatarName,
                AvatarParent,
                CustomAnimatorController);

            ProcessSpawnedAvatar(managedAvatar);
        }

        [InspectorButton("\nSpawn Avatar Definition\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SpawnAvatarDefinition()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Spawn Avatar Definition", "Log in first!");
                return;
            }

            var managedAvatar = await AvatarSdk.LoadAvatarByDefinitionAsync(
                AvatarDefinition,
                UserAvatarName,
                AvatarParent,
                CustomAnimatorController);

            ProcessSpawnedAvatar(managedAvatar);
        }

        [InspectorButton("\nSpawn User Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void SpawnUserAvatar()
        {
            if (AvatarSdk.IsLoggedIn is false)
            {
                ShowPopUp("⚠️ Spawn User Avatar", "Log in first!");
                return;
            }

            var managedAvatar = await AvatarSdk.LoadUserAvatarAsync(
                UserAvatarName,
                AvatarParent,
                CustomAnimatorController);

            ProcessSpawnedAvatar(managedAvatar);
        }

        private void ProcessSpawnedAvatar(ManagedAvatar managedAvatar)
        {
            if (managedAvatar is null)
            {
                return;
            }

            SpawnedAvatars.Add(managedAvatar);
            AvatarToDebug = managedAvatar.Component;
            SpawnAvatarStateDisplay?.UpdateStateDisplay(
                SpawnedAvatars.ConvertAll(a => a.Component),
                AvatarToDebug);
            AvatarEditorStateDisplay?.UpdateStateDisplay(AvatarToDebug, AvatarEditorCamera);
        }

        [InspectorButton("===== Avatar Destroying =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarDestroying() { }

        [InspectorButton("\nDestroy Last Spawned Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void DestroyLastSpawnedAvatar()
        {
            if (SpawnedAvatars.Count == 0)
            {
                ShowPopUp("⚠️ Destroy Last Spawned Avatar", "No avatars to destroy!");
                return;
            }

            var lastAvatar = SpawnedAvatars[SpawnedAvatars.Count - 1];
            SpawnedAvatars.RemoveAt(SpawnedAvatars.Count - 1);

            lastAvatar?.Dispose();

            // Update AvatarToDebug to the new last avatar (stack behavior)
            AvatarToDebug = SpawnedAvatars.Count > 0 ? SpawnedAvatars[SpawnedAvatars.Count - 1].Component : null;

            SpawnAvatarStateDisplay?.UpdateStateDisplay(
                SpawnedAvatars.ConvertAll(a => a.Component),
                AvatarToDebug);
            AvatarEditorStateDisplay?.UpdateStateDisplay(AvatarToDebug, AvatarEditorCamera);
        }

        [InspectorButton("Destroy All Spawned Avatars", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void DestroyAllSpawnedAvatars()
        {
            foreach (var avatar in SpawnedAvatars)
            {
                avatar?.Dispose();
            }
            SpawnedAvatars.Clear();
            AvatarToDebug = null;
            SpawnAvatarStateDisplay?.UpdateStateDisplay(null, null);
            AvatarEditorStateDisplay?.UpdateStateDisplay(null, AvatarEditorCamera);
        }

        [InspectorButton("===== Avatar Debugging =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarDebugging() { }

        [InspectorButton("\nGet Debug Avatar Definition\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void GetDebugAvatarDefinition()
        {
            if (AvatarToDebug == null)
            {
                ShowPopUp(title: "⚠️ Get Avatar Definition", message: $"Reference field {nameof(_avatarToDebug)} is not set");
                return;
            }

            var avatarDefinition = AvatarToDebug.ManagedAvatar?.GetDefinition() ?? "";

            if (string.IsNullOrWhiteSpace(avatarDefinition))
            {
                ShowPopUp(title: "⚠️ Get Avatar Definition", message: "Failed to retrieve avatar definition");
                return;
            }

            ShowAvatarDefinitionPopup(avatarDefinition);
        }

        private void ShowAvatarDefinitionPopup(string avatarDefinition)
        {
            ShowPopUp(title: "Avatar Definition",
                message: avatarDefinition,
                buttonTextConfirm: "Copy to clipboard",
                buttonTextCancel: "Close",
                onConfirm: () =>
                {
                    GUIUtility.systemCopyBuffer = avatarDefinition;
                    Debug.Log("Avatar definition copied to clipboard!");
                });
        }

        [InspectorButton("===== Avatar Editor =====", InspectorButtonAttribute.ExecutionMode.EditMode)]
        private void HeaderAvatarDebugEditor() { }

        [InspectorButton("Check Is Avatar Editor Open", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private void CheckIsAvatarEditorOpen()
        {
            var isOpen = AvatarSdk.IsAvatarEditorOpen;
            ShowPopUp("Is Avatar Editor Open?", $"{(isOpen ? "TRUE" : "FALSE")}");
        }

        [InspectorButton("\nEdit Avatar\n", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void OpenAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen)
            {
                ShowPopUp("⚠️ Edit Avatar", "Avatar Editor is already open!");
                return;
            }

            if (AvatarToDebug == null)
            {
                ShowPopUp("⚠️ Edit Avatar", "No avatars spawned! Spawn an avatar first!");
                return;
            }

            try
            {
                await AvatarSdk.OpenAvatarEditorAsync(AvatarToDebug.ManagedAvatar, AvatarEditorCamera);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to open the Avatar Editor: {ex.Message}");
            }
        }

        [InspectorButton("Close Avatar Editor", InspectorButtonAttribute.ExecutionMode.PlayMode)]
        private async void CloseAvatarEditor()
        {
            if (AvatarSdk.IsAvatarEditorOpen is false)
            {
                ShowPopUp("⚠️ Close Avatar Editor", "Avatar Editor is already closed!");
                return;
            }

            try
            {
                await AvatarSdk.CloseAvatarEditorAsync(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to close Avatar Editor: {ex.Message}");
            }
        }

        private void ShowPopUp(string title, string message)
        {
#if UNITY_EDITOR
            EditorUtility.DisplayDialog(title, message, "OK");
#endif
            Debug.LogWarning($"{title}: {message}");
        }

        private void ShowPopUp(string title, string message, string buttonTextConfirm, string buttonTextCancel, Action onConfirm = null, Action onCancel = null)
        {
            Debug.LogWarning($"{title}: {message}");
#if UNITY_EDITOR
            if (EditorUtility.DisplayDialog(title, message, buttonTextConfirm, buttonTextCancel))
            {
                onConfirm?.Invoke();
            }
            else
            {
                onCancel?.Invoke();
            }
#endif
        }
    }
}
