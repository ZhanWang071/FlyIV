using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Customization.Framework
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ICommand
#else
    public interface ICommand
#endif
    {
        UniTask ExecuteAsync(CancellationToken cancellationToken = default);
        UniTask UndoAsync(CancellationToken cancellationToken = default);
    }
}