using Cysharp.Threading.Tasks;

namespace Genies.Ugc
{
    /// <summary>
    /// Must be able to fetch a <see cref="Wearable"/> instance from a given ID.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IUgcWearableDefinitionService
#else
    public interface IUgcWearableDefinitionService
#endif
    {
        UniTask<Wearable> FetchAsync(string wearableId);
    }
}
