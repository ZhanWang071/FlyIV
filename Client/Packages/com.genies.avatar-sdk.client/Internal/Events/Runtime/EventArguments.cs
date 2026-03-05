namespace Genies.Events
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SceneLoadRequestedEventArguments
#else
    public struct SceneLoadRequestedEventArguments
#endif
    {
        public int currentSceneIndex;
        public int requestedSceneIndex;
    }
}