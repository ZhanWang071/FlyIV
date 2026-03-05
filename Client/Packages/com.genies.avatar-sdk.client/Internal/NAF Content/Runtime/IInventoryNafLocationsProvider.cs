using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Inventory;
using UnityEngine;

namespace Genies.Naf.Content
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IInventoryNafLocationsProvider
#else
    public interface IInventoryNafLocationsProvider
#endif
    {
        public UniTask UpdateAssetLocations(DefaultInventoryAsset asset);
        public UniTask AddCustomResourceLocationsFromInventory(bool includeV1Inventory = false);
    }
}
