using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Anonymous
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAnonymousLoginFlowController : IDisposable
#else
    public interface IAnonymousLoginFlowController : IDisposable
#endif
    {
        Task<GeniesAuthAnonymousResponse>   SignInAnonymouslyAsync(string applicationId = ""); 
    }
}