namespace Genies.Analytics
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class NoOpAnalyticsService : IAnalyticsService
#else
    public sealed class NoOpAnalyticsService : IAnalyticsService
#endif
    {
        public void LogEvent(string eventName) { }
        public void LogEvent(string eventName, AnalyticProperties properties) { }
        public void SetUserProperty(string propertyName, string value) { }
        public void SetIdentity(IdentityData identityData) { }
        public void IncrementProfileProperty(string key, int incrementValue) { }
    }
}
