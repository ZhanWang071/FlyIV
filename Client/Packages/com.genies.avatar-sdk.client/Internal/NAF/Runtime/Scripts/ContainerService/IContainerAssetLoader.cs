using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Naf
{
    /// <summary>
    /// Represents an instance capable of loading a single asset type from the native Container API. Container API
    /// assets are usually cached on disk after the first fetch, so subsequent load operations for the same asset should
    /// be faster. With this loader, you can preload assets to cache them on disk before they are needed.
    /// </summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IContainerAssetLoader<T>
#else
    public interface IContainerAssetLoader<T>
#endif
    {
        /// <summary>
        /// Loads an asset by its ID with optional parameters.
        /// Performs fetch, cache, and load operations. Fetch and cache are performed if the asset is not already cached on disk.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to load.</param>
        /// <param name="parameters">Optional dictionary of parameters for the operation.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded asset.</returns>
        UniTask<T> LoadAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads an asset from a load request.
        /// Performs fetch, cache, and load operations. Fetch and cache are performed if the asset is not already cached on disk.
        /// </summary>
        /// <param name="request">The request containing the asset ID and parameters.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded asset.</returns>
        UniTask<T> LoadAsync(in ContainerAssetRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads multiple assets by their IDs with optional shared parameters.
        /// Performs fetch, cache, and load operations for each asset. Fetch and cache are performed if an asset is not already cached on disk.
        /// If providing more than one asset ID, please note that this batched version will only log any exceptions that
        /// occur during the load operations, so successful loads will still be returned even if some of the assets fail
        /// to load.
        /// </summary>
        /// <param name="assetIds">The collection of asset IDs to load.</param>
        /// <param name="parameters">Optional dictionary of shared parameters for all operations.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>A task that represents the asynchronous load operations. The task result contains an array of loaded assets.</returns>
        UniTask<T[]> LoadAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Loads multiple assets from a collection of load requests.
        /// Performs fetch, cache, and load operations for each asset. Fetch and cache are performed if an asset is not already cached on disk.
        /// If providing more than one load request, please note that this batched version will only log any exceptions
        /// that occur during the load operations, so successful loads will still be returned even if some of the assets
        /// fail to load.
        /// </summary>
        /// <param name="loadRequests">The collection of requests to load.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>A task that represents the asynchronous load operations. The task result contains an array of loaded assets.</returns>
        UniTask<T[]> LoadAsync(IEnumerable<ContainerAssetRequest> loadRequests, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads an asset by its ID with optional parameters.
        /// Only if not cached yet, performs fetch and cache operations.
        /// </summary>
        /// <param name="assetId">The unique identifier of the asset to preload.</param>
        /// <param name="parameters">Optional dictionary of parameters for the operation.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>Whether the asset was successfully preloaded.</returns>
        UniTask<bool> PreloadAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads an asset from a load request.
        /// Only if not cached yet, performs fetch and cache operations.
        /// </summary>
        /// <param name="request">The request containing the asset ID and parameters.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>Whether the asset was successfully preloaded.</returns>
        UniTask<bool> PreloadAsync(in ContainerAssetRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads multiple assets by their IDs with optional shared parameters.
        /// Only if not cached yet, performs fetch and cache operations for each asset.
        /// </summary>
        /// <param name="assetIds">The collection of asset IDs to preload.</param>
        /// <param name="parameters">Optional dictionary of shared parameters for all the operations.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>An array indicating whether each asset was successfully preloaded.</returns>
        UniTask<bool[]> PreloadAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Preloads multiple assets from a collection of requests.
        /// Only if not cached yet, performs fetch and cache operations for each asset.
        /// </summary>
        /// <param name="loadRequests">The collection of requests to preload.</param>
        /// <param name="cancellationToken">Optional cancellation token for the operation.</param>
        /// <returns>An array indicating whether each asset was successfully preloaded.</returns>
        UniTask<bool[]> PreloadAsync(IEnumerable<ContainerAssetRequest> loadRequests, CancellationToken cancellationToken = default);
    }
}
