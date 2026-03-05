using Genies.Avatars;

namespace Genies.Ugc
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal readonly struct GearAssetData
#else
    public readonly struct GearAssetData
#endif
    {
        /// <summary>
        /// The unique ID of this gear asset.
        /// </summary>
        public readonly string Id;

        /// <summary>
        /// The wearable slot.
        /// </summary>
        public readonly string Slot;

        /// <summary>
        /// The wearable subcategory.
        /// </summary>
        public readonly string Subcategory;

        /// <summary>
        /// The collision data of the gear asset.
        /// </summary>
        public readonly OutfitCollisionData CollisionData;

        public GearAssetData(
            string id,
            string slot,
            string subcategory,
            OutfitCollisionData collisionData)
        {
            Id = id;
            Slot = slot;
            Subcategory = subcategory;
            CollisionData = collisionData;
        }
    }
}
