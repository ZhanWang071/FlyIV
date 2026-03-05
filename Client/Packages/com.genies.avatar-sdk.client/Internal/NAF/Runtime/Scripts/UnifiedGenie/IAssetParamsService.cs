using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IAssetParamsService
#else
    public interface IAssetParamsService
#endif
    {
        UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId);
    }
}