using Genies.Services.Model;

namespace Genies.Inventory
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal readonly struct UserInventoryItem
#else
    public readonly struct UserInventoryItem
#endif
    {
        public string AssetId { get; }
        public string AssetType { get; }
        public string InstanceId { get; }
        public long? DateCreated { get; }
        public string Origin { get;}
        public string Creator { get; }
        public InventoryItemAsset Asset { get; }
        public InventoryTags Tags { get;}

        public UserInventoryItem(InventoryAssetInstance inventoryAsset)
        {
            AssetId = inventoryAsset.Asset.AssetId;
            AssetType = inventoryAsset.Asset.AssetType;
            InstanceId = inventoryAsset.AssetInstanceId;
            DateCreated = inventoryAsset.DateCreated;
            Asset = inventoryAsset.Asset;
            Tags = inventoryAsset.Tags;
            Origin = inventoryAsset.Origin;
            Creator = inventoryAsset.Creator;
        }
    }
}
