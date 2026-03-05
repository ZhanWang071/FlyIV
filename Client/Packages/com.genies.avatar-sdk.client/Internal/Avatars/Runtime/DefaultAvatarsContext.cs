namespace Genies.Avatars
{
    /// <summary>
    /// Use the <see cref="Instance"/> field to set a default <see cref="AvatarsContext"/> that will be used
    /// by the <see cref="AvatarsFactory"/> when no context is provided.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class DefaultAvatarsContext
#else
    public static class DefaultAvatarsContext
#endif
    {
        public static AvatarsContext Instance;
    }
}
