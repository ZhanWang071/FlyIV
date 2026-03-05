// Anonymous Login Flow:
//
// The anonymous login flow creates a temporary, anonymous session using an application identifier.
// This session behaves as a logged-in user (tokens, expiry timers, etc.), but can later be upgraded
// to a full account using an email and optional profile fields.
//
// Flow Steps:
// 1. Call StartLoginAnonymousAsync(applicationId) to initiate anonymous sign-in
//    - Validates the applicationId
//    - On success, creates an anonymous session and starts token expiry timers
//    - Triggers one of two events based on the result:
//      a) LoginAnonymousSucceeded: Anonymous sign-in succeeded
//      b) LoginAnonymousFailed: Anonymous sign-in failed (invalid applicationId or other error)
//
// 2. (Optional) To convert the anonymous user into a full account, call UpgradeAnonymousAsync(...)
//    - Validates the provided email and optional profile data
//    - On success, upgrades the anonymous user and keeps the session active
//    - Triggers one of two events based on the result:
//      a) AnonymousUpgradeComplete: Upgrade succeeded
//      b) AnonymousUpgradeFailed: Upgrade failed (validation or server error)

using System;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        public static partial class Events
        {
            /// <summary>
            /// Invoked when an anonymous sign-in succeeds. Provides the applicationId used.
            /// </summary>
            public static event Action<string> LoginAnonymousSucceeded
            {
                add => Instance.CoreSdk.LoginAnonymously.LoginAnonymousSucceeded += value;
                remove => Instance.CoreSdk.LoginAnonymously.LoginAnonymousSucceeded -= value;
            }

            /// <summary>
            /// Invoked when an anonymous sign-in fails. Provides the applicationId and failure reason.
            /// </summary>
            public static event Action<(string applicationId, string failReason)> LoginAnonymousFailed
            {
                add => Instance.CoreSdk.LoginAnonymously.LoginAnonymousFailed += value;
                remove => Instance.CoreSdk.LoginAnonymously.LoginAnonymousFailed -= value;
            }
        }

        /// <summary>
        /// Starts the anonymous login flow by signing in with the given applicationId.
        /// </summary>
        /// <param name="applicationId">The application identifier to use for anonymous sign-in.</param>
        /// <returns>
        /// A tuple indicating success and an optional failure reason.
        /// </returns>
        public static async UniTask<(bool succeeded, string failReason)> StartLoginAnonymousAsync(string applicationId = "")
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginAnonymously.StartAnonymousLoginAsync(applicationId);
        }
    }
}
