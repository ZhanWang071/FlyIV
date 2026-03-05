using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.UniversalResourceLocation;
using Genies.CrashReporting;
using Genies.Naf.Content;
using Genies.Refs;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Naf.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class NafContentResourceProvider : ICustomResourceProvider, IDisposable
#else
    public class NafContentResourceProvider : ICustomResourceProvider, IDisposable
#endif
    {
        private ContainerService ContainerService { get; }

        private IAssetParamsService _assetParamsService = ServiceManager.GetService<IAssetParamsService>(null);
        private IAssetIdConverter _idConverter = ServiceManager.GetService<IAssetIdConverter>(null);

        private bool IsDisposed { get; set; }

        public NafContentResourceProvider(NafAssetResolverConfig resolverConfig)
        {
            // NAF plugin initialization is required for asset resolvers
            if (NafPlugin.IsInitialized is false)
            {
                NafPlugin.Initialize();
            }

            ContainerService = new ContainerService(resolverConfig);
        }

        ~NafContentResourceProvider()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }
            IsDisposed = true;

            ContainerService?.Dispose();
        }

        public async UniTask<Ref<Sprite>> Provide(string internalId)
        {
            if (IsDisposed) { return default; }

            var convertedId = await _idConverter.ConvertToUniversalIdAsync(internalId);
            var parameters = await _assetParamsService.FetchParamsAsync(internalId);
            parameters["lod"] = "0"; // Always load LOD 0 for icons
            Ref<Sprite> iconRef = await LoadIconAsyncInternal(convertedId, parameters);
            return iconRef;
        }

        private async UniTask<Ref<Sprite>> LoadIconAsyncInternal(string assetId, Dictionary<string, string> parameters = null)
        {
            using GnWrappers.Texture texture = await ContainerService.Icon.LoadAsync(assetId, parameters);

            if (texture == null || texture.IsNull())
            {
                return default; // Failed to load
            }

            Ref<UnityEngine.Texture> refTex = texture.AsUnityTexture();
            Sprite sprite = CreateFromTexture(refTex.Item as UnityEngine.Texture2D);

            Ref<Sprite> spriteRef = CreateRef.FromUnityObject(sprite);
            spriteRef = CreateRef.FromDependentResource(spriteRef, refTex);

            return spriteRef;
        }

        private Sprite CreateFromTexture(Texture2D texture)
        {
            if (texture == null)
            {
                CrashReporter.LogInternal("Creating default texture for missing icon!");
                texture = Texture2D.grayTexture;
            }

            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        }
    }
}
