using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for OTP (One-Time Password) submission operations.
    /// Contains status information about the OTP verification process.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthSendOtpResponse : GeniesAuthMessage
#else
    public class GeniesAuthSendOtpResponse : GeniesAuthMessage
#endif
    {
        /// <summary>
        /// The specific status code for the OTP submission operation.
        /// </summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>
        /// Defines the possible status codes for OTP submission operations.
        /// </summary>
        public enum StatusCode
        {
            None,
            SendOtpFailure,
            SendOtpSuccess,
            SendOtpError,
            AccountUpgradeRequired,
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