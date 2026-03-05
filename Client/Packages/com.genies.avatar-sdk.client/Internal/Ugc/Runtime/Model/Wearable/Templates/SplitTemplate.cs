using System.Collections.Generic;

namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SplitTemplate
#else
    public class SplitTemplate
#endif
    {
        public string MaterialVersion;
        public readonly List<string> ElementIds;
        public Dictionary<string, List<RegionTemplate>> ElementRegionTemplates;

        private readonly HashSet<string> _elementIds;

        public SplitTemplate(List<string> elementIds)
        {
            ElementIds = elementIds;
            _elementIds = new HashSet<string>(elementIds);
        }

        public bool IsElementIdAvailable(string elementId)
            => _elementIds.Contains(elementId);
    }
}
