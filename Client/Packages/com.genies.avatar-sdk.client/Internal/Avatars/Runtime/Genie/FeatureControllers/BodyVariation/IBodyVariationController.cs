using System;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal interface IBodyVariationController : IBodyController
#else
    public interface IBodyVariationController : IBodyController
#endif
    {
        //body variations
        string CurrentVariation { get; }
        new event Action Updated;
        UniTask SetBodyVariationAsync(string bodyVariation);

        // deprecated methods to keep compatibility with chaos mode presets
        GSkelModifierPreset GetCurrentBodyAsPreset();
        UniTask SetPresetAsync(GSkelModifierPreset preset);
    }
}
