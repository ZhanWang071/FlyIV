using UnityEngine;

#if UNITY_EDITOR
#endif
namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MaterialDataIconColor", menuName = "Genies/MaterialData/MaterialDataIconColor")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class MaterialDataIconColor : MaterialDataContainer
#else
    public class MaterialDataIconColor : MaterialDataContainer
#endif
    {
        public Color IconColor;
        public string TargetContainerType;
    }
}
