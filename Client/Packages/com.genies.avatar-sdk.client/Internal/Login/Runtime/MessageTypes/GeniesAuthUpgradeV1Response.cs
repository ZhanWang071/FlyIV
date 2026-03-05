using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthUpgradeV1Response : GeniesAuthMessage
#else
    public class GeniesAuthUpgradeV1Response : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            UpgradeSuccess,
            ApiException,
            StdException,
            EmptyResponse,
            ClientNotInitialized,
            UpgradeFailed,
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
                if (Contains(ErrorMessage, "upgradeuserv1") ||
                    Contains(ErrorMessage, "failed to create workos user"))
                {
                    parsed = StatusCode.UpgradeFailed;
                    StatusCodeString = nameof(StatusCode.UpgradeFailed);
                }
            }

            ResponseStatusCode = parsed;
        }

        private static bool Contains(string s, string needle) =>
            s?.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}