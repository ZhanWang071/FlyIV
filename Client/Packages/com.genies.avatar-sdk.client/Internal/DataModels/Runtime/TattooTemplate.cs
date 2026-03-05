using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TattooTemplate", menuName = "Genies/Tattoos/TattooTemplate")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TattooTemplate : DecoratedSkinTemplate
#else
    public class TattooTemplate : DecoratedSkinTemplate
#endif
    {
        public TattooCategory category;
    }
}
