using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Otp
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IHybridOtpLoginFlowController : IDisposable
#else
    public interface IHybridOtpLoginFlowController : IDisposable
#endif
    {
        Task<GeniesAuthStartHybridOtpResponse> SubmitCredentialAsync(string email);
        Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string email, string code);
        Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string email);
        Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email);            
        Task<GeniesAuthSignUpV2Response> SignUp(string email);
    }
}