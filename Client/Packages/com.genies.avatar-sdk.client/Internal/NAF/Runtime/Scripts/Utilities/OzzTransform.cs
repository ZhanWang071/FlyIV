using System.Runtime.InteropServices;
using UnityEngine;

namespace Genies.Naf
{
    [StructLayout(LayoutKind.Sequential)]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct OzzTransform
#else
    public struct OzzTransform
#endif
    {
        public Matrix4x4 Matrix => Matrix4x4.TRS(Translation, Rotation, Scale);

        public Vector3    Translation => new(TranslationX, TranslationY, TranslationZ);
        public Quaternion Rotation    => new(RotationX, RotationY, RotationZ, RotationW);
        public Vector3    Scale       => new(ScaleX, ScaleY, ScaleZ);

        public float TranslationX, TranslationY, TranslationZ;
        public float RotationX, RotationY, RotationZ, RotationW;
        public float ScaleX, ScaleY, ScaleZ;
    }
}
