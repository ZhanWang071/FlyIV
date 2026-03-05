using UnityEngine;

namespace Genies.MegaEditor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum GSkelPresetGender
#else
    public enum GSkelPresetGender
#endif
    {
        None = 0,
        Female = 1,
        Male = 2,
        Androgynous = 3
    }

    #if GENIES_INTERNAL
    [CreateAssetMenu(menuName = "Genies/Chaos Mode/GSkelModifierPresetIcon", fileName = "gSkelModifierPresetIcon.asset")]
    #endif

#if GENIES_SDK && !GENIES_INTERNAL
    internal class GSkelModifierPresetIcon : ScriptableObject
#else
    public class GSkelModifierPresetIcon : ScriptableObject
#endif
    {
        public string PresetAddress;
        public Sprite Icon;
        public GSkelPresetGender FilterGender;
    }
}