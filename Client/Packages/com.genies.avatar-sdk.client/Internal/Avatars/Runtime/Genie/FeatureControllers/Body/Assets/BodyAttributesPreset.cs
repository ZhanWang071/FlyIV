using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BodyAttributesPreset", menuName = "Genies/Body Attributes Preset")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class BodyAttributesPreset : ScriptableObject
#else
    public sealed class BodyAttributesPreset : ScriptableObject
#endif
    {
        public List<BodyAttributeState> attributesStates = new();
    }
}
