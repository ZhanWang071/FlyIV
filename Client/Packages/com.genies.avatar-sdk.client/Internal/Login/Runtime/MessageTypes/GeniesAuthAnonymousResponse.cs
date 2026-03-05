using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthAnonymousResponse : GeniesAuthMessage
#else
    public class GeniesAuthAnonymousResponse : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            AnonymousSignUpSuccess,
            AnonymousRefreshSuccess,
            AnonymousUpgradeSuccess,
            AnonymousError
        }

        public StatusCode ResponseStatusCode = StatusCode.None;

        public override void OnAfterDeserialize()
        {
            if (!Enum.TryParse(StatusCodeString, out StatusCode parsed))
            {
                parsed = StatusCode.None;
            }

            ResponseStatusCode = parsed;
        }
    }
}