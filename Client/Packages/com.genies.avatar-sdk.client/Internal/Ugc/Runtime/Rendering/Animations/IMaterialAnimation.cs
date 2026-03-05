using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IMaterialAnimation
#else
    public interface IMaterialAnimation
#endif
    {
        Material Material { get; set; }
        bool IsPlaying { get; }

        UniTask PlayAsync(ValueAnimation animation, bool ignoreIfPlaying = false);
        void Stop();
        void StopNoRestore();
    }
}
