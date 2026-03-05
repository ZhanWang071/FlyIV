using Genies.Utilities;

namespace Genies.Sdk
{
    internal sealed partial class CoreSdk
    {
        internal static class ProcessIds
        {
            // Core methods
            public static ProcessId ProcessIdInitialize = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(InitializeAsync)}");

            // Avatar methods
            public static ProcessId ProcessIdLoadDefaultAvatar = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.LoadDefaultAvatarAsync)}");
            public static ProcessId ProcessIdLoadUserAvatar = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.LoadUserAvatarAsync)}");
            public static ProcessId ProcessIdLoadAvatarByDefinition = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.LoadAvatarByDefinitionAsync)}");
            public static ProcessId ProcessIdGetUserAvatarDefinition = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.GetUserAvatarDefinition)}");
            public static ProcessId ProcessIdPrecacheUserAvatarAssets = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.PrecacheUserAvatarAssetsAsync)}");
            public static ProcessId ProcessIdPrecacheDefaultAvatarAssets = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.PrecacheDefaultAvatarAssetsAsync)}");
            public static ProcessId ProcessIdPrecacheAvatarAssetsByDefinition = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Avatar)}.{nameof(Avatar.PrecacheAvatarAssetsByDefinitionAsync)}");

            // Login methods
            public static ProcessId ProcessIdTryInstantLogin = ProcessSpan.CreateId($"{nameof(CoreSdk)}.{nameof(Login)}.{nameof(Login.TryInstantLoginAsync)}");
        }
    }
}
