using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;

namespace Genies.Customization.MegaEditor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class TransitionNodeCustomizationController : BaseCustomizationController
#else
    public class TransitionNodeCustomizationController : BaseCustomizationController
#endif
    {
        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
        }

        public override void StopCustomization()
        {

        }

        public override void Dispose()
        {

        }

    }
}
