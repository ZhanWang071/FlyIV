using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Sdk.Samples.Common
{
    public class GeniesLoginUI : MonoBehaviour
    {
        [Header("Status")]
        public TextMeshProUGUI statusText;

        [Header("Email Entry")]
        public TMP_InputField emailInputField;
        public Button submitEmailButton;
        public GameObject emailCard;

        [Header("Sign Up")]
        public Button signUpButton;

        [Header("Code Entry")]
        public TMP_InputField otpInputField;
        public Button submitOtpButton;
        public Button requestNewCodeButton;
        public GameObject authCard;
        
        [Header("Anonymous Login")]
        public Button signInAnonymouslyButton;

        [Header("Logged-in UI")]
        public Button openAvatarEditor;
        public Button logOutButton;
        public GameObject titleBar;
        
        private readonly string anonWarning = "You're logged in as a guest user. Log out to log in with your account";
        private bool anonymousLoggedIn = false;

        
        public Action AvatarEditorButtonPressed;
        

        /// <summary>
        /// Shows the anonymous warning if the user is logged in anonymously.
        /// </summary>
        private void UpdateAnonymousWarningDisplay()
        {
            if (anonymousLoggedIn && statusText != null)
            {
                statusText.gameObject.SetActive(true);
                UpdateStatus(anonWarning);
            }
        }
        // ------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------

        private async void Awake()
        {
            // Initial UI state
            SetBusy(false);
            UpdateStatus("Initializing...");

            // Init SDK (idempotent)
            await AvatarSdk.InitializeAsync();

            var instantLoginResult = await AvatarSdk.TryInstantLoginAsync(); 
            anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously;
            // Subscribe to the same events used by the Debug/LoginEmailOtp pattern
            SubscribeEvents();
            ResetUI();
        }

        private void OnEnable()
        {
            // Safety: ensure listeners are added if domain reloaded
            WireButtons();
        }

        private void OnDisable()
        {
            UnwireButtons();
        }

        private void OnDestroy()
        {
            // Clean up events when this sample is removed
            UnsubscribeEvents();
        }

        // ------------------------------------------------------------------
        // Event subscriptions (LoginEmailOtp pattern)
        // ------------------------------------------------------------------

        private void SubscribeEvents()
        {
            AvatarSdk.Events.UserLoggedIn += OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut += OnUserLoggedOut;
            
            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded += OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed += OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded += OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed += OnEmailCodeSubmissionFailed;
            AvatarSdk.Events.LoginAnonymousSucceeded += OnLoginAnonymousSucceeded;
        }

        private void OnLoginAnonymousSucceeded(string obj)
        {
            anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously;
            ShowStatusText(true);
            UpdateAnonymousWarningDisplay();
        }

        public void ShowAvatarEditorButton(bool isVisible)
        {
            if (openAvatarEditor != null)
            {
                openAvatarEditor.gameObject.SetActive(isVisible);
            }
        }
        public void ShowStatusText(bool isVisible)
        {
            // Force visibility if user is logged in anonymously (to show warning)
            if (anonymousLoggedIn)
            {
                isVisible = true;
            }
            
            if (statusText != null)
            {
                statusText.gameObject.SetActive(isVisible);
            }
        }

        private void UnsubscribeEvents()
        {
            AvatarSdk.Events.UserLoggedIn -= OnUserLoggedIn;
            AvatarSdk.Events.UserLoggedOut -= OnUserLoggedOut;

            AvatarSdk.Events.LoginEmailOtpCodeRequestSucceeded -= OnEmailCodeRequestSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeRequestFailed -= OnEmailCodeRequestFailed;

            AvatarSdk.Events.LoginEmailOtpCodeSubmissionSucceeded -= OnEmailCodeSubmissionSucceeded;
            AvatarSdk.Events.LoginEmailOtpCodeSubmissionFailed -= OnEmailCodeSubmissionFailed;
            
            AvatarSdk.Events.LoginAnonymousSucceeded -= OnLoginAnonymousSucceeded;
            
        }

        // ------------------------------------------------------------------
        // Flow state / wiring
        // ------------------------------------------------------------------

        public void ResetUI()
        {
            if (AvatarSdk.IsLoggedIn)
            {
                OnUserLoggedIn();
                return;
            }

            OnUserLoggedOut();
        }

        private void WireButtons()
        {
            if (submitEmailButton != null)
            {
                submitEmailButton.onClick.RemoveAllListeners();
                submitEmailButton.onClick.AddListener(SubmitEmailForMagicAuth);
            }

            if (signUpButton != null)
            {
                signUpButton.onClick.RemoveAllListeners();
                signUpButton.onClick.AddListener(GoToSignUpPage);
            }

            if (submitOtpButton != null)
            {
                submitOtpButton.onClick.RemoveAllListeners();
                submitOtpButton.onClick.AddListener(SubmitOtpCode);
            }

            if (requestNewCodeButton != null)
            {
                requestNewCodeButton.onClick.RemoveAllListeners();
                requestNewCodeButton.onClick.AddListener(RequestNewCode);
            }
            
            if (signInAnonymouslyButton != null)
            {
                signInAnonymouslyButton.onClick.RemoveAllListeners();
                signInAnonymouslyButton.onClick.AddListener(SignInAnonymously);
            }

            if (openAvatarEditor != null)
            {
                openAvatarEditor.onClick.RemoveAllListeners();
                openAvatarEditor.onClick.AddListener(OpenAvatarEditor);
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton != null)
            {
                logOutButton.onClick.RemoveAllListeners();
                logOutButton.onClick.AddListener(LogOutAndExit);
            }
        }

        private void OpenAvatarEditor()
        {
            AvatarEditorButtonPressed?.Invoke();
        }

        private async void LogOutAndExit()
        {
            await AvatarSdk.LogOutAsync();
        }

        private void GoToSignUpPage()
        {
            Application.OpenURL(AvatarSdk.UrlGeniesHubSignUp);
        }

        private void UnwireButtons()
        {
            submitEmailButton?.onClick.RemoveAllListeners();
            signUpButton?.onClick.RemoveAllListeners();
            submitOtpButton?.onClick.RemoveAllListeners();
            requestNewCodeButton?.onClick.RemoveAllListeners();
            logOutButton?.onClick.RemoveAllListeners();
        }
        // ------------------------------------------------------------------
        // UI helpers
        // ------------------------------------------------------------------

        private void SetBusy(bool busy)
        {
            if (submitEmailButton)
            {
                submitEmailButton.interactable = !busy;
            }

            if (signUpButton)
            {
                signUpButton.interactable = !busy;
            }

            if (submitOtpButton)
            {
                submitOtpButton.interactable = !busy;
            }

            if (requestNewCodeButton)
            {
                requestNewCodeButton.interactable = !busy;
            }

            if (emailInputField)
            {
                emailInputField.interactable = !busy;
            }

            if (otpInputField)
            {
                otpInputField.interactable = !busy;
            }

            if (openAvatarEditor )
            {
                openAvatarEditor.interactable = !busy;
            }

            if (logOutButton)
            {
                logOutButton.interactable = !busy;
            }
        }

        private void ShowEmailEntryUI()
        {
            if (statusText != null)
            {
                statusText.gameObject.SetActive(true);
            }

            if (emailCard != null)
            {
                emailCard.gameObject.SetActive(true);
            }

            if (authCard != null)
            {
                authCard.gameObject.SetActive(false);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            SetBusy(false);
        }

        private void ShowCodeEntryUI()
        {
            if (emailCard != null)
            {
                emailCard.gameObject.SetActive(false);
            }

            if (authCard != null)
            {
                authCard.gameObject.SetActive(true);
            }

            if (openAvatarEditor)
            {
                openAvatarEditor.gameObject.SetActive(false);
            }

            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            SetBusy(false);
            UpdateStatus("Enter the verification code sent to your email.");
        }

        private void UpdateStatus(string text)
        {
            if (statusText)
            {
                statusText.text = text;
            }
        }

        private void ShowProcessing(string message)
        {
            SetBusy(true);
            UpdateStatus(message);
        }

        // ------------------------------------------------------------------
        // Button handlers (call simple SDK methods; events drive the UI)
        // ------------------------------------------------------------------

        private async void SubmitEmailForMagicAuth()
        {
            if (emailInputField == null)
            {
                UpdateStatus("Email field not set.");
                return;
            }

            var email = emailInputField.text?.Trim();
            if (string.IsNullOrWhiteSpace(email))
            {
                UpdateStatus("Please enter a valid email.");
                return;
            }

            // Basic email format validation
            if (!email.Contains("@") || !email.Contains(".") || email.Length < 5)
            {
                UpdateStatus("Please enter a valid email address.");
                return;
            }

            try
            {
                ShowProcessing($"Sending verification code to {email}...");
                await AvatarSdk.StartLoginEmailOtpAsync(email);
                // Success/failure UI continues via events.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to request email code: {ex.Message}\n{ex.StackTrace}", this);
                SetBusy(false);
                UpdateStatus($"Failed to request code: {ex.Message}");
                ShowEmailEntryUI();
            }
        }

        private async void SubmitOtpCode()
        {
            if (otpInputField == null)
            {
                UpdateStatus("OTP field not set.");
                return;
            }

            var code = otpInputField.text?.Trim();
            if (string.IsNullOrWhiteSpace(code))
            {
                UpdateStatus("Please enter the verification code.");
                return;
            }

            try
            {
                ShowProcessing("Verifying code...");
                await AvatarSdk.SubmitEmailOtpCodeAsync(code);
                // Events will handle success/failure transitions.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to submit OTP code: {ex.Message}\n{ex.StackTrace}", this);
                SetBusy(false);
                UpdateStatus($"Failed to submit code: {ex.Message}");
                ShowCodeEntryUI();
            }
        }

        private async void RequestNewCode()
        {
            try
            {
                ShowProcessing("Resending email code...");
                await AvatarSdk.ResendEmailCodeAsync();
                // Events will handle follow-up UI.
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to resend email code: {ex.Message}\n{ex.StackTrace}", this);
                SetBusy(false);
                UpdateStatus($"Failed to resend code: {ex.Message}");
                ShowCodeEntryUI();
            }
        }
        private async void SignInAnonymously()
        {
            try
            {
                ShowProcessing("Signing in as guest...");
                var (succeeded, failReason) = await AvatarSdk.StartLoginAnonymousAsync();

                // Update anonymous state from SDK (single source of truth)
                anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously;

                // Success should trigger UserLoggedIn and your existing UI flow.
                if (!succeeded)
                {
                    SetBusy(false);
                    ResetUI();
                    UpdateStatus(string.IsNullOrWhiteSpace(failReason)
                        ? "Anonymous sign-in failed. Please try again."
                        : failReason);
                }
            }
            catch (Exception ex)
            {
                anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously; // Ensure state is correct even on error
                SetBusy(false);
                ResetUI();
                UpdateStatus($"Anonymous sign-in failed: {ex.Message}");
            }
        }

        // ------------------------------------------------------------------
        // Event handlers (mirror LoginEmailOtp pattern)
        // ------------------------------------------------------------------

        private void OnUserLoggedIn()
        {
            // Update anonymous state to ensure accuracy
            anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously;
            
            // Clear busy and finish flow
            SetBusy(false);
            UpdateStatus("Logged in successfully.");

            // Show anonymous warning if applicable
            if (anonymousLoggedIn)
            {
                UpdateAnonymousWarningDisplay();
            }
            else if (statusText != null)
            {
                statusText.gameObject.SetActive(false);
            }

            if (emailCard != null)
            {
                emailCard.gameObject.SetActive(false);
            }

            if (authCard != null)
            {
                authCard.gameObject.SetActive(false);
            }

            if (titleBar != null)
            {
                titleBar.gameObject.SetActive(true);
            }

            if (logOutButton != null)
            {
                logOutButton.gameObject.SetActive(true);
            }

        }

        private void OnUserLoggedOut()
        {
            // Reset anonymous state when user logs out
            anonymousLoggedIn = AvatarSdk.IsLoggedInAnonymously;
            
            if (logOutButton)
            {
                logOutButton.gameObject.SetActive(false);
            }

            if (AvatarSdk.IsAwaitingEmailOtpCode)
            {
                // We've already requested a code; go straight to code entry
                ShowCodeEntryUI();
                UpdateStatus("Enter the verification code sent to your email.");
            }
            else
            {
                // Fresh flow
                ShowEmailEntryUI();
                UpdateStatus("Enter your email");
            }
        }

        private void OnEmailCodeRequestSucceeded(string email)
        {
            SetBusy(false);
            ShowCodeEntryUI();

            // Optional: reflect where the code was sent
            if (statusText != null)
            {
                UpdateStatus($"A verification code was sent to {email}.");
            }
        }

        private void OnEmailCodeRequestFailed((string email, string failReason) fail)
        {
            SetBusy(false);

            // If we were already waiting for a code, keep the code UI; otherwise, return to email
            if (AvatarSdk.IsAwaitingEmailOtpCode)
            {
                ShowCodeEntryUI();
                UpdateStatus($"Could not resend code: {fail.failReason}");
            }
            else
            {
                ShowEmailEntryUI();
                UpdateStatus(string.IsNullOrWhiteSpace(fail.failReason)
                    ? "Could not request code. Please try again."
                    : fail.failReason);
            }
        }

        private void OnEmailCodeSubmissionSucceeded(string code)
        {
            // Logged-in event will handle final teardown; just clear busy here
            SetBusy(false);
            UpdateStatus("Code accepted. Finalizing login...");
        }

        private void OnEmailCodeSubmissionFailed((string code, string failReason) fail)
        {
            SetBusy(false);
            ShowCodeEntryUI();
            UpdateStatus(string.IsNullOrWhiteSpace(fail.failReason)
                ? "Verification failed. Please try again."
                : fail.failReason);
        }

        public void ShowTitleBar(bool isVisible)
        {
            if (titleBar != null)
            {
                titleBar.gameObject.SetActive(isVisible);
            }
        }
    }
}
