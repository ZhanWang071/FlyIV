using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface ISkinColorController
#else
    public interface ISkinColorController
#endif
    {
        ColorAsset CurrentColor { get; }

        event Action Updated;

        UniTask LoadAndSetSkinColorAsync(string assetId);

        void SetSkinColor(ColorAsset colorAsset);
        void SetSkinColor(Color color);
        bool IsColorEquipped(string assetId);
    }
}
