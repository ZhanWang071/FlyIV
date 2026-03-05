using Cysharp.Threading.Tasks;
using Genies.Refs;
using GnWrappers;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class TextureLoader : ContainerAssetLoaderBase<Texture>
#else
    public sealed class TextureLoader : ContainerAssetLoaderBase<Texture>
#endif
    {
        public TextureLoader(Handle<ContainerApi> containerApi)
            : base(containerApi) { }

        protected override Future LoadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return ContainerApi.LoadTextureAsync(assetId, cParams, cancellationToken);
        }

        protected override BoolFuture PreloadAsyncInternal(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            // TODO implement real texture preloading
            var promise = new BoolPromise();
            TextureFuture future = ContainerApi.LoadTextureAsync(assetId, cParams, cancellationToken);
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
