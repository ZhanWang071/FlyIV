using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for instant login operations that attempt to automatically authenticate users using cached credentials.
    /// Contains status information about session validation and token refresh processes.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthInstantLoginResponse : GeniesAuthMessage
#else
    public class GeniesAuthInstantLoginResponse : GeniesAuthMessage
#endif
    {
        /// <summary>The specific status code for the instant login operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for instant login operations.</summary>
        public enum StatusCode
        {
            None,
            SessionNotFound,
            SessionValid,
            SessionRefreshed,
            SessionTokenRefreshFailed,
            SessionTokenValidationFailed
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