namespace Genies.PerformanceMonitoring
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class CustomInstrumentationOperations
#else
    public static class CustomInstrumentationOperations
#endif
    {
        public const string LoadAvatarTransaction = "load_avatar";
        public const string LoadBakedAvatarTransaction = "load_baked_avatar";
        public const string CreateNewLookTransaction = "create_new_look";

        // app startup time
        public const string AppStartupTransaction = "App Startup";
        public const string AppStartupLoginSpan = "Login";
        public const string AppStartupPostLoinSpan = "Post Login";
        public const string AppStartupPreloadSpan = "Preload Assets";
    }
}
