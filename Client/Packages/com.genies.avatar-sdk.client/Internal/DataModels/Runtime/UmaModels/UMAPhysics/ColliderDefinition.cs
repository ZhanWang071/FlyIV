using UnityEngine;

namespace UMA.Dynamics
{
	[System.Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
	internal class ColliderDefinition
#else
	public class ColliderDefinition
#endif
	{
		[System.Serializable]
		public enum ColliderType {Box, Sphere, Capsule}
		public ColliderType colliderType;
		public Vector3 colliderCentre;

		//Box Collider Only
		[Tooltip("The size of the box collider")]
		public Vector3 boxDimensions;

		//Sphere Collider Only
		public float sphereRadius;

		//Capsule Collider Only
		public float capsuleRadius;
		public float capsuleHeight;
		public enum Direction {X,Y,Z}
		public Direction capsuleAlignment;
	}
}