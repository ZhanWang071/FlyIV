using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IReferenceShapesLoader
#else
    public interface IReferenceShapesLoader
#endif
    {
        UniTask<Dictionary<string, IReferenceShape>> LoadShapesAsync();
    }
}