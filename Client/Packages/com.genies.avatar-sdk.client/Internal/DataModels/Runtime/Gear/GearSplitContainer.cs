using System.Collections.Generic;
using UMA;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// This is the scriptable object for gear split & elements.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "GearSplitElementContainer", menuName = "Genies/Gear/GearSplitElementContainer", order = 0)]
#endif
#if GENIES_SDK && !GENIES_INTERNAL
    internal class GearSplitContainer : ASplitElementContainer
#else
    public class GearSplitContainer : ASplitElementContainer
#endif
    {
        [SerializeField] private List<SlotDataAsset> slotDataAssets;
        [SerializeField] private List<MeshHideAsset> meshHideAssets;
        [SerializeField] private CollisionData collisionData;

        public List<SlotDataAsset> SlotDataAssets
        {
            get => slotDataAssets;
            set => slotDataAssets = value;
        }

        public List<MeshHideAsset> MeshHideAssets
        {
            get => meshHideAssets;
            set => meshHideAssets = value;
        }

        public CollisionData CollisionData
        {
            get => collisionData;
            set => collisionData = value;
        }
    }
}
