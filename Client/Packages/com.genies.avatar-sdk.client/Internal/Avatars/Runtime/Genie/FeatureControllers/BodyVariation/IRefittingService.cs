using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IRefittingService
#else
    public interface IRefittingService
#endif
    {
        UniTask LoadAllVectorsAsync();
        string GetBodyVariationBlendShapeName(string bodyVariation);
        UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation);
        UniTask WaitUntilReadyAsync();
    }
}
