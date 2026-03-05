using System;
using System.Collections.Generic;
using Genies.Services.Model;

namespace Genies.Inventory
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal readonly struct UserInventoryData
#else
    public readonly struct UserInventoryData
#endif
    {
        public readonly string UserId;
        public readonly IReadOnlyList<UserInventoryItem> Items;

        public UserInventoryData(string userId, List<InventoryAssetInstance> assets)
        {
            UserId = userId;

            // create a mutable array to add items
            var tempItems = new List<UserInventoryItem>(assets.Count);
            foreach (InventoryAssetInstance asset in assets)
            {
                tempItems.Add(new UserInventoryItem(asset));
            }

            // convert to immutable
            Items = tempItems.AsReadOnly();
        }
    }
}
