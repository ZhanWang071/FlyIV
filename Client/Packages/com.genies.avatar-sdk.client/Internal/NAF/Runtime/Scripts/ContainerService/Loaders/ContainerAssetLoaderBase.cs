using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using GnWrappers;
using UnityEngine;

using CancellationToken = System.Threading.CancellationToken;
using NativeCancellationToken = GnWrappers.CancellationToken;

namespace Genies.Naf
{
    /// <summary>
    /// Base implementation of the <see cref="IContainerAssetLoader{T}"/> interface that handles the common logic of
    /// invoking the native Container API. It weakly references a Container API instance using a <see cref="Handle{ContainerApi}"/>
    /// that a higher level service is responsible for managing. As soon as the Container API instance is disposed, all
    /// in-flight load and preload operations will be cancelled and this loader will refuse to start new operations,
    /// throwing an exception if attempted.
    /// </summary>
    /// <typeparam name="T">The type of asset to load.</typeparam>
#if GENIES_SDK && !GENIES_INTERNAL
    internal abstract class ContainerAssetLoaderBase<T> : IContainerAssetLoader<T>
#else
    public abstract class ContainerAssetLoaderBase<T> : IContainerAssetLoader<T>
#endif
    {
        protected ContainerApi ContainerApi => _containerApi.Resource;

        private readonly Handle<ContainerApi> _containerApi;

        protected ContainerAssetLoaderBase(Handle<ContainerApi> containerApi)
        {
            _containerApi = containerApi;
        }

        protected abstract Future     LoadAsyncInternal   (string assetId, UnorderedMapString cParams, NativeCancellationToken cancellationToken);
        protected abstract BoolFuture PreloadAsyncInternal(string assetId, UnorderedMapString cParams, NativeCancellationToken cancellationToken);
        protected abstract T          GetResult           (Future future);

        public UniTask<T> LoadAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return LoadAsync(assetId, cParams, cancellationToken);
        }

        public UniTask<T> LoadAsync(in ContainerAssetRequest request, CancellationToken cancellationToken = default)
        {
            return LoadAsync(request.assetId, request.parameters, cancellationToken);
        }

        public async UniTask<T[]> LoadAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            if (assetIds is null)
            {
                return Array.Empty<T>();
            }

            var tasks = new List<UniTask<T>>(32);
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            foreach (string assetId in assetIds)
            {
                tasks.Add(LoadAsync(assetId, cParams, cancellationToken));
            }

            // when there is only one task, we can propagate any exceptions to the caller as if we were using the single load method
            var results = new T[tasks.Count];
            if (tasks.Count == 1)
            {
                results[0] = await tasks[0];
                return results;
            }

            /**
             * If we used UniTask.WhenAll directly here, if any of the load operations throws an exception, the whole
             * batch would fail and we wouldn't get the results of the successfully loaded assets. Also, in most cases,
             * the loaded assets must be explicitly disposed by the caller which means that successfully loaded assets
             * would be leaked if we threw an exception due to any failed load in the batch.
             */
            for (int i = 0; i < tasks.Count; ++i)
            {
                try
                {
                    results[i] = await tasks[i];
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                    results[i] = default;
                }
            }

            return results;
        }

        public async UniTask<T[]> LoadAsync(IEnumerable<ContainerAssetRequest> loadRequests, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            if (loadRequests is null)
            {
                return Array.Empty<T>();
            }

            var tasks = new List<UniTask<T>>(32);
            foreach (ContainerAssetRequest request in loadRequests)
            {
                using UnorderedMapString cParams = request.parameters.AsUnorderedMapString();
                tasks.Add(LoadAsync(request.assetId, cParams, cancellationToken));
            }

            // when there is only one task, we can propagate any exceptions to the caller as if we were using the single load method
            var results = new T[tasks.Count];
            if (tasks.Count == 1)
            {
                results[0] = await tasks[0];
                return results;
            }

            /**
             * If we used UniTask.WhenAll directly here, if any of the load operations throws an exception, the whole
             * batch would fail and we wouldn't get the results of the successfully loaded assets. Also, in most cases,
             * the loaded assets must be explicitly disposed by the caller which means that successfully loaded assets
             * would be leaked if we threw an exception due to any failed load in the batch.
             */
            for (int i = 0; i < tasks.Count; ++i)
            {
                try
                {
                    results[i] = await tasks[i];
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                    results[i] = default;
                }
            }

            return results;
        }

        public UniTask<bool> PreloadAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            return PreloadAsync(assetId, cParams, cancellationToken);
        }

        public UniTask<bool> PreloadAsync(in ContainerAssetRequest request, CancellationToken cancellationToken = default)
        {
            return PreloadAsync(request.assetId, request.parameters, cancellationToken);
        }

        public UniTask<bool[]> PreloadAsync(IEnumerable<string> assetIds, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            if (assetIds is null)
            {
                return UniTask.FromResult(Array.Empty<bool>());
            }

            var tasks = new List<UniTask<bool>>(32);
            using UnorderedMapString cParams = parameters.AsUnorderedMapString();
            foreach (string assetId in assetIds)
            {
                tasks.Add(PreloadAsync(assetId, cParams, cancellationToken));
            }

            return UniTask.WhenAll(tasks);
        }

        public UniTask<bool[]> PreloadAsync(IEnumerable<ContainerAssetRequest> loadRequests, CancellationToken cancellationToken = default)
        {
            AssertIsNotDisposed();

            if (loadRequests is null)
            {
                return UniTask.FromResult(Array.Empty<bool>());
            }

            var tasks = new List<UniTask<bool>>(32);
            foreach (ContainerAssetRequest request in loadRequests)
            {
                using UnorderedMapString cParams = request.parameters.AsUnorderedMapString();
                tasks.Add(PreloadAsync(request.assetId, cParams, cancellationToken));
            }

            return UniTask.WhenAll(tasks);
        }

        private UniTask<T> LoadAsync(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return PerformOperationAsync(assetId, cParams, cancellationToken, LoadAsyncInternal, GetResult);
        }

        private UniTask<bool> PreloadAsync(string assetId, UnorderedMapString cParams, CancellationToken cancellationToken)
        {
            return PerformOperationAsync(assetId, cParams, cancellationToken, PreloadAsyncInternal, GetBoolFutureResult);
        }

        /**
         * Performs the common logic of invoking a Container API operation, awaiting its completion, and returning its
         * result while properly handling cancellation and disposal of the Container API instance.
         */
        private async UniTask<Result> PerformOperationAsync<Result>(
            string               assetId,
            UnorderedMapString   cParams,
            CancellationToken    cancellationToken,
            NativeLoadAsyncFunc  nativeLoadAsync,
            Func<Future, Result> getFutureResult
        ) {
            if (string.IsNullOrWhiteSpace(assetId))
            {
                return default;
            }

            // link the provided cancellation token with the application exit token
             using var linkedCs = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, Application.exitCancellationToken);
             cancellationToken = linkedCs.Token;

            // create a native cancellation token for the native load operation and bind it to the .NET token
            using var nativeCancellationToken = new NativeCancellationToken();
            await using CancellationTokenRegistration reg = cancellationToken.Register(nativeCancellationToken.Cancel);

            // ensure that the future is cancelled if the container API is disposed while the load operation is in-flight
            void HandleDispose(ContainerApi containerApi) => nativeCancellationToken.Cancel();
            _containerApi.Releasing += HandleDispose;

            try
            {
                // invoke the native load operation and await for its completion or cancellation
                using Future future = nativeLoadAsync(assetId, cParams, nativeCancellationToken);
                await future.WaitAsync(cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                // if the native token was cancelled at this point, it means the container API was disposed
                if (nativeCancellationToken.IsCancelled())
                {
                    throw new OperationCanceledException("The Container API instance was disposed while the operation was in-flight");
                }

                // return the future result. This call propagates exceptions thrown during the operation, if any
                return getFutureResult(future);
            }
            finally
            {
                if (_containerApi.IsAlive)
                {
                    // clean up the container API dispose handler
                    _containerApi.Releasing -= HandleDispose;
                }
            }
        }

        private void AssertIsNotDisposed()
        {
            if (!_containerApi.IsAlive)
            {
                throw new ObjectDisposedException(nameof(ContainerAssetLoaderBase<T>), "The Container API instance has been disposed");
            }
        }

        private static bool GetBoolFutureResult(Future future)
        {
            return ((BoolFuture)future).Get();
        }

        private delegate Future NativeLoadAsyncFunc(string assetId, UnorderedMapString cParams, NativeCancellationToken cancellationToken);
    }
}
