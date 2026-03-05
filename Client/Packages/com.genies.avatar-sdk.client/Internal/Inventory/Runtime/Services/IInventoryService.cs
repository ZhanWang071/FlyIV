using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Inventory
{
    /// <summary>
    /// Responsible to manage the metadata of user's inventory
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IInventoryService
#else
    public interface IInventoryService
#endif
    {
        /// <summary>
        /// Get the inventory for the signed-in user, uses v1 endpoint
        /// </summary>
        /// <param name="limit">The max number of assets that can be returned, newest first.
        /// If null, all are returned</param>
        /// <returns></returns>
        UniTask<UserInventoryData> GetUserInventory(int? limit = null);
        UniTask<UserInventoryDecorData> GetUserInventoryDecor();

        /// <summary>
        /// Clears cached inventory items
        /// </summary>
        public void ClearCache();
    }

    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct UserInventoryDecorData
#else
    public struct UserInventoryDecorData
#endif
    {
        public string UserId;
        public List<InventoryDecorData> DecorList;
    }
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct InventoryDecorData
#else
    public struct InventoryDecorData
#endif
    {
        public string AssetId;
    }
}
