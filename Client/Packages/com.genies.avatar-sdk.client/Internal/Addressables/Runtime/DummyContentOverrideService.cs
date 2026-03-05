namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DummyContentOverrideService : IContentOverrideService
#else
    public class DummyContentOverrideService : IContentOverrideService
#endif
    {
        public string GetOverrideUrl(string fallback)
        {
            return fallback;
        }
    }
}
