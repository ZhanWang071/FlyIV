using System;
using Genies.Refs;
using GnWrappers;

namespace Genies.Naf
{
    /// <summary>
    /// Native Container API service manager that provides high level asset loading and preloading functionalities.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class ContainerService : IDisposable
#else
    public sealed class ContainerService : IDisposable
#endif
    {
        public CombinableAssetLoader CombinableAsset { get; }
        public TextureLoader         Texture         { get; }
        public IconLoader            Icon            { get; }

        public bool IsDisposed => !_containerApi.IsAlive;

        private readonly Ref<ContainerApi> _containerApi;

        /// <summary>
        /// Initializes a new instance using the default <see cref="NafAssetResolverConfig"/> instance.
        /// </summary>
        public ContainerService()
            : this(NafAssetResolverConfig.Default.Serialize())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerService"/> class with the specified configuration.
        /// </summary>
        /// <param name="config">The asset resolver configuration. If null, uses the default configuration.</param>
        public ContainerService(NafAssetResolverConfig config)
            : this(config ? config.Serialize() : NafAssetResolverConfig.Default.Serialize())
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerService"/> class with a serialized configuration.
        /// </summary>
        /// <param name="serializedConfig">The serialized asset resolver configuration JSON string.</param>
        public ContainerService(string serializedConfig)
        {
            _containerApi = CreateRef.FromDisposable(new ContainerApi(serializedConfig));

            CombinableAsset = new CombinableAssetLoader(_containerApi.Handle);
            Texture         = new TextureLoader(_containerApi.Handle);
            Icon            = new IconLoader(_containerApi.Handle);
        }

        public void Dispose()
        {
            _containerApi.Dispose();
        }
    }
}
