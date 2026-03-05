using Cysharp.Threading.Tasks;
using GnWrappers;

using CancellationToken = System.Threading.CancellationToken;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class FutureExtensions
#else
    public static class FutureExtensions
#endif
    {
        public static UniTask WaitAsync(this Future future)
        {
            return UniTask.WaitUntil(future.IsReady);
        }

        public static async UniTask WaitAsync(this Future future, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            while (!future.IsReady())
            {
                await UniTask.Yield();
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }
}
