using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthResendMagicLinkResponse : GeniesAuthMessage
#else
    public class GeniesAuthResendMagicLinkResponse : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            ResentSuccessfully,
            ResendError
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