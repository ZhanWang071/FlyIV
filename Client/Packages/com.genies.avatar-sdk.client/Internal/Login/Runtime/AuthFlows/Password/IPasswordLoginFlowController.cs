using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Password
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IPasswordLoginFlowController : IDisposable
#else
    public interface IPasswordLoginFlowController : IDisposable
#endif
    {
        Task<GeniesAuthSignInV2Response> SignInAsync(string email, string password);
        Task<GeniesAuthVerifyEmailV2Response> VerifyEmailAsync(string code);
        Task<GeniesAuthSignUpV2Response> SignUp(string email, string password);
    }
}