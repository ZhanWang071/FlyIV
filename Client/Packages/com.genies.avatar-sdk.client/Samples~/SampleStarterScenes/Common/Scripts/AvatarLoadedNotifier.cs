using System;

namespace Genies.Sdk.Samples.Common
{
    public static class AvatarLoadedNotifier
    {
        public static event Action<ManagedAvatar> Loaded;
        public static event Action<ManagedAvatar> Destroyed;

        public static void InvokeLoaded(ManagedAvatar avatar) => Loaded?.Invoke(avatar);
        public static void InvokeDestroyed(ManagedAvatar avatar) => Destroyed?.Invoke(avatar);
    }
}