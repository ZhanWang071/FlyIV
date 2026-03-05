using System.Collections.Generic;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class BlendshapeDataFacePresetContainer : OrderedScriptableObject
#else
    public class BlendshapeDataFacePresetContainer : OrderedScriptableObject
#endif
    {
        public Texture2D maleIcon;
        public Texture2D femaleIcon;
        public Texture2D unifiedIcon;
        public string facePresetIdentifier;
        public List<string> blendShapeIds = new();
    }
}
