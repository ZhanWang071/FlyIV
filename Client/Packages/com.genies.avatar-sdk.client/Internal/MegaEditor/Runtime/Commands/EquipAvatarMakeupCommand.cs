using Cysharp.Threading.Tasks;
using Genies.Avatars;


namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Command for equipping a makeup asset with <see cref="_targetId"/> to <see cref="_slotId"/> (slot)
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal class EquipAvatarMakeupCommand : UnifiedGenieModificationCommand
#else
    public class EquipAvatarMakeupCommand : UnifiedGenieModificationCommand
#endif
    {
        private readonly string _targetId;
        private readonly string _slotId;
        private readonly string _previousId;

        public EquipAvatarMakeupCommand(string targetId, string slotId, UnifiedGenieController controller) : base(controller)
        {
            _targetId = targetId;
            _slotId = slotId;

            controller.Makeup.TryGetEquippedAssetId(slotId, out _previousId);
        }

        protected override async UniTask ExecuteModificationAsync(UnifiedGenieController controller)
        {
            await controller.Makeup.LoadAndEquipAssetAsync(_targetId, _slotId);
        }

        protected override async UniTask UndoModificationAsync(UnifiedGenieController controller)
        {
            if (string.IsNullOrEmpty(_previousId))
            {
                await controller.Makeup.ClearSlotAsync(_slotId);
                return;
            }

            await controller.Makeup.LoadAndEquipAssetAsync(_previousId, _slotId);
        }
    }
}
