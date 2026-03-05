namespace Genies.Addressables
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IContentOverrideService
#else
    public interface IContentOverrideService
#endif
    {
        public string GetOverrideUrl(string fallback);
    }
}
