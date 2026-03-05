namespace Genies.Avatars.Services
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class AvatarServiceEvents
#else
    public class AvatarServiceEvents
#endif
    {
        //Events for AvatarService
        public static readonly string SyncAvatarCloudDefinition = "SyncAvatarCloudDefinition";
    }
}
