namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum AnimationAssetType
#else
    public enum AnimationAssetType
#endif
    {
        None = 0,
        SpacesIdle = 1,
        BehaviorAnim = 2,
        GeniesCameraEmote = 3,
    }
}
