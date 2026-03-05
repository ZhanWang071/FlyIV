using Genies.Refs;
using GnWrappers;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class CombinableAssetLoader : ContainerAssetLoaderBase<Entity>
#else
    public sealed class CombinableAssetLoader : ContainerAssetLoaderBase<Entity>
#endif
    {
        public CombinableAssetLoader(Handle<ContainerApi> containerApi)
            : base(containerApi) { }

        protected override Future LoadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return ContainerApi.LoadCombinableAssetAsync(assetId, cParams, cancellationToken);
        }

        protected override BoolFuture PreloadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return ContainerApi.PreloadCombinableAssetAsync(assetId, cParams, cancellationToken);
        }

        protected override Entity GetResult(Future future)
        {
            return ((EntityFuture)future).Get();
        }
    }
}
