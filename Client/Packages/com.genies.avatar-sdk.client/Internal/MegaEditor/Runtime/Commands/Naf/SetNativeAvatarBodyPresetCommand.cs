using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Customization.Framework;
using Genies.Naf;

namespace Genies.Looks.Customization.Commands
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class SetNativeAvatarBodyPresetCommand : ICommand
#else
    public class SetNativeAvatarBodyPresetCommand : ICommand
#endif
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly GSkelModifierPreset          _preset;
        private readonly GSkelModifierPreset          _previousPreset;

        public SetNativeAvatarBodyPresetCommand(GSkelModifierPreset preset, NativeUnifiedGenieController controller)
        {
            _controller     = controller;
            _preset         = preset;
            _previousPreset = controller.GetBodyPreset();
        }

        public SetNativeAvatarBodyPresetCommand(GSkelModifierPreset preset, GSkelModifierPreset previousPreset, NativeUnifiedGenieController controller)
        {
            _controller     = controller;
            _preset         = preset;
            _previousPreset = previousPreset;
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _controller.SetBodyPresetAsync(_preset);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            return _controller.SetBodyPresetAsync(_previousPreset);
        }
    }
}
