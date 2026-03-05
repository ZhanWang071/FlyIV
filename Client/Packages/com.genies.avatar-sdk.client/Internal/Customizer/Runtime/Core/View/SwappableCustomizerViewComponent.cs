using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Customization.Framework
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SwappableCustomizerViewComponent : CustomizerViewComponents
#else
    public class SwappableCustomizerViewComponent : CustomizerViewComponents
#endif
    {
        public RectTransform backSwapLayer;
        public RectTransform frontSwapLayer;
        public float inYPivot;
        [FormerlySerializedAs("outYPivot")]
        public float outTopYPivot;
        public float outBottomYPivot;
    }
}
