using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using GnWrappers;

using CancellationToken = System.Threading.CancellationToken;

namespace Genies.Naf
{
    /// <summary>
    /// Utility class that encapsulates the logic of loading, equipping and unequipping combinable assets from a
    /// <see cref="NativeGenieBuilder"/>, ensuring that only one operation is executed at a time (serialized execution).
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GenieLoadAndEquipSerializer : IDisposable
#else
    public sealed class GenieLoadAndEquipSerializer : IDisposable
#endif
    {
        /// <summary>
        /// The serialized executor used internally to serialize all the operations. You can use it externally to
        /// execute any custom operation that needs to be serialized with the load and equip operations of this class.
        /// </summary>
        public readonly SerializedAsyncExecutor SerializedExecutor;

        private readonly Ref<ContainerService> _containerServiceRef;
        private readonly NativeGenieBuilder    _builder;

        public GenieLoadAndEquipSerializer(Ref<ContainerService> containerServiceRef, NativeGenieBuilder builder, bool latestOpWins = false)
        {
            if (!containerServiceRef.IsAlive)
            {
                throw new ArgumentException("ContainerService reference must be alive.", nameof(containerServiceRef));
            }

            if (builder == null)
            {
                throw new ArgumentException("NativeGenieBuilder instance must be provided.", nameof(builder));
            }

            SerializedExecutor = new SerializedAsyncExecutor(latestOpWins);

            _containerServiceRef = containerServiceRef;
            _builder             = builder;
        }

        public UniTask LoadAndEquipAsync(string assetId, Dictionary<string, string> parameters = null, CancellationToken cancellationToken = default)
        {
            var requests = new ContainerAssetRequest[] { new(assetId, parameters), };
            return SerializedExecutor.ExecuteAsync(
                token => LoadAndEquipOrSetOp(requests, token, isSetOperation: false),
                cancellationToken
            );
        }

        public UniTask LoadAndEquipAsync(IEnumerable<ContainerAssetRequest> requests, CancellationToken cancellationToken = default)
        {
            return SerializedExecutor.ExecuteAsync(
                token => LoadAndEquipOrSetOp(requests, token, isSetOperation: false),
                cancellationToken
            );
        }

        public UniTask LoadAndSetAsync(IEnumerable<ContainerAssetRequest> requests, CancellationToken cancellationToken = default)
        {
            return SerializedExecutor.ExecuteAsync(
                token => LoadAndEquipOrSetOp(requests, token, isSetOperation: true),
                cancellationToken
            );
        }

        public UniTask UnequipAsync(string assetId, CancellationToken cancellationToken = default)
        {
            return SerializedExecutor.ExecuteAsync(
                () => UnequipOp(new[] { assetId, }),
                cancellationToken
            );
        }

        public UniTask UnequipAsync(IEnumerable<string> assetIds, CancellationToken cancellationToken = default)
        {
            return SerializedExecutor.ExecuteAsync(
                () => UnequipOp(assetIds),
                cancellationToken
            );
        }

        public void Dispose()
        {
            _containerServiceRef.Dispose();
        }

        private async UniTask LoadAndEquipOrSetOp(IEnumerable<ContainerAssetRequest> requests, CancellationToken cancellationToken, bool isSetOperation)
        {
            // load the operation assets using the container service
            Entity[] assets = await _containerServiceRef.Item.CombinableAsset.LoadAsync(requests, cancellationToken);
            if (assets is null || assets.Length == 0)
            {
                return;
            }

            try
            {
                // validate that the builder is still alive
                if (_builder == null)
                {
                    throw new ObjectDisposedException(nameof(NativeGenieBuilder));
                }

                // check for cancellation before modifying the builder state, to avoid unnecessary work
                cancellationToken.ThrowIfCancellationRequested();

                if (isSetOperation)
                {
                    _builder.ClearEntities();
                }

                // equip the loaded assets and rebuild
                _builder.AddEntities(assets);
                await _builder.RebuildAsync();

                /**
                 * For simplicity, we are not implementing any rollback mechanism in case of cancellation or exceptions
                 * during the equip and rebuild process. Once the genie is rebuilt, there is no going back.
                 */
            }
            finally
            {
                // ensure we dispose our shared references to the loaded assets
                foreach (Entity asset in assets)
                {
                    asset?.Dispose();
                }
            }
        }

        private UniTask UnequipOp(IEnumerable<string> assetIds)
        {
            if (_builder == null)
            {
                throw new ObjectDisposedException(nameof(NativeGenieBuilder));
            }

            if (assetIds is null)
            {
                return UniTask.CompletedTask;
            }

            var assetIdsSet = new HashSet<string>(assetIds);
            if (assetIdsSet.Count == 0)
            {
                return UniTask.CompletedTask;
            }

            using VectorEntity entities = _builder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                if (assetIdsSet.Contains(entity.Name()))
                {
                    _builder.RemoveEntity(entity);
                }
            }

            return _builder.RebuildAsync();
        }
    }
}
