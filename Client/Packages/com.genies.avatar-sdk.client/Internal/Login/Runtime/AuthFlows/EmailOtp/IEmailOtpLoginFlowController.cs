using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.EmailOtp
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IEmailOtpLoginFlowController : IDisposable
#else
    public interface IEmailOtpLoginFlowController : IDisposable
#endif
    {
        Task<GeniesAuthStartEmailOtpResponse> SubmitEmailAsync(string email);
        Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string email, string code);
        Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string email);
        Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email);            
        Task<GeniesAuthSignUpV2Response> SignUp(string email);
    }
}