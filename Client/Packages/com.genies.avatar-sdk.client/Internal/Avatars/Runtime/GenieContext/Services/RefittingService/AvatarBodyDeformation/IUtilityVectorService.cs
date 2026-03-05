using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IUtilityVectorService
#else
    public interface IUtilityVectorService
#endif
    {
        UniTask<UtilityVector> LoadAsync(string vectorId);

        UtilMeshName GetUtilityMeshFromAssetCategory(OutfitAsset asset);
    }
}