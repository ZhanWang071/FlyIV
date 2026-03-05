using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.UI.Scroller
{
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class ScrollableScaleAnimator : MonoBehaviour, IScrollableAnimator
#else
    public class ScrollableScaleAnimator : MonoBehaviour, IScrollableAnimator
#endif
    {
        [FormerlySerializedAs("curve")] public AnimationCurve Curve;

        public void Animate(float normalizedValue)
        {
            var scale = Curve.Evaluate(normalizedValue);
            transform.localScale = Vector3.one * scale;
        }
    }
}
