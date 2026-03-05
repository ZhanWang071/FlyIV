using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "BlendShapesList", menuName = "Genies/BlendShapesList")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BlendShapesList : ScriptableObject {
#else
    public class BlendShapesList : ScriptableObject {
#endif
        public List<BlendShapeDataContainer> BlendShapeContainers;
    }
}
