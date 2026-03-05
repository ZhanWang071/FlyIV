using UnityEngine;

namespace Genies.UIFramework.Widgets {
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal class TweenSpinner : MonoBehaviour {
#else
    public class TweenSpinner : MonoBehaviour {
#endif

        [Header("Options")]
        public float degreesPerSecond = -500f;

        private void Update() {
            float angle = Time.time * degreesPerSecond;

            if (angle > 180)
            {
                angle -= 360f;
            }

            if (angle < 180)
            {
                angle += 360f;
            }

            transform.rotation = Quaternion.Euler(0f,0f,angle);
        }
    }
}
