using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class ReferenceShapesLoaderAsset : ScriptableObject, IReferenceShapesLoader
#else
    public abstract class ReferenceShapesLoaderAsset : ScriptableObject, IReferenceShapesLoader
#endif
    {
        public abstract UniTask<Dictionary<string, IReferenceShape>> LoadShapesAsync();
    }
}