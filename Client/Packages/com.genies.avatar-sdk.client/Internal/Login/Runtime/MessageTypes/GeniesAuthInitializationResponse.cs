using System;

namespace Genies.Login.AuthMessages
{
    /// <summary>
    /// Response message for login system initialization operations.
    /// Contains status information about the initialization process and user sign-up status.
    /// </summary>
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GeniesAuthInitializationResponse : GeniesAuthMessage
#else
    public class GeniesAuthInitializationResponse : GeniesAuthMessage
#endif
    {
        /// <summary>Indicates whether a new user was signed up during the initialization process.</summary>
        public bool UserSignedUp = false;

        /// <summary>The specific status code for the initialization operation.</summary>
        [NonSerialized]
        public StatusCode ResponseStatusCode = StatusCode.None;

        /// <summary>Defines the possible status codes for initialization operations.</summary>
        public enum StatusCode
        {
            None,
            InitializationSuccess,
            InitializationException,
            InitializationFailed,
            ConfigUpdated
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
            return $"{base.ToString()}, statusCode: {ResponseStatusCode}, statusCodeString: {StatusCodeString}, userSignedUp: {UserSignedUp}";
        }
    }
}