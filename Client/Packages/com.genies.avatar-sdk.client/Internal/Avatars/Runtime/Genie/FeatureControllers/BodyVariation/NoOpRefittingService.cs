using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NoOpRefittingService : IRefittingService
#else
    public sealed class NoOpRefittingService : IRefittingService
#endif
    {
        public UniTask LoadAllVectorsAsync()
            => UniTask.CompletedTask;
        
        public string GetBodyVariationBlendShapeName(string bodyVariation)
            => string.Empty;

        public UniTask AddBodyVariationBlendShapeAsync(OutfitAsset asset, string bodyVariation)
            => UniTask.CompletedTask;

        public UniTask WaitUntilReadyAsync()
            => UniTask.CompletedTask;
    }
}