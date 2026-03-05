namespace UMA
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface INameProvider
#else
    public interface INameProvider
#endif
    {
        string GetAssetName();
        int GetNameHash();
    }
}
