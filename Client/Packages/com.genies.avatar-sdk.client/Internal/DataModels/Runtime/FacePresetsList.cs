using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models {
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "FacePresetsList", menuName = "Genies/FacePresetsList")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FacePresetsList : ScriptableObject {
#else
    public class FacePresetsList : ScriptableObject {
#endif
        public List<BlendshapeDataFacePresetContainer> FacePresetContainers;
    }
}
