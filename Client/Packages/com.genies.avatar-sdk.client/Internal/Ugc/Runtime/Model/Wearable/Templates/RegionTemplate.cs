namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class RegionTemplate
#else
    public class RegionTemplate
#endif
    {
        public int RegionNumber;
        public StyleTemplate StyleTemplate;
    }
}
