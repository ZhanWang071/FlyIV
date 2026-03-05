
namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StyleTemplate
#else
    public class StyleTemplate
#endif
    {
        public string MaterialVersion;
        public ICategorizedItems<string> MaterialIds;
        public PatternTemplate PatternTemplate;
    }
}
