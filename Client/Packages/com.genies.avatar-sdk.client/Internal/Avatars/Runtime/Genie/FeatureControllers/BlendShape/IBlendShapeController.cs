using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IBlendShapeController : IAssetsController<BlendShapeAsset>
#else
    public interface IBlendShapeController : IAssetsController<BlendShapeAsset>
#endif
    {
        UniTask LoadAndEquipPresetAsync(string assetId);
        UniTask EquipPresetAsync(BlendShapePresetAsset preset);
        string GetEquippedBlendShapeForSlot(string slot);
        UniTask<bool> IsPresetEquippedAsync(string presetId);
        bool IsPresetEquipped(BlendShapePresetAsset preset);
        bool IsPresetEquipped(IEnumerable<string> assetIds);
    }
}