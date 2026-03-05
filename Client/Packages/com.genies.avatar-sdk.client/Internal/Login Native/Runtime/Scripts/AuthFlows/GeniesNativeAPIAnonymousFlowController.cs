using System.Threading.Tasks;
using Genies.Login.Anonymous;
using Genies.Login.AuthMessages;
using Genies.Login.Native.Data;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIAnonymousFlowController : IAnonymousLoginFlowController
#else
    public sealed class GeniesNativeAPIAnonymousFlowController : IAnonymousLoginFlowController
#endif
    {
        private GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIAnonymousFlowController(GeniesNativeAPIAuth auth) => _auth = auth;

        public async Task<GeniesAuthAnonymousResponse> SignInAnonymouslyAsync(string clientId = "")
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                var settings = GeniesAuthSettings.LoadFromResources();

                if (settings != null)
                {
                    clientId = settings.ClientId;
                }
            }

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return new GeniesAuthAnonymousResponse { 
                    Status = "error",
                    Message = "Missing client ID. Please embed one in Genies Auth settings and try again",
                    ErrorMessage = "Missing client ID. Please embed one in Genies Auth settings and try again",
                };
            }
            
            var res = await _auth.AnonymousSignUpAsync(clientId);

            // Anonymous session is still a session—hydrate + schedule
            if (res.IsSuccessful)
            {
                // Currently, anonymous users cant have profiles
                // await _auth.GetUserAttributesAsync();
                _auth.StartTokenExpiryTimer();
                _auth.InvokeOnUserLoggedIn();
            }

            return res;
        }

        public void Dispose() { _auth = null; }
    }
}
