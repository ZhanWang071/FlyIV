using Cysharp.Threading.Tasks;
using Genies.Refs;
using GnWrappers;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class IconLoader : ContainerAssetLoaderBase<Texture>
#else
    public sealed class IconLoader : ContainerAssetLoaderBase<Texture>
#endif
    {
        public IconLoader(Handle<ContainerApi> containerApi)
            : base(containerApi) { }

        protected override Future LoadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return ContainerApi.LoadIconAsync(assetId, cParams, cancellationToken);
        }

        protected override BoolFuture PreloadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            // TODO implement real icon preloading
            var promise = new BoolPromise();
            TextureFuture future = ContainerApi.LoadIconAsync(assetId, cParams, cancellationToken);
            future.WaitAsync().ContinueWith(() =>
            {
                try
                {
                    using Texture texture = future.Get();
                    promise.SetValue(!cancellationToken.IsCancelled() && !(texture?.IsNull() ?? true));
                }
                finally
                {
                    future.Dispose();
                    promise.Dispose();
                }
            }).Forget();

            return promise.GetFuture();
        }

        protected override Texture GetResult(Future future)
        {
            return ((TextureFuture)future).Get();
        }
    }
}
