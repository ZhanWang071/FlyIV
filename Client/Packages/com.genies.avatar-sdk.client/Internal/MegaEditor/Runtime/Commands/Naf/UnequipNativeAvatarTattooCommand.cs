using System.Threading;
using CancellationToken = System.Threading.CancellationToken;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;
using GnWrappers;

namespace Genies.Looks.Customization.Commands
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class UnequipNativeAvatarTattooCommand : ICommand
#else
    public class UnequipNativeAvatarTattooCommand : ICommand
#endif
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly MegaSkinTattooSlot           _slot;
        private readonly string                       _previousTattooGuid;

        public UnequipNativeAvatarTattooCommand(MegaSkinTattooSlot slot, NativeUnifiedGenieController controller)
        {
            _controller         = controller;
            _slot               = slot;
            _previousTattooGuid = controller.GetEquippedTattoo(slot);
        }

        /// <summary>
        /// Returns the tattoo GUID that was equipped in the slot before this unequip command was created.
        /// </summary>
        public string GetPreviousTattooGuid() => _previousTattooGuid;

        public UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _controller.UnequipTattooAsync(_slot);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_previousTattooGuid))
            {
                return _controller.EquipTattooAsync(_slot, _previousTattooGuid);
            }

            return UniTask.CompletedTask;
        }
    }
}
