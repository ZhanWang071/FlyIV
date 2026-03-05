using System;
using UnityEngine;

namespace Genies.Models
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class CollisionData {
#else
    public class CollisionData {
#endif
        [SerializeField]
        public CollisionType Type;
        [SerializeField]
        public int Layer;
        [SerializeField]
        public CollisionMode Mode;
        [SerializeField]
        public HatHairBehavior HatMode;

    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum CollisionType {
#else
    public enum CollisionType {
#endif
        open,
        closed
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum CollisionMode {
#else
    public enum CollisionMode {
#endif
        none,
        blend,
        simulated
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal enum HatHairBehavior {
#else
    public enum HatHairBehavior {
#endif
        none,
        blendshape,
        fallback
    }
}
