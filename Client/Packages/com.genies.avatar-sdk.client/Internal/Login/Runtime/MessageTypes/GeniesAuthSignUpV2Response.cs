using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthSignUpV2Response : GeniesAuthMessage
#else
    public class GeniesAuthSignUpV2Response : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            SignUpSuccess,
            ApiException,
            StdException,
            EmptyResponse,
            ClientNotInitialized,
            EmailInUse,
        }

        public StatusCode ResponseStatusCode = StatusCode.None;

        public override void OnAfterDeserialize()
        {
            if (!Enum.TryParse(StatusCodeString, out StatusCode parsed))
            {
                parsed = StatusCode.None;
            }

            if ((parsed == StatusCode.ApiException || parsed == StatusCode.None) &&
                !string.IsNullOrEmpty(ErrorMessage))
            {
                if (Contains(ErrorMessage, "code = AlreadyExists") ||
                    (Contains(ErrorMessage, "already exists") && Contains(ErrorMessage, "email")))
                {
                    parsed = StatusCode.EmailInUse;
                    StatusCodeString = nameof(StatusCode.EmailInUse);
                }
            }

            ResponseStatusCode = parsed;
        }

        private static bool Contains(string s, string needle) =>
            s?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}