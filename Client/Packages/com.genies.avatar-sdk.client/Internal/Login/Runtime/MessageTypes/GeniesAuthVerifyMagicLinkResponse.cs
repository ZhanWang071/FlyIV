using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthVerifyMagicLinkResponse : GeniesAuthMessage
#else
    public class GeniesAuthVerifyMagicLinkResponse : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            VerificationSuccess,
            InvalidCode,
            ExpiredCode,
            VerificationError
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