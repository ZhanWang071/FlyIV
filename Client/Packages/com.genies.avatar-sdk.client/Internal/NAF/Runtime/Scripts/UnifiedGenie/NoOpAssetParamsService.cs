using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NoOpAssetParamsService : IAssetParamsService
#else
    public sealed class NoOpAssetParamsService : IAssetParamsService
#endif
    {
        public UniTask<Dictionary<string, string>> FetchParamsAsync(string assetId)
        {
            return UniTask.FromResult<Dictionary<string, string>>(null);
        }
    }
}