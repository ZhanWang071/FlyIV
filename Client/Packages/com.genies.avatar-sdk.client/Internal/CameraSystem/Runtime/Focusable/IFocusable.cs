using UnityEngine;

namespace Genies.CameraSystem.Focusable
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IFocusable
#else
    public interface IFocusable
#endif
    {
        public Vector3 TargetViewDirection { get;}
        public Bounds GetBounds();
    }
}
