using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Inventory
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class InventoryConstants
#else
    public static class InventoryConstants
#endif
    {
        public static int DefaultPageSize = 20, LargePageSize = 200;
    }
}
