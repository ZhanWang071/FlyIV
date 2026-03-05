using UnityEngine;

namespace UMA
{
	/// <summary>
	/// Base class of UMA mesh combiners.
	/// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    [AddComponentMenu("")]
    internal abstract class UMAMeshCombiner : MonoBehaviour
#else
    public abstract class UMAMeshCombiner : MonoBehaviour
#endif
    {
        public abstract void UpdateUMAMesh(bool updatedAtlas, UMAData umaData, int atlasResolution);

		/// <summary>
		/// This method is called prior to generating the content and allows the MeshCombiner to prepare and/or alter what is about to happen. 
		/// It was introduced to support the UMAPowerTools bone baking that treats isShapeDirty similar to isMeshDirty.
		/// </summary>
		/// <param name="umaData">the character that will be generated</param>
		public virtual void Preprocess(UMAData umaData)	{ }
    }
}
