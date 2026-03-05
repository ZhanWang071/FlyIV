using Genies.Utilities;

namespace Genies.Avatars.Sdk
{
    internal static  class ProcessIds
    {
        // GeniesAvatarSdk
        public static ProcessId LoadUserAvatarController = ProcessSpan.CreateId($"{nameof(GeniesAvatarsSdk)}.{nameof(GeniesAvatarsSdk.LoadUserAvatarController)}");

        // GeniesAvatarLoader
        public static ProcessId SetupAvatarAndControllers = ProcessSpan.CreateId($"{nameof(GeniesAvatarLoader)}.{nameof(GeniesAvatarLoader.SetupAvatarAndControllers)}");

        // GeniesAvatarSdkService
        public static ProcessId CreateAvatarAsync = ProcessSpan.CreateId($"{nameof(GeniesAvatarSdkService)}.{nameof(IGeniesAvatarSdkService.CreateAvatarAsync)}");
        public static ProcessId GetMyAvatarDefinition = ProcessSpan.CreateId($"{nameof(GeniesAvatarSdkService)}.{nameof(GeniesAvatarSdkService.GetMyAvatarDefinition)}");
        public static ProcessId GetMyAvatarDefinitionFetch = ProcessSpan.CreateId($"{nameof(GeniesAvatarSdkService)}.{nameof(GeniesAvatarSdkService.GetMyAvatarDefinition)}::Fetch");
    }
}
