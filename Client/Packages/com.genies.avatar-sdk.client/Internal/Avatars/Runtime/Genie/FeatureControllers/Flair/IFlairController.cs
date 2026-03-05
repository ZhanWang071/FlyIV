using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IFlairController : IAssetSlotsController<FlairAsset>
#else
    public interface IFlairController : IAssetSlotsController<FlairAsset>
#endif
    {
        Dictionary<string, FlairColorPreset> EquippedColorPresets { get; }
        Dictionary<string, FlairAsset> EquippedPresets { get; }
        public UniTask EquipColorPreset(string presetId, Color[] colorPreset, string slot);
        public  UniTask UnequipColorPreset(string slot);
    }
}
