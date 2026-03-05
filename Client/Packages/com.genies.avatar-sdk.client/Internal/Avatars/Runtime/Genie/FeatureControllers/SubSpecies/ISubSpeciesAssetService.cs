using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ISubSpeciesAssetService
#else
    public interface ISubSpeciesAssetService
#endif
    {
        UniTask<Ref<SubSpeciesContainer>> LoadContainerAsync(string id, string lod = AssetLod.Default);
    }
}
