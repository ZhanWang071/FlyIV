using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "StickersTemplate", menuName = "Genies/Makeup/StickersTemplate")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class StickersTemplate : MakeupTemplate, IDynamicAsset
#else
    public class StickersTemplate : MakeupTemplate, IDynamicAsset
#endif
    {
        public override MakeupPresetCategory Category => MakeupPresetCategory.Stickers;
    }
}
