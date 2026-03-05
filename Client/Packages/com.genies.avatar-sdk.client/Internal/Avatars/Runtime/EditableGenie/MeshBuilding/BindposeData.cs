using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct BindposeData
#else
    public struct BindposeData
#endif
    {
        public int       BoneHash;
        public Matrix4x4 Matrix;
    }
}