using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class ModifyUnifiedBodyTypeCommand : UnifiedGenieModificationCommand
#else
    public class ModifyUnifiedBodyTypeCommand : UnifiedGenieModificationCommand
#endif
    {
        private readonly GSkelModifierPreset _bodyPreset;
        private readonly GSkelModifierPreset _previousBodyPreset;

        public ModifyUnifiedBodyTypeCommand(GSkelModifierPreset bodyTypePreset, UnifiedGenieController controller)
        : base(controller)
        {
            _bodyPreset = bodyTypePreset;
            _previousBodyPreset = controller.BodyVariation.GetCurrentBodyAsPreset();
        }

        public ModifyUnifiedBodyTypeCommand(GSkelModifierPreset bodyTypePreset, GSkelModifierPreset prevBodyTypePreset, UnifiedGenieController controller)
        : base(controller)
        {
            _bodyPreset = bodyTypePreset;
            _previousBodyPreset = prevBodyTypePreset;
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.BodyVariation.SetPresetAsync(_bodyPreset);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            await controller.BodyVariation.SetPresetAsync(_previousBodyPreset);
        }
    }
}
