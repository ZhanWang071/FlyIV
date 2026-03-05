using System;
using UnityEngine;

namespace Genies.CameraSystem
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FixedCameraAngle
#else
    public class FixedCameraAngle
#endif
    {
        public string name;
        public Vector3 position;
        public Vector3 direction;
    }
}
