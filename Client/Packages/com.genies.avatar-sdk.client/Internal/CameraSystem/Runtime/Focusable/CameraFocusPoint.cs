using UnityEngine;

namespace Genies.CameraSystem.Focusable
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "CameraFocusPoint", menuName = "Genies/CameraFocusPoint")]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CameraFocusPoint : ScriptableObject, IFocusable
#else
    public class CameraFocusPoint : ScriptableObject, IFocusable
#endif
    {
        public Bounds Bounds;
        public Vector3 ViewDirection;
        public Vector3 TargetViewDirection => ViewDirection;
        public Bounds GetBounds() => Bounds;
    }
}
