using System.Collections.Generic;

namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class WearableTemplate
#else
    public class WearableTemplate
#endif
    {
        public string TemplateId;
        public List<SplitTemplate> SplitTemplates;
        public bool IsBasic;
    }
}
