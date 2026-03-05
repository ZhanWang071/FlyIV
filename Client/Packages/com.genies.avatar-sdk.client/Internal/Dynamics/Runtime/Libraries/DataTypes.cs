using UnityEngine;

namespace Genies.Components.Dynamics
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct ParticleData
#else
    public struct ParticleData
#endif
    {
        public int CollisionEnabled;
        public Vector3 CurrentCollisionCenter;
        public float CollisionRadius;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal struct ParticleDataJobs
#else
    public struct ParticleDataJobs
#endif
    {
        public bool CollisionEnabled;
        public Vector3 CurrentCollisionCenter;
        public Vector3 WorldSpaceCollisionCenter;
        public float ScaledCollisionRadius;
        public Vector3 CurrentPosition;
        public Quaternion Rotation;
        public Vector3 CollisionOffset;
        public Quaternion ModelSpaceRotation;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal struct SphereColliderData
#else
    public struct SphereColliderData
#endif
    {
        public Vector3 Position;
        public float CollisionRadius;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal struct CapsuleColliderData
#else
    public struct CapsuleColliderData
#endif
    {
        public Vector3 StartPosition;
        public Vector3 EndPosition;
        public float CollisionRadius;
    }

#if GENIES_SDK && !GENIES_INTERNAL
    internal struct ColliderDataJobs
#else
    public struct ColliderDataJobs
#endif
    {
        public bool IsSphere;
        public Vector3 Center;

        public bool IsCapsule;
        public Vector3 StartPosition;
        public Vector3 EndPosition;

        public float ScaledCollisionRadius;
    }
}
