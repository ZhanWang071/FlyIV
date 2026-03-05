namespace Genies.Models
{
    /// <summary>
    /// The various categorical types of animations available within looks
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AnimationCategory
#else
    public enum AnimationCategory
#endif
    {
        none,
        actions,
        cameras,
        dances,
        emotions,
        idles,
        music,
        poses,
        rnd,
        sports,
        things,
        wearables
    }
}