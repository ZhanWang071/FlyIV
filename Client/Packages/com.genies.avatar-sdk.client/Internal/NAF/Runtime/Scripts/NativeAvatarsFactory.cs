using Cysharp.Threading.Tasks;
using Genies.Refs;
using Genies.Utilities;
using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Genies.Naf
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class NativeAvatarsFactory
#else
    public static class NativeAvatarsFactory
#endif
    {
        public static async UniTask<NativeUnifiedGenieController> CreateUnifiedGenieAsync(
            string                definition          = null,
            Transform             parent              = null,
            IAssetParamsService   assetParamsService  = null,
            Ref<ContainerService> containerServiceRef = default
        ) {
            NativeGenieBuilder builder = CreateDefaultNativeGenieBuilder(parent);
#if UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
            builder.DisableRefitting = true;
#endif
            NativeUnifiedGenieController controller = await CreateUnifiedGenieAsync(builder, definition, assetParamsService, containerServiceRef);
            return controller;
        }

        public static async UniTask<NativeUnifiedGenieController> CreateUnifiedGenieAsync(
            NativeGenieBuilder    builder,
            string                definition          = null,
            IAssetParamsService   assetParamsService  = null,
            Ref<ContainerService> containerServiceRef = default
        ) {
            using var processSpan = new ProcessSpan(ProcessIds.CreateUnifiedGenieAsync);

            // set a no-op asset params service if none is provided
            assetParamsService ??= new NoOpAssetParamsService();

            // create the controller
            var controller = new NativeUnifiedGenieController(builder, assetParamsService, containerServiceRef);

            // set the definition if provided
            if (!string.IsNullOrWhiteSpace(definition))
            {
                await controller.SetDefinitionAsync(definition);
            }

            return controller;
        }

        public static NativeGenieBuilder CreateDefaultNativeGenieBuilder(Transform parent = null)
        {
            using var processSpan = new ProcessSpan(ProcessIds.CreateDefaultNativeGenieBuilder);

            var prefab = Resources.Load<NativeGenieBuilder>("NativeGenie");
            if (!prefab)
            {
                Debug.LogError($"[{nameof(NativeAvatarsFactory)}] could not find {nameof(NativeGenieBuilder)} prefab in Resources.");
                return null;
            }

            NativeGenieBuilder genie = Object.Instantiate(prefab, parent);
            if (!genie)
            {
                return null;
            }

            return genie;
        }
    }
}
