using System;
using Cysharp.Threading.Tasks;
using Genies.Login.Anonymous;
using Genies.Login.Native;

namespace Genies.Sdk
{
    internal partial class CoreSdk
    {
        public class LoginAnonymous
        {
            private CoreSdk Parent { get; }

            private LoginAnonymous() { }

            internal LoginAnonymous(CoreSdk parent)
            {
                Parent = parent;
            }

            /// <summary>
            /// Invoked when anonymous sign-in succeeds. Provides the applicationId used.
            /// </summary>
            public event Action<string> LoginAnonymousSucceeded;

            /// <summary>
            /// Invoked when anonymous sign-in fails. Provides the applicationId and failure reason.
            /// </summary>
            public event Action<(string applicationId, string failReason)> LoginAnonymousFailed;

            private IAnonymousLoginFlowController AnonymousLoginFlowController { get; set; }

            /// <summary>
            /// Starts the anonymous login flow by signing in with the given client ID. Passing nothing attempts to pull
            /// the id there were given via the Genies Auth settings in player settings
            /// </summary>
            /// <param name="clientId">The application identifier to use for anonymous sign-in.</param>
            /// <returns>
            /// A tuple indicating success and an optional failure reason.
            /// </returns>
            public async UniTask<(bool succeeded, string failReason)> StartAnonymousLoginAsync(string clientId = "")
            {
                if (await Parent.InitializeAsync() is false)
                {
                    var failReason = "Genies SDK initialization failed.";
                    InvokeLoginAnonymousFailed(clientId, failReason);
                    return (false, failReason);
                }

                if (Parent.LoginApi.IsLoggedIn)
                {
                    var failReason = "User is already logged in.";
                    InvokeLoginAnonymousFailed(clientId, failReason);
                    return (false, failReason);
                }

                // Reset any existing anonymous flow
                AnonymousLoginFlowController?.Dispose();
                AnonymousLoginFlowController = null;

                AnonymousLoginFlowController = GeniesLoginSdk.StartAnonymousLogin();

                var result = await AnonymousLoginFlowController.SignInAnonymouslyAsync(clientId);

                if (result.IsSuccessful)
                {
                    InvokeLoginAnonymousSucceeded(clientId);
                    return (true, null);
                }

                InvokeLoginAnonymousFailed(clientId, result.ErrorMessage);
                return (false, result.ErrorMessage);
            }

            private void InvokeLoginAnonymousSucceeded(string applicationId) =>
                LoginAnonymousSucceeded?.Invoke(applicationId);

            private void InvokeLoginAnonymousFailed(string applicationId, string reason) =>
                LoginAnonymousFailed?.Invoke((applicationId, reason));
        }
    }
}
