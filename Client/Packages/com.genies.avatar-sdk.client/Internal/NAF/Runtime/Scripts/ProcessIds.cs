using Genies.Utilities;

namespace Genies.Naf
{
    internal static class ProcessIds
    {
        // AvatarDefinitionLoader
        public static ProcessId LoadAssetsAsync = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.LoadAssetsAsync)}");
        public static ProcessId LoadAssetsAsyncCombinables = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.LoadAssetsAsync)}::Combinables");
        public static ProcessId LoadAssetsAsyncTattoos = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.LoadAssetsAsync)}::Tattoos");
        public static ProcessId PreloadAssetsAsync = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.PreloadAssetsAsync)}");
        public static ProcessId PreloadAssetsAsyncCombinables = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.PreloadAssetsAsync)}::Combinables");
        public static ProcessId PreloadAssetsAsyncTattoos = ProcessSpan.CreateId($"{nameof(AvatarDefinitionAssetLoader)}.{nameof(AvatarDefinitionAssetLoader.PreloadAssetsAsync)}::Tattoos");

        // NativeAvatarsFactory
        public static ProcessId CreateDefaultNativeGenieBuilder = ProcessSpan.CreateId($"{nameof(NativeAvatarsFactory)}.{nameof(NativeAvatarsFactory.CreateDefaultNativeGenieBuilder)}");
        public static ProcessId CreateUnifiedGenieAsync = ProcessSpan.CreateId($"{nameof(NativeAvatarsFactory)}.{nameof(NativeAvatarsFactory.CreateUnifiedGenieAsync)}");

        // NativeUnifiedGenieController
        public static ProcessId SetDefinitionAsync = ProcessSpan.CreateId($"{nameof(NativeUnifiedGenieController)}.{nameof(NativeUnifiedGenieController.SetDefinitionAsync)}");
        public static ProcessId SetDefinitionAsyncSetColor = ProcessSpan.CreateId($"{nameof(NativeUnifiedGenieController)}.{nameof(NativeUnifiedGenieController.SetDefinitionAsync)}::SetColor");
        public static ProcessId SetDefinitionAsyncSetBodyAttr = ProcessSpan.CreateId($"{nameof(NativeUnifiedGenieController)}.{nameof(NativeUnifiedGenieController.SetDefinitionAsync)}::SetBodyAttributes");
        public static ProcessId SetDefinitionAsyncAssetsAndTattoos = ProcessSpan.CreateId($"{nameof(NativeUnifiedGenieController)}.{nameof(NativeUnifiedGenieController.SetDefinitionAsync)}::AssetsAndTattoos");
        public static ProcessId SetDefinitionAsyncRebuildAvatar = ProcessSpan.CreateId($"{nameof(NativeUnifiedGenieController)}.{nameof(NativeUnifiedGenieController.SetDefinitionAsync)}::RebuildAvatar");
    }
}
