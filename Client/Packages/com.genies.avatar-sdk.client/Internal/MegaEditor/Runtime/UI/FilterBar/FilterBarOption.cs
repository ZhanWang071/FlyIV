using System;

namespace Genies.Customization.MegaEditor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FilterBarOption
#else
    public class FilterBarOption
#endif
    {
        public string displayName;
        public string filterId;
        public int countDisplay;
        public Action onClicked;
    }
}
