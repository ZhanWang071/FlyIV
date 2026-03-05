using UnityEngine;

namespace Genies.Components.CreatorTools.TexturePlacement
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct Ray
#else
    public struct Ray
#endif
    {
        public Vector3 Org;
        public Vector3 Dir;

        public Ray(Vector3 org, Vector3 dir)
        {
            Org = org;
            Dir = dir;
        }
    }
}
