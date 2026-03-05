using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Addressables.CustomResourceLocation;
using Genies.Assets.Services;
using Genies.Models;

namespace Genies.Inventory.Providers
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class InventoryResourceLocationProvider
#else
    public static class InventoryResourceLocationProvider
#endif
    {
        public static readonly string[] _assetLods = { "", $"_{AssetLod.Medium}", $"_{AssetLod.Low}" };
        public static readonly string[] _iconSizes = { "", $"_x512", $"_x1024" };
    }
}
