using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.UniversalResourceLocation;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Genies.Addressables.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UniversalContentResourceProvider : ResourceProviderBase
#else
    public class UniversalContentResourceProvider : ResourceProviderBase
#endif
    {
        private const string _providerSuffix = "universal_content";
        public static string CustomProviderId => $"{typeof(UniversalContentResourceProvider).FullName}{_providerSuffix}";
        public override string ProviderId => CustomProviderId;

        private readonly ICustomResourceProvider _customResourceProvider = ServiceManager.GetService<ICustomResourceProvider>(null);
        private readonly Dictionary<object, Ref> _assetRefs = new();

        public override void Provide(ProvideHandle handle)
        {
            IResourceLocation location = handle.Location;
            Type requestedType = handle.Type;

            var internalId = location.InternalId;
            var primaryKey = location.PrimaryKey;

            try
            {
                if (requestedType == typeof(Sprite))
                {
                    // fire native calls once they complete handle.Complete will be called for each call!
                    _ = ProvideInternalAsync(handle, primaryKey);
                }
                else
                {
                    handle.Complete<object>(default, false, new NotSupportedException($"Unsupported Type {requestedType}"));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load asset from {internalId}: {ex}");
            }
        }

        public override void Release(IResourceLocation location, object obj)
        {
            if (obj is null || !_assetRefs.TryGetValue(obj, out Ref assetRef))
            {
                return;
            }

            _assetRefs.Remove(obj);
            assetRef.Dispose();
        }

        // Move async to its own internal method to keep Provide synchronous
        private async UniTask ProvideInternalAsync(ProvideHandle handle, string internalId)
        {
            try
            {
                Ref<Sprite> sprite = await _customResourceProvider.Provide(internalId);

                if (!sprite.IsAlive || sprite.Item == null)
                {
                    // Complete with failure so Addressables doesn't try to track a null item
                    handle.Complete<Sprite>(
                        default,
                        false,
                        new Exception($"Sprite provider returned null for '{internalId}'."));
                    return;
                }

                /*
                 * The Addressables system only expects the sprite, and will later call our Release method to release
                 * the resource. We rely on the Refs system so we will store the ref in a dictionary so we can dispose
                 * it on the release later.
                 */
                SaveAssetRefForRelease(sprite);

                handle.Complete(sprite.Item, true, null);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load asset internal {internalId}: {ex}");

                // Always complete the handle, even on failure
                handle.Complete<Sprite>(default, false, ex);
            }
        }

        private void SaveAssetRefForRelease<T>(Ref<T> assetRef)
        {
            if (!assetRef.IsAlive || assetRef.Item is null)
            {
                return;
            }

            // this should never happen, but just in case
            if (_assetRefs.TryGetValue(assetRef.Item, out Ref existingRef))
            {
                existingRef.Dispose();
                Debug.LogError("Trying to save an already saved asset ref!");
            }

            _assetRefs[assetRef.Item] = assetRef;
        }
    }
}
