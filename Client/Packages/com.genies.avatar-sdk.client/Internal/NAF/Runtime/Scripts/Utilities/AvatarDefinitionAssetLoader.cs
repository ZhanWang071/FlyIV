using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Utilities;
using GnWrappers;
using UnityEngine;

using CancellationToken = System.Threading.CancellationToken;
using Texture = GnWrappers.Texture;

namespace Genies.Naf
{
    /// <summary>
    /// Utility class for loading and caching avatar definition assets.
    /// Provides common functionality for both precaching and loading avatar assets.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class AvatarDefinitionAssetLoader : IDisposable
#else
    public sealed class AvatarDefinitionAssetLoader : IDisposable
#endif
    {
        public struct Assets : IDisposable
        {
            public Entity[]                                    Entities;
            public (MegaSkinTattooSlot slot, Texture tattoo)[] Tattoos;

            public void Dispose()
            {
                if (Entities is not null)
                {
                    foreach (Entity asset in Entities)
                    {
                        asset?.Dispose();
                    }
                }

                if (Tattoos is not null)
                {
                    foreach ((MegaSkinTattooSlot slot, Texture tattoo) pair in Tattoos)
                    {
                        pair.tattoo?.Dispose();
                    }
                }
            }
        }

        private readonly IAssetParamsService   _assetParamsService;
        private readonly Ref<ContainerService> _containerServiceRef;

        public AvatarDefinitionAssetLoader(IAssetParamsService assetParamsService, Ref<ContainerService> containerServiceRef = default)
        {
            _assetParamsService  = assetParamsService ?? throw new ArgumentNullException(nameof(assetParamsService));
            _containerServiceRef = containerServiceRef;

            if (!containerServiceRef.IsAlive)
            {
                _containerServiceRef = CreateRef.FromDisposable(new ContainerService());
            }
        }

        /// <summary>
        /// Fetches, caches, and loads all assets (combinable assets and tattoos) for an avatar definition into memory.
        /// Fetch and cache are performed if assets are not already cached on disk.
        /// </summary>
        /// <param name="definition">The avatar definition containing asset IDs to load.</param>
        /// <returns>A tuple containing loaded entities and tattoos.</returns>
        public async UniTask<Assets> LoadDisposableAssetsAsync(AvatarDefinition definition, CancellationToken cancellationToken = default)
        {
            (Entity[] assets, (MegaSkinTattooSlot slot, Texture tattoo)[] tattoos) = await LoadAssetsAsync(definition, cancellationToken);
            return new Assets
            {
                Entities = assets,
                Tattoos  = tattoos
            };
        }

        /// <summary>
        /// Fetches, caches, and loads all assets (combinable assets and tattoos) for an avatar definition into memory.
        /// Fetch and cache are performed if assets are not already cached on disk.
        /// </summary>
        /// <param name="definition">The avatar definition containing asset IDs to load.</param>
        /// <returns>A tuple containing loaded entities and tattoos.</returns>
        public async UniTask<(Entity[] assets, (MegaSkinTattooSlot slot, Texture tattoo)[] tattoos)> LoadAssetsAsync(AvatarDefinition definition, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            if (definition is null)
            {
                return (null, null);
            }

            using var processSpan = new ProcessSpan(ProcessIds.LoadAssetsAsync);

            var assetsTask = UniTask.FromResult<Entity[]>(null);
            var tattoosTask = UniTask.FromResult<(MegaSkinTattooSlot slot, Texture tattoo)[]>(null);

            if (definition.equippedAssetIds is { Count: > 0 })
            {
                assetsTask = UniTask.WhenAll(definition.equippedAssetIds.Select(
                    assetId => LoadCombinableAssetAsync(assetId, cancellationToken, processSpan)
                ));
            }

            if (definition.equippedTattooIds is { Count: > 0 })
            {
                tattoosTask = UniTask.WhenAll(definition.equippedTattooIds.Select(
                    pair => LoadTattooAsync(pair, cancellationToken, processSpan)
                ));
            }

            var result = await UniTask.WhenAll(assetsTask, tattoosTask);

            /**
             * If cancelalation was requested, it is possible that some assets where loaded before the cancellation was
             * observed. Dispose any loaded assets to prevent memory leaks before throwing. All the load methods were
             * prepared to avoid throwing any exceptions so we can safely disposed the loaded assets here.
             */
            if (cancellationToken.IsCancellationRequested)
            {
                if (result.Item1 is not null)
                {
                    foreach (Entity asset in result.Item1)
                    {
                        asset?.Dispose();
                    }
                }

                if (result.Item2 is not null)
                {
                    foreach ((MegaSkinTattooSlot slot, Texture tattoo) pair in result.Item2)
                    {
                        pair.tattoo?.Dispose();
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            return result;
        }

        /// <summary>
        /// Fetches and caches assets for an avatar definition. This is useful for pre-caching assets to improve future
        /// load times without keeping them in memory.
        /// </summary>
        /// <param name="definition">The avatar definition containing asset IDs to preload.</param>
        public UniTask PreloadAssetsAsync(AvatarDefinition definition, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            cancellationToken.ThrowIfCancellationRequested();

            if (definition is null)
            {
                return UniTask.CompletedTask;
            }

            using var processSpan = new ProcessSpan(ProcessIds.PreloadAssetsAsync);

            var assetsTask = UniTask.CompletedTask;
            var tattoosTask = UniTask.CompletedTask;

            if (definition.equippedAssetIds is { Count: > 0 })
            {
                assetsTask = UniTask.WhenAll(definition.equippedAssetIds.Select(
                    assetId => PreloadCombinableAssetAsync(assetId, cancellationToken, processSpan)
                ));
            }

            if (definition.equippedTattooIds is { Count: > 0 })
            {
                tattoosTask = UniTask.WhenAll(definition.equippedTattooIds.Select(
                    pair => PreloadTattooAsync(pair.Value, cancellationToken, processSpan)
                ));
            }

            return UniTask.WhenAll(assetsTask, tattoosTask);
        }

        public void Dispose()
        {
            _containerServiceRef.Dispose();
        }

        private async UniTask<Entity> LoadCombinableAssetAsync(
            string assetId, CancellationToken cancellationToken, ProcessSpan? processSpanParent
        ) {
            // ensure that this method doesn't throw so we can safely dispose all loaded assets later if cancelled
            try
            {
                using var processSpan = new ProcessSpan(ProcessIds.LoadAssetsAsyncCombinables, processSpanParent);

                Dictionary<string, string> parameters = await _assetParamsService.FetchParamsAsync(assetId);
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();
                return await _containerServiceRef.Item.CombinableAsset.LoadAsync(assetId, parameters, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception thrown while combinable asset with ID '{assetId}': {exception}");
                return null;
            }
        }

        private async UniTask<(MegaSkinTattooSlot slot, Texture tattoo)> LoadTattooAsync(
            KeyValuePair<MegaSkinTattooSlot, string> pair, CancellationToken cancellationToken, ProcessSpan? processSpanParent
        ) {
            try
            {
                using var processSpan = new ProcessSpan(ProcessIds.LoadAssetsAsyncTattoos, processSpanParent);

                Dictionary<string, string> parameters = await _assetParamsService.FetchParamsAsync(pair.Value);
                ThrowIfDisposed();
                cancellationToken.ThrowIfCancellationRequested();
                Texture tattoo = await _containerServiceRef.Item.Texture.LoadAsync(pair.Value, parameters, cancellationToken);
                return (pair.Key, tattoo);
            }
            catch (OperationCanceledException)
            {
                return (pair.Key, null);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception thrown while loading tattoo texture with ID '{pair.Value}': {exception}");
                return (pair.Key, null);
            }
        }

        private async UniTask PreloadCombinableAssetAsync(
            string assetId, CancellationToken cancellationToken, ProcessSpan? processSpanParent
        ) {
            using var processSpan = new ProcessSpan(ProcessIds.PreloadAssetsAsyncCombinables, processSpanParent);

            Dictionary<string, string> parameters = await _assetParamsService.FetchParamsAsync(assetId);
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            await _containerServiceRef.Item.CombinableAsset.PreloadAsync(assetId, parameters, cancellationToken);
        }

        private async UniTask PreloadTattooAsync(
            string assetId, CancellationToken cancellationToken, ProcessSpan? processSpanParent
        ) {
            using var processSpan = new ProcessSpan(ProcessIds.PreloadAssetsAsyncTattoos, processSpanParent);

            Dictionary<string, string> parameters = await _assetParamsService.FetchParamsAsync(assetId);
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            await _containerServiceRef.Item.Texture.LoadAsync(assetId, parameters, cancellationToken);
        }

        private void ThrowIfDisposed()
        {
            if (!_containerServiceRef.IsAlive)
            {
                throw new ObjectDisposedException(nameof(AvatarDefinitionAssetLoader));
            }
        }
    }
}
