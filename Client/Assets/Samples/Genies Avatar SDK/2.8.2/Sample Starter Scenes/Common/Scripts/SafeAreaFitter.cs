using UnityEngine;

namespace Genies.Sdk.Samples.Common
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaFitter : MonoBehaviour
    {
        private void Awake() => Apply();

        private void Apply()
        {
            var panel = GetComponent<RectTransform>();
            var area = Screen.safeArea;

            Vector2 anchorMin = area.position;
            Vector2 anchorMax = area.position + area.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            panel.anchorMin = anchorMin;
            panel.anchorMax = anchorMax;
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
        }
    }

}
