using System;

namespace Genies.Customization.Framework.Actions
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FilterOption
#else
    public class FilterOption
#endif
    {
        public string displayName;
        public Action filterApplied;
    }
}