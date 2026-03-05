using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.MaterialData
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataMakeupColor", menuName = "Genies/MaterialData/MaterialDataMakeupColor")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MaterialDataMakeupColor : MaterialDataIconColor
#else
    public class MaterialDataMakeupColor : MaterialDataIconColor
#endif
    {
        public Color IconColor2;
        public Color IconColor3;
        public MakeupPresetCategory makeupPresetCategory;
    }
}
