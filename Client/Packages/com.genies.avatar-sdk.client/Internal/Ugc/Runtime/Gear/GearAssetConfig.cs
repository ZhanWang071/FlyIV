using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace Genies.Ugc
{
    [Serializable]
#if GENIES_SDK && !GENIES_INTERNAL
    internal struct GearAssetConfig
#else
    public struct GearAssetConfig
#endif
    {
        [FormerlySerializedAs("assetAddress")] public string AssetAddress;
        [FormerlySerializedAs("elementAddresses")] public List<string> ElementAddresses;

        public string GetUniqueName()
        {
            return $"{AssetAddress}_{string.Join("-", ElementAddresses)}";
        }
    }
}
