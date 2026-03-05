using System;

namespace Genies.Login.AuthMessages
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthVerifyEmailV2Response : GeniesAuthMessage
#else
    public class GeniesAuthVerifyEmailV2Response : GeniesAuthMessage
#endif
    {
        public enum StatusCode
        {
            None,
            VerificationSuccess,
            InvalidCode,
            ExpiredCode,
            AlreadyVerified,
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