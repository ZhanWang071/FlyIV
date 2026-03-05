using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for token validity verification operations.
    /// Contains status information about the validation of authentication tokens including expiration and format checks.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthTokenValidityResponse : GeniesAuthMessage
#else
    public class GeniesAuthTokenValidityResponse : GeniesAuthMessage
#endif
    {
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        public enum StatusCode
        {
            None,
            InvalidTokenFormat,
            TokenPayloadParseFailed,
            MissingExpiration,
            TokenExpired,
            TokenValid,
            TokenException,
            TokenError
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            if (!Enum.TryParse(StatusCodeString, true, out ResponseStatusCode))
            {
                ResponseStatusCode = StatusCode.None;
            }
        }

        public override string ToString()
        {
            return $"{base.ToString()}, statusCode: {ResponseStatusCode}, statusCodeString: {StatusCodeString}";
        }
    }
}